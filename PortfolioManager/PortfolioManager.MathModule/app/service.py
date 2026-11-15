"""Use cases shared by the v1 and legacy endpoints: fetch prices from the provider, then run the pure maths."""

from __future__ import annotations

import logging
from collections.abc import Sequence
from dataclasses import dataclass
from datetime import date, timedelta

import pandas as pd

from .config import Settings
from .forecast.model import ModelCard, RiskModel
from .forecast.portfolio import INDEX_SYMBOL, VIX_SYMBOL, PortfolioForecast, forecast_portfolio
from .growth import Position, portfolio_value_series
from .optimisation import OptimisationResult, normalise_portfolio, optimise
from .providers import MarketDataProvider

logger = logging.getLogger(__name__)

# GARCH looks at up to 10 years of daily returns and the longest feature at one more year.
FORECAST_HISTORY_DAYS = round(11 * 365.25)


@dataclass(frozen=True)
class ForecastRun:
    forecast: PortfolioForecast
    card: ModelCard
    age_days: int
    stale: bool
    warnings: tuple[str, ...]


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


def run_forecast(
    provider: MarketDataProvider,
    model: RiskModel,
    symbols: Sequence[str],
    weights: Sequence[float],
    include_assets: bool,
    today: date,
) -> ForecastRun:
    """Risk of the portfolio (and of each stock) over the model's horizon, from about 11 years of prices."""
    base_weights = normalise_portfolio(symbols, weights)
    start = today - timedelta(days=FORECAST_HISTORY_DAYS)
    closes = provider.close_prices([*base_weights, INDEX_SYMBOL, VIX_SYMBOL], start=start)
    result = forecast_portfolio(model, closes, base_weights, include_assets)

    card = model.card
    age_days = (result.as_of - card.trained_through).days
    stale = age_days > card.stale_after_days
    warnings = []
    if stale:
        warnings.append("model_stale")
    if result.portfolio.out_of_domain:
        warnings.append("portfolio_out_of_domain")
    if any(asset.forecast is not None and asset.forecast.out_of_domain for asset in result.assets):
        warnings.append("assets_out_of_domain")
    if any(asset.forecast is None for asset in result.assets):
        warnings.append("assets_without_forecast")
    if warnings:
        logger.info("Forecast for %s as of %s: %s", ", ".join(base_weights), result.as_of, ", ".join(warnings))
    return ForecastRun(forecast=result, card=card, age_days=age_days, stale=stale, warnings=tuple(warnings))
