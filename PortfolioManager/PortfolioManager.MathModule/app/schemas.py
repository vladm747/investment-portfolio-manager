"""Request and response bodies.

The v1 contract uses camelCase JSON. The legacy models describe what the .NET API sends today
(``YahooFinance.Client`` and ``PortfolioStatisticService``); they go away once the API moves to v1.
"""

from __future__ import annotations

import datetime as dt
from typing import Annotated, Any

from pydantic import BaseModel, ConfigDict, Field, StringConstraints
from pydantic.alias_generators import to_camel

SymbolStr = Annotated[str, StringConstraints(strip_whitespace=True, to_upper=True, min_length=1, max_length=20)]


class ApiModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class ErrorResponse(ApiModel):
    code: str
    message: str


class HealthResponse(ApiModel):
    status: str


class OptimisationRequest(ApiModel):
    symbols: list[SymbolStr] = Field(min_length=1, max_length=100)
    weights: list[float] = Field(min_length=1, max_length=100, description="Current weights, same order as symbols, sum 1.")


class Metrics(ApiModel):
    """All values are fractions (0.12 = 12 %) except the three ratios. null when undefined, e.g. no losing day."""

    cumulative_return: float | None
    cagr: float | None
    sharpe: float | None
    sortino: float | None
    calmar: float | None
    max_drawdown: float | None
    volatility: float | None
    expected_monthly: float | None
    expected_yearly: float | None


class Period(ApiModel):
    start: dt.date
    end: dt.date
    trading_days: int


class WeightsPair(ApiModel):
    base: dict[str, float]
    optimised: dict[str, float]


class MetricsPair(ApiModel):
    base: Metrics
    optimised: Metrics


class OptimisationResponse(ApiModel):
    period: Period
    risk_free_rate: float
    weights: WeightsPair
    metrics: MetricsPair


class PositionIn(ApiModel):
    symbol: SymbolStr
    quantity: float = Field(gt=0)
    entry_date: dt.date


class GrowthRequest(ApiModel):
    positions: list[PositionIn] = Field(max_length=1000)


class GrowthPoint(ApiModel):
    date: dt.date
    value: float


class GrowthResponse(ApiModel):
    points: list[GrowthPoint]


class PricesRequest(ApiModel):
    symbols: list[SymbolStr] = Field(min_length=1, max_length=200)


class PricesResponse(ApiModel):
    prices: dict[str, float]
    missing: list[str]


class StockResponse(ApiModel):
    symbol: str
    name: str | None
    currency: str | None
    sector: str | None
    price: float | None


class ForecastRequest(ApiModel):
    symbols: list[SymbolStr] = Field(min_length=1, max_length=100)
    weights: list[float] = Field(min_length=1, max_length=100, description="Current weights, same order as symbols, sum 1.")
    include_assets: bool = Field(True, description="Also forecast each stock; false forecasts the portfolio only.")


class MetricForecastOut(ApiModel):
    """A fraction (0.18 = 18 %). ``low`` and ``high`` bound the 80% interval; ``previous`` is the same metric over the
    last ``horizonDays`` trading days. null where undefined."""

    value: float | None
    low: float | None
    high: float | None
    previous: float | None


class RiskForecastOut(ApiModel):
    """Over the next ``horizonDays`` trading days: volatility of daily returns (annualised), one-day VaR 95% (a
    positive loss: returns fall below minus this on 5% of the days) and maximum drawdown (positive)."""

    volatility: MetricForecastOut
    value_at_risk_95: MetricForecastOut
    max_drawdown: MetricForecastOut
    out_of_domain: bool = Field(description="A feature is outside the range seen in training, or quotes look stale.")
    out_of_domain_features: list[str]
    stale_quotes: bool


class AssetForecastOut(ApiModel):
    symbol: str
    weight: float
    forecast: RiskForecastOut | None = Field(description="null when the stock's own price history is too short.")


class ForecastModelInfo(ApiModel):
    version: str
    trained_through: dt.date = Field(description="Last price date of the training data.")
    horizon_days: int
    interval_level: float
    age_days: int = Field(description="Days from trainedThrough to asOf.")
    stale_after_days: int
    stale: bool = Field(description="ageDays > staleAfterDays: the model should be trained again on newer data.")


class ForecastResponse(ApiModel):
    model: ForecastModelInfo
    as_of: dt.date = Field(description="Last trading day of the prices the forecast starts from.")
    trading_days: int = Field(description="Daily returns in the portfolio's common history.")
    portfolio: RiskForecastOut
    assets: list[AssetForecastOut]
    warnings: list[str] = Field(
        description="model_stale, portfolio_out_of_domain, assets_out_of_domain, assets_without_forecast."
    )


class ForecastModelResponse(ApiModel):
    version: str
    created: str
    trained_through: dt.date
    last_sample: dt.date = Field(description="Last day whose future was known in training: the last training example.")
    horizon_days: int
    interval_level: float
    stale_after_days: int
    features: list[str]
    training_stocks: int
    training_portfolios: int
    chosen_variants: dict[str, Any]
    evaluation: dict[str, Any] = Field(description="Walk-forward test summary from meta.json (criteria, DM tests).")


class LegacyStock(BaseModel):
    """One position as the .NET StockDTO serialises it (PascalCase). Only these three fields are used."""

    model_config = ConfigDict(extra="ignore")

    Symbol: SymbolStr
    Quantity: float = Field(ge=0)
    EntryDate: dt.datetime


class LegacyOptimisationRequest(BaseModel):
    model_config = ConfigDict(extra="ignore")

    base_weights: list[float] = Field(min_length=1)
    portfolio_symbols: list[SymbolStr] = Field(min_length=1)
