"""The risk forecast without HTTP: the bundled model, agreement with the training code, the portfolio series, the
flags for unusual inputs, and refusing a model folder that does not fit this service.

``tests/data/forecast_expected.json`` holds what the training code (ml-training/risk_model/predict.py) forecast for
the synthetic prices in ``tests/data/forecast_prices.csv`` with the bundled model (``make_forecast_fixture.py`` wrote
both). If a feature, a correction or the GARCH blend were computed differently here, these numbers would differ.
"""

from __future__ import annotations

import json
import shutil
from datetime import date
from pathlib import Path

import numpy as np
import pandas as pd
import pytest

from app.config import BUNDLED_MODEL_DIR
from app.errors import MarketDataUnavailable, NotEnoughHistory, SymbolNotFound
from app.forecast.features import BASE_FEATURES, feature_names
from app.forecast.model import ModelLoadError, RiskModel
from app.forecast.portfolio import (
    PORTFOLIO_COLUMN,
    forecast_portfolio,
    market_frame,
    portfolio_prices,
    previous_metrics,
)

DATA = Path(__file__).resolve().parent / "data"
EXPECTED = json.loads((DATA / "forecast_expected.json").read_text(encoding="utf-8"))
METRICS = {"vol": "volatility", "var95": "value_at_risk_95", "mdd": "max_drawdown"}
# GARCH is fitted by a numerical optimiser; everything else is exact.
RTOL = 1e-6


@pytest.fixture(scope="module")
def model() -> RiskModel:
    return RiskModel(BUNDLED_MODEL_DIR)


@pytest.fixture
def closes() -> pd.DataFrame:
    return pd.read_csv(DATA / "forecast_prices.csv", index_col="Date", parse_dates=["Date"])


def assert_matches(risk, expected: dict) -> None:
    for target, field in METRICS.items():
        metric = getattr(risk, field)
        assert metric.value == pytest.approx(expected[target], rel=RTOL), target
        assert metric.low == pytest.approx(expected[f"{target}_lo"], rel=RTOL), target
        assert metric.high == pytest.approx(expected[f"{target}_hi"], rel=RTOL), target
    assert risk.out_of_domain == expected["out_of_domain"]


# ---- the bundled model -------------------------------------------------------------------------------------------


def test_the_bundled_model_is_risk_v1_with_the_services_features(model: RiskModel) -> None:
    card = model.card

    assert (card.version, card.horizon_days, card.interval_level) == ("risk-v1", 63, 0.8)
    assert list(card.features) == feature_names(use_vix=True) == [*BASE_FEATURES, "vix"]
    assert card.trained_through == date(2026, 11, 13)
    assert card.stale_after_days == 60
    assert card.training_stocks == 100
    assert card.evaluation["criteria"] and all(card.evaluation["criteria"].values())
    assert set(model.intervals) == {"vol", "var95", "mdd"}


# ---- agreement with the training code ----------------------------------------------------------------------------


def test_portfolio_forecast_equals_the_training_codes(model: RiskModel, closes: pd.DataFrame) -> None:
    expected = EXPECTED["forecast_portfolio"]

    result = forecast_portfolio(model, closes[["AAA", "BBB", "^GSPC", "^VIX"]], EXPECTED["weights"])

    assert result.as_of == date(2026, 11, 13)
    assert_matches(result.portfolio, expected["PORTFOLIO"])
    assert [asset.symbol for asset in result.assets] == ["AAA", "BBB"]
    for asset in result.assets:
        assert asset.weight == EXPECTED["weights"][asset.symbol]
        assert_matches(asset.forecast, expected[asset.symbol])


def test_forecast_of_stocks_with_different_histories_equals_the_training_codes(
    model: RiskModel, closes: pd.DataFrame
) -> None:
    # CCC trades for 320 days only: its features exist, GARCH (504 days at least) does not, so its volatility is the
    # booster's alone. Each stock keeps its own history; nothing is cut to the youngest one.
    stocks, index_close, vix = market_frame(closes, ["AAA", "BBB", "CCC"], use_vix=True)

    result = model.forecast(stocks, index_close, vix)

    for symbol, expected in EXPECTED["forecast_frame"].items():
        row = result.loc[symbol]
        for target in METRICS:
            for suffix in ("", "_lo", "_hi"):
                assert row[target + suffix] == pytest.approx(expected[target + suffix], rel=RTOL), (symbol, target)
        assert bool(row["out_of_domain"]) == expected["out_of_domain"]


def test_a_forecast_is_repeatable(model: RiskModel, closes: pd.DataFrame) -> None:
    first = forecast_portfolio(model, closes, {"AAA": 0.5, "CCC": 0.5})
    second = forecast_portfolio(model, closes, {"AAA": 0.5, "CCC": 0.5})

    assert first == second


# ---- the portfolio series and the previous-period metrics --------------------------------------------------------


def test_portfolio_is_rebalanced_daily_from_the_youngest_stocks_first_close() -> None:
    stocks = pd.DataFrame(
        {"A": [10.0, 11.0, 12.1, 11.0], "B": [np.nan, 20.0, 22.0, 22.0]}, index=pd.bdate_range("2024-01-01", periods=4)
    )

    series = portfolio_prices(stocks, {"A": 3, "B": 1})  # weights are normalised: 75% and 25%

    # Day 2: A +10%, B +10% -> +10%. Day 3: A -9.09%, B 0% -> -6.82%.
    expected = [np.nan, 1.0, 1.1, 1.1 * (1 + 0.75 * (11.0 / 12.1 - 1))]
    np.testing.assert_allclose(series.to_numpy(), expected, rtol=1e-12)
    assert series.name == PORTFOLIO_COLUMN


def test_a_day_without_a_return_for_one_stock_counts_as_zero_for_the_portfolio() -> None:
    stocks = pd.DataFrame(
        {"A": [10.0, 11.0, np.nan, 13.0], "B": [20.0, 22.0, 24.0, 24.0]}, index=pd.bdate_range("2024-01-01", periods=4)
    )

    series = portfolio_prices(stocks, {"A": 0.5, "B": 0.5})

    # As for the synthetic training portfolios: the day with A missing and the day after (no previous close) are flat.
    np.testing.assert_allclose(series.to_numpy(), [1.0, 1.1, 1.1, 1.1], rtol=1e-12)


def test_previous_metrics_are_the_targets_definitions_over_the_last_h_days() -> None:
    prices = pd.DataFrame({"A": [100.0, 110.0, 99.0, 108.9, 107.811]})

    previous = previous_metrics(prices, horizon=4).loc["A"]

    returns = np.array([0.10, -0.10, 0.10, -0.01])
    assert previous["vol"] == pytest.approx(returns.std(ddof=1) * np.sqrt(252))
    assert previous["var95"] == pytest.approx(-np.quantile(returns, 0.05))
    assert previous["mdd"] == pytest.approx(0.10)  # 110 -> 99
    assert previous_metrics(prices, horizon=5).loc["A"].isna().all()  # needs H + 1 closes


def test_each_forecast_comes_with_the_last_three_months_for_comparison(model: RiskModel, closes: pd.DataFrame) -> None:
    result = forecast_portfolio(model, closes, {"AAA": 1.0})

    expected = previous_metrics(closes[["AAA"]], 63).loc["AAA"]
    risk = result.assets[0].forecast
    assert risk.volatility.previous == pytest.approx(expected["vol"])
    assert risk.value_at_risk_95.previous == pytest.approx(expected["var95"])
    assert risk.max_drawdown.previous == pytest.approx(expected["mdd"])
    # One stock at 100%: the portfolio is that stock.
    assert result.portfolio.volatility.previous == pytest.approx(expected["vol"])


# ---- inputs the model was not trained for ------------------------------------------------------------------------


def test_less_than_252_days_of_common_history_is_refused_naming_the_stock(
    model: RiskModel, closes: pd.DataFrame
) -> None:
    closes.loc[closes.index[:-200], "CCC"] = np.nan

    with pytest.raises(NotEnoughHistory, match=r"CCC has prices only from .* 199 trading days; .* at least 252"):
        forecast_portfolio(model, closes, {"AAA": 0.5, "CCC": 0.5})


def test_exactly_252_days_is_enough(model: RiskModel, closes: pd.DataFrame) -> None:
    closes.loc[closes.index[:-253], "CCC"] = np.nan

    result = forecast_portfolio(model, closes, {"CCC": 1.0})

    assert result.trading_days == 252
    assert result.portfolio.volatility.value is not None


def test_an_unknown_symbol_is_symbol_not_found(model: RiskModel, closes: pd.DataFrame) -> None:
    with pytest.raises(SymbolNotFound, match="ZZZ"):
        forecast_portfolio(model, closes, {"AAA": 0.5, "ZZZ": 0.5})


def test_without_a_recent_vix_there_is_no_forecast(model: RiskModel, closes: pd.DataFrame) -> None:
    closes.loc[closes.index[-4:], "^VIX"] = np.nan  # carried forward for 3 days at most, as in training

    with pytest.raises(MarketDataUnavailable, match="VIX"):
        forecast_portfolio(model, closes, {"AAA": 1.0})
    with pytest.raises(MarketDataUnavailable, match="GSPC"):
        forecast_portfolio(model, closes.drop(columns="^GSPC"), {"AAA": 1.0})


def test_a_stock_far_more_volatile_than_any_in_training_is_flagged(model: RiskModel, closes: pd.DataFrame) -> None:
    returns = closes["BBB"].pct_change().fillna(0)
    closes["BBB"] = 50 * np.cumprod(1 + 12 * returns.clip(-0.08, 0.08))  # daily moves up to 96%

    result = forecast_portfolio(model, closes, {"AAA": 0.9, "BBB": 0.1})

    wild = result.assets[1].forecast
    assert wild.out_of_domain and {"vol_252", "rel_vol_63"} <= set(wild.out_of_domain_features)
    assert not result.assets[0].forecast.out_of_domain
    assert wild.volatility.value is not None  # still forecast, only flagged


def test_stale_quotes_are_flagged(model: RiskModel, closes: pd.DataFrame) -> None:
    # Every other day of the last 63 without a price change: more than the 20% training treated as stale quotes.
    every_other = closes.index[-63::2]
    closes.loc[every_other, "BBB"] = closes["BBB"].shift(1).loc[every_other]

    result = forecast_portfolio(model, closes, {"AAA": 0.5, "BBB": 0.5})

    stale = result.assets[1].forecast
    assert stale.stale_quotes and stale.out_of_domain
    assert not result.assets[0].forecast.stale_quotes


def test_a_stock_with_a_long_gap_has_no_forecast_but_the_portfolio_has(model: RiskModel, closes: pd.DataFrame) -> None:
    closes.loc[closes.index[-100:-90], "BBB"] = np.nan  # longer than the 5 days a close is carried forward

    result = forecast_portfolio(model, closes, {"AAA": 0.5, "BBB": 0.5})

    assert result.assets[1].forecast is None
    assert result.assets[0].forecast is not None
    assert result.portfolio.volatility.value is not None


def test_two_weeks_without_any_price_change_leave_no_forecast(model: RiskModel, closes: pd.DataFrame) -> None:
    # RSI over 14 days is 0 / 0 then: training had no such rows, so the model gives no number rather than a guess.
    closes.loc[closes.index[-14:], "BBB"] = closes["BBB"].iloc[-15]

    result = forecast_portfolio(model, closes, {"AAA": 0.5, "BBB": 0.5})

    assert result.assets[1].forecast is None
    assert result.portfolio.volatility.value is not None


def test_short_gaps_from_other_exchanges_holidays_are_bridged(model: RiskModel, closes: pd.DataFrame) -> None:
    closes.loc[closes.index[-50:-47], "BBB"] = np.nan

    result = forecast_portfolio(model, closes, {"BBB": 1.0})

    assert result.assets[0].forecast is not None


def test_the_portfolio_alone_skips_the_stocks(model: RiskModel, closes: pd.DataFrame) -> None:
    alone = forecast_portfolio(model, closes, EXPECTED["weights"], include_assets=False)
    full = forecast_portfolio(model, closes, EXPECTED["weights"])

    assert alone.assets == []
    assert alone.portfolio == full.portfolio


def test_a_zero_weight_stock_is_forecast_but_does_not_shorten_the_portfolio(
    model: RiskModel, closes: pd.DataFrame
) -> None:
    result = forecast_portfolio(model, closes, {"AAA": 1.0, "CCC": 0.0})

    assert result.trading_days == len(closes) - 1  # AAA's history, not CCC's 320 days
    assert result.assets[1].weight == 0.0 and result.assets[1].forecast is not None


# ---- model folders that must not be used -------------------------------------------------------------------------


@pytest.fixture
def model_copy(tmp_path: Path) -> Path:
    target = tmp_path / "risk-test"
    shutil.copytree(BUNDLED_MODEL_DIR, target)
    return target


def _edit_meta(folder: Path, change) -> None:
    path = folder / "meta.json"
    meta = json.loads(path.read_text(encoding="utf-8"))
    change(meta)
    path.write_text(json.dumps(meta), encoding="utf-8")


def test_a_copy_of_the_model_loads(model_copy: Path) -> None:
    assert RiskModel(model_copy).card.version == "risk-v1"


def test_a_missing_folder_names_the_path(tmp_path: Path) -> None:
    with pytest.raises(ModelLoadError, match="FORECAST_MODEL_DIR"):
        RiskModel(tmp_path / "nowhere")


def test_features_in_another_order_are_refused(model_copy: Path) -> None:
    _edit_meta(model_copy, lambda meta: meta["features"].reverse())

    with pytest.raises(ModelLoadError, match="trained with other features"):
        RiskModel(model_copy)


def test_a_model_without_vix_does_not_fit_boosters_trained_with_it(model_copy: Path) -> None:
    def drop_vix(meta: dict) -> None:
        meta["use_vix"] = False
        meta["features"] = BASE_FEATURES

    _edit_meta(model_copy, drop_vix)

    with pytest.raises(ModelLoadError, match="expects 24 features, meta.json lists 23"):
        RiskModel(model_copy)


def test_a_missing_booster_file_is_refused(model_copy: Path) -> None:
    (model_copy / "mdd.txt").unlink()

    with pytest.raises(ModelLoadError, match="mdd.txt not found"):
        RiskModel(model_copy)


def test_a_broken_booster_file_is_refused(model_copy: Path) -> None:
    (model_copy / "var95.txt").write_text("not a model", encoding="utf-8")

    with pytest.raises(ModelLoadError, match="var95.txt"):
        RiskModel(model_copy)


def test_interval_ratios_that_do_not_surround_the_forecast_are_refused(model_copy: Path) -> None:
    _edit_meta(model_copy, lambda meta: meta["intervals_80"].update(vol=[1.2, 1.5]))

    with pytest.raises(ModelLoadError, match="interval"):
        RiskModel(model_copy)
