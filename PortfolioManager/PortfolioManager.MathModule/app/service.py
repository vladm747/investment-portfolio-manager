"""Use cases shared by the v1 and legacy endpoints: fetch prices from the provider, then run the pure maths."""

from __future__ import annotations

import logging
from collections.abc import Sequence
from datetime import date

import pandas as pd

from .config import Settings
from .growth import Position, portfolio_value_series
from .optimisation import OptimisationResult, normalise_portfolio, optimise
from .providers import MarketDataProvider

logger = logging.getLogger(__name__)


def run_optimisation(
    provider: MarketDataProvider, settings: Settings, symbols: Sequence[str], weights: Sequence[float]
) -> OptimisationResult:
    base_weights = normalise_portfolio(symbols, weights)
    closes = provider.close_prices(list(base_weights), start=settings.history_start)
    return optimise(closes, base_weights, settings.risk_free_rate, settings.min_history_days)


def run_growth(provider: MarketDataProvider, positions: Sequence[Position], end: date) -> pd.Series:
    if not positions:
        return pd.Series(dtype=float)
    symbols = list(dict.fromkeys(position.symbol for position in positions))
    closes = provider.close_prices(symbols, start=min(position.entry_date for position in positions))
    for symbol in symbols:
        if closes[symbol].dropna().empty:
            logger.warning("No price history for %s; it adds 0 to the growth series.", symbol)
    return portfolio_value_series(positions, closes, end)
