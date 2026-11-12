"""The nine metrics the comparison screen shows, with their formulas.

Every function takes simple daily returns r_1 ... r_n of one portfolio as a pandas Series with a DatetimeIndex,
and uses P = 252 trading days per year. The definitions match quantstats 0.0.86 (``quantstats.stats``), which
produced these numbers before; ``tests/test_metrics.py`` checks each function against it on synthetic data.

Notation
  W_t  = (1 + r_1)(1 + r_2)...(1 + r_t), the value of 1 unit invested at the start; W_0 = 1
  rf   = annual risk-free rate (a fraction); rf_d = (1 + rf)^(1/P) - 1 is the daily rate with the same yield
  e_t  = r_t - rf_d, the daily excess return
  mean and std are over t = 1..n; std uses n - 1 in the denominator
"""

from __future__ import annotations

import math

import numpy as np
import pandas as pd

from .config import TRADING_DAYS_PER_YEAR

P = TRADING_DAYS_PER_YEAR

METRIC_NAMES = (
    "cumulative_return",
    "cagr",
    "sharpe",
    "sortino",
    "calmar",
    "max_drawdown",
    "volatility",
    "expected_monthly",
    "expected_yearly",
)


def daily_rate(annual_rate: float, periods: int = P) -> float:
    """rf_d = (1 + rf)^(1/P) - 1: compounding it over P days gives rf."""
    return (1.0 + annual_rate) ** (1.0 / periods) - 1.0


def cumulative_return(returns: pd.Series) -> float:
    """Total return over the period: W_n - 1."""
    return float((1.0 + returns).prod() - 1.0)


def cagr(returns: pd.Series, periods: int = P) -> float:
    """Compound annual growth rate: W_n^(1 / years) - 1, with years = n / P.

    Years are counted in trading days (n / 252), as quantstats does, not in calendar days.
    """
    years = returns.count() / periods
    wealth = 1.0 + cumulative_return(returns)
    if years <= 0 or wealth < 0:
        return math.nan
    return float(wealth ** (1.0 / years) - 1.0)


def volatility(returns: pd.Series, periods: int = P) -> float:
    """Annualised volatility: std(r) * sqrt(P)."""
    return float(returns.std(ddof=1) * math.sqrt(periods))


def sharpe(returns: pd.Series, risk_free_rate: float = 0.0, periods: int = P) -> float:
    """Annualised Sharpe ratio: mean(e) / std(e) * sqrt(P)."""
    excess = returns - daily_rate(risk_free_rate, periods)
    deviation = excess.std(ddof=1)
    if not deviation:
        return math.nan
    return float(excess.mean() / deviation * math.sqrt(periods))


def sortino(returns: pd.Series, risk_free_rate: float = 0.0, periods: int = P) -> float:
    """Annualised Sortino ratio: mean(e) / D * sqrt(P), with downside deviation D = sqrt(sum(e_t^2 for e_t < 0) / n).

    Like the Sharpe ratio, but only falls below the risk-free rate count as risk.
    """
    excess = returns - daily_rate(risk_free_rate, periods)
    downside = math.sqrt(float((excess[excess < 0] ** 2).sum()) / excess.count())
    if not downside:
        return math.nan
    return float(excess.mean() / downside * math.sqrt(periods))


def max_drawdown(returns: pd.Series) -> float:
    """Largest fall from a peak: min over t of W_t / max(W_0, ..., W_t) - 1. Zero or negative; W_0 = 1 counts as a peak."""
    wealth = (1.0 + returns).cumprod()
    peaks = np.maximum(wealth.cummax(), 1.0)
    return float(min((wealth / peaks).min() - 1.0, 0.0))


def calmar(returns: pd.Series, periods: int = P) -> float:
    """CAGR / |max drawdown|."""
    drawdown = max_drawdown(returns)
    if not drawdown:
        return math.nan
    return float(cagr(returns, periods) / abs(drawdown))


def expected_return(returns: pd.Series, frequency: str) -> float:
    """Geometric mean return per calendar month or year: (prod over k of (1 + R_k))^(1/K) - 1.

    R_k is the compounded return of the k-th calendar month (or year) in the data, K the number of those periods.
    A partial first or last month counts as a whole one, as in quantstats.
    """
    index = pd.DatetimeIndex(returns.index)
    if frequency == "monthly":
        keys = [index.year, index.month]
    elif frequency == "yearly":
        keys = [index.year]
    else:
        raise ValueError(f"Unknown frequency {frequency!r}; use 'monthly' or 'yearly'.")
    period_returns = (1.0 + returns).groupby(keys).prod() - 1.0
    count = period_returns.count()
    if not count:
        return math.nan
    return float(np.prod(1.0 + period_returns) ** (1.0 / count) - 1.0)


def summary(returns: pd.Series, risk_free_rate: float = 0.0) -> dict[str, float]:
    """All nine metrics for one portfolio's daily returns. Values can be NaN, for example a Sortino ratio with no losing day."""
    return {
        "cumulative_return": cumulative_return(returns),
        "cagr": cagr(returns),
        "sharpe": sharpe(returns, risk_free_rate),
        "sortino": sortino(returns, risk_free_rate),
        "calmar": calmar(returns),
        "max_drawdown": max_drawdown(returns),
        "volatility": volatility(returns),
        "expected_monthly": expected_return(returns, "monthly"),
        "expected_yearly": expected_return(returns, "yearly"),
    }
