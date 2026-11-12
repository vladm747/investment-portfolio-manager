"""Max-Sharpe optimisation of a long-only portfolio on historical prices. No network access.

Given adjusted closes (one column per symbol) and the portfolio's current weights:

1. Window: from the youngest symbol's first close onwards, so every symbol has a price on every day
   (``common_window``). Gaps from different exchange holidays carry the previous close forward.
   Fewer than ``min_history_days`` daily returns is an error (``NotEnoughHistory``).
2. Daily returns r_t = P_t / P_(t-1) - 1 per symbol.
3. Expected returns mu = annualised geometric mean return per symbol, and the annualised sample covariance S
   (PyPortfolioOpt ``mean_historical_return`` and ``sample_cov``, 252 trading days).
4. Optimised weights w* = argmax (w . mu - rf) / sqrt(w' S w) subject to sum(w) = 1 and 0 <= w_i <= 1
   (``EfficientFrontier.max_sharpe``), with the same rf as the metrics.
5. Both portfolios are held at constant weights, rebalanced daily: r_p,t = sum_i w_i r_i,t.
   Their metrics come from ``metrics.summary`` over the same window.

The evaluation is in-sample: the weights are fitted and measured on the same period.
"""

from __future__ import annotations

import math
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from datetime import date

import pandas as pd
from pypfopt import expected_returns, risk_models
from pypfopt.efficient_frontier import EfficientFrontier
from pypfopt.exceptions import OptimizationError

from . import metrics
from .config import TRADING_DAYS_PER_YEAR
from .errors import InvalidWeights, NotEnoughHistory, OptimisationFailed, SymbolNotFound

WEIGHT_SUM_TOLERANCE = 1e-6


@dataclass(frozen=True)
class OptimisationResult:
    start: date
    end: date
    trading_days: int
    risk_free_rate: float
    base_weights: dict[str, float]
    optimised_weights: dict[str, float]
    base_metrics: dict[str, float]
    optimised_metrics: dict[str, float]


def normalise_portfolio(symbols: Sequence[str], weights: Sequence[float]) -> dict[str, float]:
    """Checks the weights and returns {symbol: weight}, in first-seen order.

    A portfolio holds one row per purchase, so a symbol can appear more than once; its weights are added up.
    """
    if len(symbols) != len(weights):
        raise InvalidWeights(f"Got {len(symbols)} symbols but {len(weights)} weights; they must match one to one.")
    if not symbols:
        raise InvalidWeights("At least one symbol is required.")
    for symbol, weight in zip(symbols, weights, strict=True):
        if not math.isfinite(weight) or weight < 0:
            raise InvalidWeights(f"Weight of {symbol} is {weight}; weights must be zero or positive numbers.")
    total = math.fsum(weights)
    if abs(total - 1.0) > WEIGHT_SUM_TOLERANCE:
        raise InvalidWeights(f"Weights must add up to 1; they add up to {total:.6f}.")

    combined: dict[str, float] = {}
    for symbol, weight in zip(symbols, weights, strict=True):
        key = symbol.strip().upper()
        if not key:
            raise InvalidWeights("Symbols must not be empty.")
        combined[key] = combined.get(key, 0.0) + float(weight)
    return combined


def common_window(closes: pd.DataFrame, min_history_days: int) -> pd.DataFrame:
    """Closes from the day every symbol has a price (the youngest symbol's first close), gaps forward-filled."""
    first_dates = {symbol: closes[symbol].first_valid_index() for symbol in closes.columns}
    missing = [symbol for symbol, first in first_dates.items() if first is None]
    if missing:
        raise SymbolNotFound(f"No price history for {', '.join(missing)}.")

    youngest = max(first_dates, key=lambda symbol: first_dates[symbol])
    start = first_dates[youngest]
    window = closes.loc[closes.index >= start].ffill()
    trading_days = len(window) - 1
    if trading_days < min_history_days:
        raise NotEnoughHistory(
            f"Not enough price history: {youngest} has prices only from {start:%Y-%m-%d}, which gives "
            f"{trading_days} trading days; the optimisation needs at least {min_history_days}."
        )
    return window


def daily_returns(prices: pd.DataFrame) -> pd.DataFrame:
    return prices.pct_change(fill_method=None).iloc[1:]


def portfolio_returns(returns: pd.DataFrame, weights: Mapping[str, float]) -> pd.Series:
    """r_p,t = sum_i w_i r_i,t: constant weights, rebalanced daily."""
    weight_series = pd.Series(weights, dtype=float)
    return returns[list(weights)].mul(weight_series, axis=1).sum(axis=1)


def max_sharpe_weights(prices: pd.DataFrame, risk_free_rate: float) -> dict[str, float]:
    symbols = list(prices.columns)
    if len(symbols) == 1:
        return {symbols[0]: 1.0}

    mu = expected_returns.mean_historical_return(prices, frequency=TRADING_DAYS_PER_YEAR)
    covariance = risk_models.sample_cov(prices, frequency=TRADING_DAYS_PER_YEAR)
    if float(mu.max()) <= risk_free_rate:
        raise OptimisationFailed(
            f"No symbol's expected return (best {float(mu.max()):.2%} a year) exceeds the risk-free rate "
            f"({risk_free_rate:.2%}), so the maximum-Sharpe portfolio is not defined."
        )
    try:
        raw = EfficientFrontier(mu, covariance).max_sharpe(risk_free_rate=risk_free_rate)
    except (OptimizationError, ValueError) as error:
        raise OptimisationFailed(f"The optimiser found no solution: {error}") from error

    # The solver can return tiny negative numbers such as -1e-17; clip them and rescale to a sum of exactly 1.
    clipped = {symbol: max(0.0, float(raw[symbol])) for symbol in symbols}
    total = math.fsum(clipped.values())
    return {symbol: weight / total for symbol, weight in clipped.items()}


def optimise(
    closes: pd.DataFrame,
    base_weights: Mapping[str, float],
    risk_free_rate: float,
    min_history_days: int = TRADING_DAYS_PER_YEAR,
) -> OptimisationResult:
    """Max-Sharpe weights for the portfolio's symbols, and the nine metrics of the current and optimised portfolios."""
    prices = common_window(closes[list(base_weights)], min_history_days)
    returns = daily_returns(prices)
    optimised_weights = max_sharpe_weights(prices, risk_free_rate)

    return OptimisationResult(
        start=returns.index[0].date(),
        end=returns.index[-1].date(),
        trading_days=len(returns),
        risk_free_rate=risk_free_rate,
        base_weights=dict(base_weights),
        optimised_weights=optimised_weights,
        base_metrics=metrics.summary(portfolio_returns(returns, base_weights), risk_free_rate),
        optimised_metrics=metrics.summary(portfolio_returns(returns, optimised_weights), risk_free_rate),
    )
