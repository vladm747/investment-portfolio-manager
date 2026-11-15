"""The trained risk model: three LightGBM boosters in LightGBM's own text format and ``meta.json``.

The model is trained offline (ml-training, ``train.py --mode final``) and saved as plain files, never as a pickle,
which runs code when loaded and breaks between library versions. The service only reads them.

For each column of a price frame (a stock or a portfolio), on the frame's last day (meta.json "how_to_forecast"):

    features   the 24 features of that day, in the order of meta.json "features" (``features.py``)
    vol        exp(pred) * exp(s^2 / 2): the booster was trained on ln(vol), so exp(pred) is the median and the factor
               (s^2 = variance of past log errors) turns it into the mean; then averaged with GARCH(1,1) on up to
               10 years of daily returns when GARCH converges (``garch.py``)
    VaR 95%    max(pred, 0) * k, with k scaling past forecasts so that exactly 5% of them were broken
    MDD        max(pred, 0)
    80% band   forecast * [lo, hi]: lo and hi are the 10th and 90th percentiles of actual / forecast over the last
               five years of the walk-forward test

``RiskModel.forecast`` follows the training code's ``risk_model/predict.py`` (``RiskModel.forecast``) step by step;
``tests/test_forecast.py`` checks its output against numbers that code produced for the same prices.
"""

from __future__ import annotations

import json
import threading
from dataclasses import dataclass, field
from datetime import date
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd

from .features import ANN, compute_features, feature_names, stale_mask
from .garch import GARCH_YEARS, garch_vol

TARGETS = ("vol", "var95", "mdd")
INTERVAL_LEVEL = 0.80
MIN_VALUE = 1e-6


class ModelLoadError(RuntimeError):
    """The model folder is missing, incomplete or does not match this service's features."""


@dataclass(frozen=True)
class ModelCard:
    """What the model is, from meta.json: shown to users next to every forecast and by GET /v1/forecast/model."""

    version: str
    created: str
    horizon_days: int
    trained_through: date  # last price date of the training data
    last_sample: date  # last day whose H-day target was known, so the last training example
    stale_after_days: int
    features: tuple[str, ...]
    training_stocks: int
    training_portfolios: int
    chosen_variants: dict[str, Any] = field(default_factory=dict)
    evaluation: dict[str, Any] = field(default_factory=dict)
    interval_level: float = INTERVAL_LEVEL


class RiskModel:
    """Loads a model folder once; ``forecast`` is safe to call from several request threads."""

    def __init__(self, model_dir: str | Path):
        import lightgbm as lgb

        self.dir = Path(model_dir)
        meta_path = self.dir / "meta.json"
        try:
            self.meta: dict[str, Any] = json.loads(meta_path.read_text(encoding="utf-8"))
        except FileNotFoundError:
            raise ModelLoadError(f"{meta_path} not found: FORECAST_MODEL_DIR must name a model folder.") from None
        except json.JSONDecodeError as error:
            raise ModelLoadError(f"{meta_path} is not valid JSON: {error}") from None

        meta = self.meta
        try:
            self.use_vix = bool(meta["use_vix"])
            self.features: list[str] = list(meta["features"])
            self.horizon = int(meta["horizon_days"])
            self.vol_objective = meta["vol"]["objective"]
            self.var_method = meta["var95"]["method"]
            domain = meta["feature_domain"]
            files = {target: self.dir / meta[target]["file"] for target in TARGETS}
        except (KeyError, TypeError, ValueError) as error:
            raise ModelLoadError(f"{meta_path} lacks a required entry: {error!r}") from None

        expected = feature_names(self.use_vix)
        if self.features != expected:
            raise ModelLoadError(
                f"{meta_path} lists the features {self.features}, but this service computes {expected}: "
                "the model was trained with other features and cannot be used."
            )
        if self.horizon <= 0:
            raise ModelLoadError(f"{meta_path}: horizon_days must be positive, got {self.horizon}.")
        if self.vol_objective not in ("log", "gamma"):
            raise ModelLoadError(f"{meta_path}: unknown vol objective {self.vol_objective!r}.")
        if self.var_method not in ("target", "quantile"):
            raise ModelLoadError(f"{meta_path}: unknown VaR method {self.var_method!r}.")
        missing_domain = [name for name in self.features if name not in domain]
        if missing_domain:
            raise ModelLoadError(f"{meta_path}: feature_domain lacks {missing_domain}.")
        self.domain = {name: (float(domain[name]["min"]), float(domain[name]["max"])) for name in self.features}

        self.boosters = {}
        for target, path in files.items():
            if not path.is_file():
                raise ModelLoadError(f"{path} not found.")
            try:
                booster = lgb.Booster(model_file=str(path))
            except lgb.basic.LightGBMError as error:
                raise ModelLoadError(f"{path} is not a LightGBM model: {error}") from None
            if booster.num_feature() != len(self.features):
                raise ModelLoadError(
                    f"{path} expects {booster.num_feature()} features, meta.json lists {len(self.features)}."
                )
            self.boosters[target] = booster

        self.intervals: dict[str, tuple[float, float]] = {}
        for target, ratios in (meta.get("intervals_80") or {}).items():
            low, high = float(ratios[0]), float(ratios[1])
            if not 0 < low <= 1 <= high:
                raise ModelLoadError(f"{meta_path}: the 80% interval ratios of {target} are not around 1: {ratios}.")
            self.intervals[target] = (low, high)

        training = meta.get("training", {})
        walk_forward = meta.get("walk_forward", {})
        self.card = ModelCard(
            version=str(meta.get("model_version", self.dir.name)),
            created=str(meta.get("created_utc", "")),
            horizon_days=self.horizon,
            trained_through=date.fromisoformat(training["last_price_date"]),
            last_sample=date.fromisoformat(training["last_sample"]),
            stale_after_days=int(meta.get("stale_after_days", 60)),
            features=tuple(self.features),
            training_stocks=len(training.get("stocks", [])),
            training_portfolios=int(training.get("train_portfolios", 0)),
            chosen_variants=dict(meta.get("chosen_variants", {})),
            evaluation={
                "testStart": walk_forward.get("test_start"),
                "criteria": walk_forward.get("criteria", {}),
                "varBreachRate": walk_forward.get("var_breach_rate"),
                "dieboldMariano": walk_forward.get("dm", {}),
            },
        )
        # LightGBM's prediction is not documented as thread-safe; requests run in a thread pool, and a forecast
        # predicts a few rows only, so one lock costs nothing.
        self._lock = threading.Lock()

    def forecast(self, prices: pd.DataFrame, index_close: pd.Series, vix: pd.Series | None = None) -> pd.DataFrame:
        """One row per column of ``prices`` (dates x assets, ending on the forecast date, NaN before an asset exists).

        Columns: vol, var95, mdd and their _lo / _hi bounds (NaN where the features of the last day are incomplete,
        for example with less than 253 days of history), features_ok, out_of_domain (a feature outside the range seen
        in training, or stale quotes), out_of_domain_features (their names) and stale_quotes.
        """
        feats = compute_features(prices, index_close, vix if self.use_vix else None)
        X = pd.DataFrame({name: feats[name].iloc[-1] for name in self.features})
        ok = X.notna().all(axis=1)
        out = pd.DataFrame(index=X.index)
        raw = {target: pd.Series(np.nan, index=X.index) for target in self.boosters}
        if ok.any():
            values = X.loc[ok, self.features].to_numpy(dtype=np.float64)
            with self._lock:
                for target, booster in self.boosters.items():
                    raw[target][ok] = booster.predict(values, num_threads=1)

        m = self.meta
        if self.vol_objective == "log":
            vol = np.exp(raw["vol"]) * np.exp(m["vol"]["bias_s2"] / 2)
        else:
            vol = raw["vol"]
        if m["vol"].get("garch_blend"):
            returns = prices.pct_change(fill_method=None).to_numpy()[-GARCH_YEARS * ANN :]
            g = np.array(
                [garch_vol(returns[:, k], self.horizon) if ok.iloc[k] else np.nan for k in range(prices.shape[1])]
            )
            vol = pd.Series(np.where(np.isfinite(g), (vol.to_numpy() + g) / 2, vol.to_numpy()), index=X.index)
        if self.var_method == "target":
            var = np.maximum(raw["var95"], MIN_VALUE) * m["var95"]["k"]
        else:
            var = np.maximum(-raw["var95"], MIN_VALUE)
        out["vol"], out["var95"], out["mdd"] = vol, var, np.maximum(raw["mdd"], MIN_VALUE)
        for target in TARGETS:
            if target in self.intervals:
                low, high = self.intervals[target]
                out[f"{target}_lo"], out[f"{target}_hi"] = out[target] * low, out[target] * high

        outside = [
            tuple(
                name
                for name in self.features
                if pd.notna(X.loc[asset, name])
                and not self.domain[name][0] <= X.loc[asset, name] <= self.domain[name][1]
            )
            for asset in X.index
        ]
        stale = stale_mask(prices).iloc[-1].astype(bool)  # a comparison, so False (not NaN) without history
        out["out_of_domain_features"] = pd.Series(outside, index=X.index, dtype=object)
        out["stale_quotes"] = stale
        out["out_of_domain"] = [bool(names) or bool(is_stale) for names, is_stale in zip(outside, stale, strict=True)]
        out["features_ok"] = ok
        return out
