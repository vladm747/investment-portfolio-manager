"""metrics.py against quantstats (a dev-only dependency) on synthetic returns, plus a small hand-checked example."""

from __future__ import annotations

import math

import numpy as np
import pandas as pd
import pytest
import quantstats as qs

from app import metrics


def synthetic_returns(seed: int, days: int = 900, drift: float = 0.0004, vol: float = 0.012) -> pd.Series:
    rng = np.random.default_rng(seed)
    index = pd.bdate_range("2019-03-14", periods=days)  # starts and ends mid-month and mid-year
    return pd.Series(rng.normal(drift, vol, size=days), index=index, name="returns")


CASES = [
    pytest.param(synthetic_returns(1), 0.0, id="rf=0"),
    pytest.param(synthetic_returns(2), 0.03, id="rf=3%"),
    pytest.param(synthetic_returns(3, drift=-0.0003, vol=0.02), 0.045, id="losing portfolio, rf=4.5%"),
]


@pytest.mark.parametrize(("returns", "rf"), CASES)
def test_metrics_match_quantstats(returns: pd.Series, rf: float) -> None:
    expected = {
        "cumulative_return": qs.stats.comp(returns),
        "cagr": qs.stats.cagr(returns, periods=252),
        "sharpe": qs.stats.sharpe(returns, rf=rf, periods=252),
        "sortino": qs.stats.sortino(returns, rf=rf, periods=252),
        "calmar": qs.stats.calmar(returns, periods=252),
        "max_drawdown": qs.stats.max_drawdown(returns),
        "volatility": qs.stats.volatility(returns, periods=252),
        "expected_monthly": qs.stats.expected_return(returns, aggregate="ME"),
        "expected_yearly": qs.stats.expected_return(returns, aggregate="YE"),
    }

    actual = metrics.summary(returns, risk_free_rate=rf)

    assert set(actual) == set(metrics.METRIC_NAMES)
    for name, value in expected.items():
        assert actual[name] == pytest.approx(float(value), rel=1e-9, abs=1e-12), name


def test_hand_checked_example() -> None:
    # Three days: +10 %, -5 %, +2 %.
    returns = pd.Series([0.10, -0.05, 0.02], index=pd.to_datetime(["2024-01-30", "2024-01-31", "2024-02-01"]))

    assert metrics.cumulative_return(returns) == pytest.approx(1.10 * 0.95 * 1.02 - 1)  # 0.0659
    # Peak 1.10 after day 1, then 1.045: a 5 % fall.
    assert metrics.max_drawdown(returns) == pytest.approx(1.045 / 1.10 - 1)
    # January compounds to 1.10 * 0.95 - 1 = 4.5 %, February is 2 %; geometric mean of the two months.
    assert metrics.expected_return(returns, "monthly") == pytest.approx(math.sqrt(1.045 * 1.02) - 1)
    # 3 trading days are 3/252 of a year.
    assert metrics.cagr(returns) == pytest.approx((1.10 * 0.95 * 1.02) ** (252 / 3) - 1)


def test_undefined_ratios_are_nan_not_errors() -> None:
    rising = pd.Series([0.01, 0.02, 0.01], index=pd.bdate_range("2024-01-01", periods=3))

    assert metrics.max_drawdown(rising) == 0.0
    assert math.isnan(metrics.calmar(rising))  # no drawdown to divide by
    assert math.isnan(metrics.sortino(rising))  # no day below the risk-free rate


def test_risk_free_rate_lowers_sharpe() -> None:
    returns = synthetic_returns(4)

    assert metrics.sharpe(returns, 0.05) < metrics.sharpe(returns, 0.0)
    assert metrics.daily_rate(0.05) == pytest.approx(1.05 ** (1 / 252) - 1)
