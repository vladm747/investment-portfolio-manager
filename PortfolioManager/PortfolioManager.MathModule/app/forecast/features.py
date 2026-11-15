"""The model's 23 features (+ VIX), computed from closing prices. Every feature on day t uses closes up to and
including day t only.

This file is the training code's ``risk_model/features.py`` (ml-training, model risk-v1) without changes below this
docstring: the model only gives the forecasts it was tested on if the service computes every feature exactly as
training did. ``tests/test_forecast.py`` checks the whole forecast against numbers the training code produced, so a
change here that alters any feature fails that test. Change both copies together, and only with a retrained model.

The service calls compute_features on the price history of the user's stocks and of the portfolio (columns of one
DataFrame) together with ^GSPC and ^VIX, and takes the last row.

23 features (as in the pilot, with the two cross-sectional ranks replaced by features relative to the market,
which can be computed from one asset and the index):
    returns       ret_21, ret_63, ret_126, ret_252, mom_12_1
    volatility    vol_21, vol_63, vol_252, ewma_vol (lambda 0.94), dvol_63 (downside), vol_ratio (vol_21/vol_252)
    shape/trend   dd_252, skew_63, ma50_dist, ma200_dist, rsi_14
    market link   beta_252
    vs market     rel_ret_252 (ret_252 minus index ret_252), rel_vol_63 (vol_63 / index vol_63)
    market state  mkt_ret_63, mkt_vol_21, mkt_vol_63, mkt_dd_252
Optional 24th: vix (^VIX / 100); risk-v1 uses it (meta.json "use_vix").
"""
from __future__ import annotations

import numpy as np
import pandas as pd

ANN = 252
EWMA_LAMBDA = 0.94

BASE_FEATURES = [
    "ret_21", "ret_63", "ret_126", "ret_252", "mom_12_1",
    "vol_21", "vol_63", "vol_252", "ewma_vol", "dvol_63", "vol_ratio",
    "dd_252", "skew_63", "ma50_dist", "ma200_dist", "rsi_14",
    "beta_252",
    "rel_ret_252", "rel_vol_63",
    "mkt_ret_63", "mkt_vol_21", "mkt_vol_63", "mkt_dd_252",
]
VIX_FEATURE = "vix"
STALE_ZERO_SHARE = 0.20   # more than 20% of days without a price change in 63 days = stale quotes (early RRC)
LONGEST_WINDOW = 252


def feature_names(use_vix: bool) -> list[str]:
    return BASE_FEATURES + ([VIX_FEATURE] if use_vix else [])


def simple_returns(prices: pd.DataFrame | pd.Series):
    returns = prices.pct_change(fill_method=None)
    return returns


def _window_return(log_returns, window: int, lag: int = 0):
    return np.expm1(log_returns.shift(lag).rolling(window).sum())


def _broadcast(series: pd.Series, like: pd.DataFrame) -> pd.DataFrame:
    return pd.DataFrame(np.repeat(series.to_numpy()[:, None], like.shape[1], axis=1), index=like.index, columns=like.columns)


def compute_features(prices: pd.DataFrame, index_close: pd.Series, vix: pd.Series | None = None) -> dict[str, pd.DataFrame]:
    """prices: dates x assets (NaN before an asset exists); index_close: ^GSPC on the same dates.

    Returns {feature name: dates x assets frame}. Windows need full data (min_periods = window).
    """
    index_close = index_close.reindex(prices.index)
    r = simple_returns(prices)
    lr = np.log1p(r)
    ri = simple_returns(index_close)
    lri = np.log1p(ri)

    f: dict[str, pd.DataFrame] = {}
    for k in (21, 63, 126, 252):
        f[f"ret_{k}"] = _window_return(lr, k)
    f["mom_12_1"] = _window_return(lr, 231, lag=21)               # 12 months back, skipping the last month
    for k in (21, 63, 252):
        f[f"vol_{k}"] = r.rolling(k).std() * np.sqrt(ANN)
    f["ewma_vol"] = np.sqrt((r ** 2).ewm(alpha=1 - EWMA_LAMBDA).mean() * ANN)
    f["dvol_63"] = np.sqrt((r.clip(upper=0) ** 2).rolling(63).mean() * ANN)
    f["vol_ratio"] = f["vol_21"] / f["vol_252"]
    f["dd_252"] = prices / prices.rolling(252).max() - 1
    f["skew_63"] = r.rolling(63).skew()
    f["ma50_dist"] = prices / prices.rolling(50).mean() - 1
    f["ma200_dist"] = prices / prices.rolling(200).mean() - 1
    up = r.clip(lower=0).rolling(14).mean()
    down = (-r.clip(upper=0)).rolling(14).mean()
    f["rsi_14"] = 100 - 100 / (1 + up / down)

    ri_frame = _broadcast(ri, prices)
    cov = r.rolling(252).cov(ri_frame)
    f["beta_252"] = cov / _broadcast(ri.rolling(252).var(), prices)

    mkt_ret_252 = np.expm1(lri.rolling(252).sum())
    mkt_vol_63 = ri.rolling(63).std() * np.sqrt(ANN)
    f["rel_ret_252"] = f["ret_252"] - _broadcast(mkt_ret_252, prices)
    f["rel_vol_63"] = f["vol_63"] / _broadcast(mkt_vol_63, prices)

    f["mkt_ret_63"] = _broadcast(np.expm1(lri.rolling(63).sum()), prices)
    f["mkt_vol_21"] = _broadcast(ri.rolling(21).std() * np.sqrt(ANN), prices)
    f["mkt_vol_63"] = _broadcast(mkt_vol_63, prices)
    f["mkt_dd_252"] = _broadcast(index_close / index_close.rolling(252).max() - 1, prices)

    if vix is not None:
        f[VIX_FEATURE] = _broadcast(vix.reindex(prices.index) / 100.0, prices)

    for name, frame in f.items():
        f[name] = frame.replace([np.inf, -np.inf], np.nan)
    return f


def stale_mask(prices: pd.DataFrame) -> pd.DataFrame:
    """True where the last 63 days look like stale quotes (too many days without any price change)."""
    r = simple_returns(prices)
    zero_share = (r == 0).astype(float).where(r.notna()).rolling(63).mean()
    return zero_share > STALE_ZERO_SHARE


def to_array(features: dict[str, pd.DataFrame], names: list[str]) -> np.ndarray:
    """Stack into a float32 array of shape (dates, assets, features) in the given order."""
    return np.stack([features[n].to_numpy(dtype=np.float32) for n in names], axis=2)
