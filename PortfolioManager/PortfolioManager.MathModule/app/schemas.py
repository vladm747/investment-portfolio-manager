"""Request and response bodies.

The v1 contract uses camelCase JSON. The legacy models describe what the .NET API sends today
(``YahooFinance.Client`` and ``PortfolioStatisticService``); they go away once the API moves to v1.
"""

from __future__ import annotations

import datetime as dt
from typing import Annotated

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
