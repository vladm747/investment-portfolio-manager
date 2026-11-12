from __future__ import annotations

import math

import numpy as np
import pandas as pd
import pytest
from pypfopt import expected_returns, risk_models
from pypfopt.base_optimizer import portfolio_performance

from app import metrics
from app.errors import InvalidWeights, NotEnoughHistory, OptimisationFailed, SymbolNotFound
from app.optimisation import (
    common_window,
    daily_returns,
    max_sharpe_weights,
    normalise_portfolio,
    optimise,
    portfolio_returns,
)
from tests.fakes import random_walk_prices

# ---- weights ------------------------------------------------------------------------------------------------------


def test_weights_are_paired_with_symbols_in_order() -> None:
    assert normalise_portfolio(["aapl", "MSFT "], [0.25, 0.75]) == {"AAPL": 0.25, "MSFT": 0.75}


def test_repeated_symbols_are_combined() -> None:
    # One row per purchase: two AAPL lots.
    weights = normalise_portfolio(["AAPL", "MSFT", "AAPL"], [0.2, 0.5, 0.3])

    assert list(weights) == ["AAPL", "MSFT"]
    assert weights["AAPL"] == pytest.approx(0.5)


@pytest.mark.parametrize(
    ("symbols", "weights", "message"),
    [
        (["AAPL", "MSFT"], [1.0], "2 symbols but 1 weights"),
        (["AAPL", "MSFT"], [0.6, 0.6], "add up to 1.200000"),
        (["AAPL", "MSFT"], [1.2, -0.2], "zero or positive"),
        (["AAPL", "MSFT"], [math.nan, 1.0], "zero or positive"),
        ([], [], "At least one symbol"),
    ],
)
def test_invalid_weights_are_rejected(symbols, weights, message) -> None:
    with pytest.raises(InvalidWeights, match=message):
        normalise_portfolio(symbols, weights)


def test_weights_within_rounding_are_accepted() -> None:
    assert normalise_portfolio(["A", "B", "C"], [1 / 3, 1 / 3, 1 / 3 + 5e-7])


# ---- window -------------------------------------------------------------------------------------------------------


def test_window_starts_at_the_youngest_symbols_first_price() -> None:
    # Overview row 14: a stock listed after HISTORY_START used to make the optimisation fail.
    closes = random_walk_prices(["OLD", "NEW"], start="2010-07-01", days=1200)
    listing_day = closes.index[400]
    closes.loc[closes.index < listing_day, "NEW"] = np.nan

    window = common_window(closes, min_history_days=252)

    assert window.index[0] == listing_day
    assert not window.isna().any().any()


def test_gaps_inside_the_window_carry_the_previous_close() -> None:
    closes = random_walk_prices(["A", "B"], days=300)
    holiday = closes.index[100]
    closes.loc[holiday, "B"] = np.nan  # an exchange holiday for B only

    window = common_window(closes, min_history_days=252)

    assert window.loc[holiday, "B"] == closes["B"].iloc[99]


def test_short_history_is_rejected_with_the_symbol_and_dates() -> None:
    closes = random_walk_prices(["OLD", "NEW"], days=600)
    closes.loc[closes.index < closes.index[450], "NEW"] = np.nan  # 149 returns of common history

    with pytest.raises(NotEnoughHistory, match=r"NEW has prices only from .* 149 trading days; .* at least 252"):
        common_window(closes, min_history_days=252)


def test_symbol_without_prices_is_not_found() -> None:
    closes = random_walk_prices(["A"], days=300).assign(GHOST=np.nan)

    with pytest.raises(SymbolNotFound, match="GHOST"):
        common_window(closes, min_history_days=252)


# ---- optimiser ----------------------------------------------------------------------------------------------------


def test_max_sharpe_weights_are_long_only_and_beat_the_current_weights() -> None:
    prices = random_walk_prices(["A", "B", "C", "D"], days=1000)
    rf = 0.02
    current = {"A": 0.25, "B": 0.25, "C": 0.25, "D": 0.25}

    weights = max_sharpe_weights(prices, rf)

    assert list(weights) == ["A", "B", "C", "D"]
    assert sum(weights.values()) == pytest.approx(1.0, abs=1e-12)
    assert all(0.0 <= w <= 1.0 for w in weights.values())
    # The optimiser's own objective: Sharpe of (mu, S) with the same rf.
    mu = expected_returns.mean_historical_return(prices)
    cov = risk_models.sample_cov(prices)
    optimised_sharpe = portfolio_performance(weights, mu, cov, risk_free_rate=rf)[2]
    current_sharpe = portfolio_performance(current, mu, cov, risk_free_rate=rf)[2]
    assert optimised_sharpe >= current_sharpe - 1e-9


def test_one_symbol_gets_all_the_weight() -> None:
    assert max_sharpe_weights(random_walk_prices(["ONLY"], days=300), 0.0) == {"ONLY": 1.0}


def test_risk_free_rate_reaches_the_optimiser() -> None:
    prices = random_walk_prices(["A", "B"], days=600, drifts=[0.0001, 0.0002])  # roughly 3 % and 5 % a year

    with pytest.raises(OptimisationFailed, match="exceeds the risk-free rate"):
        max_sharpe_weights(prices, risk_free_rate=0.5)


def test_optimise_uses_one_risk_free_rate_for_weights_and_metrics() -> None:
    closes = random_walk_prices(["A", "B", "C"], days=800)
    base = {"A": 0.5, "B": 0.3, "C": 0.2}
    rf = 0.03

    result = optimise(closes, base, risk_free_rate=rf, min_history_days=252)

    returns = daily_returns(closes)
    assert result.risk_free_rate == rf
    assert result.optimised_weights == pytest.approx(max_sharpe_weights(closes, rf))
    assert result.base_metrics["sharpe"] == pytest.approx(metrics.sharpe(portfolio_returns(returns, base), rf))
    assert result.optimised_metrics == pytest.approx(
        metrics.summary(portfolio_returns(returns, result.optimised_weights), rf), nan_ok=True
    )
    assert (result.start, result.end, result.trading_days) == (returns.index[0].date(), returns.index[-1].date(), 799)


def test_portfolio_returns_are_the_weighted_sum_of_daily_returns() -> None:
    returns = pd.DataFrame({"A": [0.01, -0.02], "B": [0.03, 0.01]}, index=pd.bdate_range("2024-01-01", periods=2))

    combined = portfolio_returns(returns, {"A": 0.25, "B": 0.75})

    assert combined.tolist() == pytest.approx([0.25 * 0.01 + 0.75 * 0.03, 0.25 * -0.02 + 0.75 * 0.01])
