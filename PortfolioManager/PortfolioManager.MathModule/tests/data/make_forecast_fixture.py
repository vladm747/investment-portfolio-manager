"""Writes the two files tests/test_forecast.py compares the service with. Not a test; run by hand, only again after
the model or the training code changes:

    python tests/data/make_forecast_fixture.py <path to ml-training>

forecast_prices.csv    900 business days of synthetic closes (no market data): AAA and BBB for the whole period,
                       CCC listed 320 days before the end, ^GSPC and ^VIX. Seeded, so the file is reproducible.
forecast_expected.json what the training code (ml-training/risk_model/predict.py) forecasts from those prices with
                       models/risk-v1: its forecast_portfolio for AAA 60% + BBB 40%, and its forecast of the frame
                       of all three stocks (CCC has too little history for GARCH, so that path is covered too).
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np
import pandas as pd

HERE = Path(__file__).resolve().parent
MODEL_DIR = HERE.parents[1] / "models" / "risk-v1"
WEIGHTS = {"AAA": 0.6, "BBB": 0.4}
COLUMNS = ["vol", "var95", "mdd", "vol_lo", "vol_hi", "var95_lo", "var95_hi", "mdd_lo", "mdd_hi"]


def synthetic_prices(days: int = 900, seed: int = 20261115) -> pd.DataFrame:
    rng = np.random.default_rng(seed)
    dates = pd.bdate_range(end="2026-11-13", periods=days)
    # A calm market with one stressed stretch three months before the end.
    sigma = np.full(days, 0.008)
    sigma[days - 130 : days - 90] = 0.022
    market = rng.normal(0.0003, 1.0, days) * sigma
    index = 4000 * np.cumprod(1 + market)
    vix = np.clip(11 + 900 * pd.Series(np.abs(market)).ewm(span=10).mean().to_numpy(), 9.5, 60)
    stocks = {}
    for name, beta, idio, drift in (("AAA", 1.1, 0.012, 0.0004), ("BBB", 0.7, 0.009, 0.0002), ("CCC", 1.4, 0.02, 0.0)):
        returns = drift + beta * market + rng.normal(0, idio, days)
        stocks[name] = 50 * np.cumprod(1 + returns)
    frame = pd.DataFrame({**stocks, "^GSPC": index, "^VIX": vix}, index=dates)
    frame.loc[frame.index[: days - 320], "CCC"] = np.nan
    frame.index.name = "Date"
    return frame.round({"AAA": 4, "BBB": 4, "CCC": 4, "^GSPC": 2, "^VIX": 2})


def rows(frame: pd.DataFrame) -> dict:
    return {
        asset: {
            **{c: float(frame.loc[asset, c]) for c in COLUMNS},
            "out_of_domain": bool(frame.loc[asset, "out_of_domain"]),
        }
        for asset in frame.index
    }


def main(training_dir: str) -> None:
    sys.path.insert(0, training_dir)
    from risk_model.predict import RiskModel  # the training code, not the service's

    prices = synthetic_prices()
    prices.to_csv(HERE / "forecast_prices.csv")
    prices = pd.read_csv(
        HERE / "forecast_prices.csv", index_col="Date", parse_dates=["Date"]
    )  # exactly what tests read

    model = RiskModel(MODEL_DIR)
    index_close, vix = prices["^GSPC"], prices["^VIX"]
    portfolio = model.forecast_portfolio(prices[["AAA", "BBB"]], WEIGHTS, index_close, vix)
    frame = model.forecast(prices[["AAA", "BBB", "CCC"]], index_close, vix)
    expected = {
        "model_version": model.meta["model_version"],
        "weights": WEIGHTS,
        "forecast_portfolio": rows(portfolio),
        "forecast_frame": rows(frame),
    }
    (HERE / "forecast_expected.json").write_text(json.dumps(expected, indent=1) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main(sys.argv[1])
