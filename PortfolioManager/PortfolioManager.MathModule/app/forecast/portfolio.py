"""Risk forecast of a portfolio and of each of its stocks. No network access: prices come in as DataFrames.

1. Calendar: the days ^GSPC traded, as in training. A stock's gaps of up to ``MAX_FILL_DAYS`` days (another exchange's
   holidays) carry its previous close; ^VIX carries its previous value for up to 3 days, as in training.
2. Portfolio series: 1 unit invested on the youngest stock's first close and rebalanced daily to the weights,
   P_t = P_(t-1) * (1 + sum_i w_i r_i,t), which is how the system values a portfolio and how the synthetic training
   portfolios were built. A day without a return for some stock counts as 0 for the whole portfolio, as in training.
   The model forecasts this series directly: in the walk-forward test that beat combining the stocks' forecasts
   through their correlations.
3. The portfolio and every stock go through ``RiskModel.forecast`` together (one frame, one column each).
4. Next to each forecast, the same metric over the last H trading days, the naive "the next three months will be like
   the last three" estimate the model was compared with.
"""

from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass
from datetime import date

import numpy as np
import pandas as pd

from ..errors import MarketDataUnavailable, NotEnoughHistory, ServiceError, SymbolNotFound
from .features import ANN, LONGEST_WINDOW
from .model import RiskModel

INDEX_SYMBOL = "^GSPC"
VIX_SYMBOL = "^VIX"
MAX_FILL_DAYS = 5
VIX_FILL_DAYS = 3
# The longest feature window is 252 daily returns, so 253 closes.
MIN_HISTORY_DAYS = LONGEST_WINDOW
PORTFOLIO_COLUMN = "__portfolio__"  # lower case, so it can never be a ticker


class ForecastFailed(ServiceError):
    status_code = 422
    code = "forecast_failed"


@dataclass(frozen=True)
class MetricForecast:
    """A forecast with its 80% interval, and the same metric over the previous H days. None where undefined."""

    value: float | None
    low: float | None
    high: float | None
    previous: float | None


@dataclass(frozen=True)
class RiskForecast:
    """Volatility (annualised), one-day VaR 95% and maximum drawdown over the next H trading days, as fractions."""

    volatility: MetricForecast
    value_at_risk_95: MetricForecast
    max_drawdown: MetricForecast
    out_of_domain: bool
    out_of_domain_features: tuple[str, ...]
    stale_quotes: bool


@dataclass(frozen=True)
class AssetForecast:
    symbol: str
    weight: float
    forecast: RiskForecast | None  # None when the stock's own history is too short or has gaps


@dataclass(frozen=True)
class PortfolioForecast:
    as_of: date
    trading_days: int  # daily returns of the portfolio series
    portfolio: RiskForecast
    assets: list[AssetForecast]


def market_frame(
    closes: pd.DataFrame, symbols: list[str], use_vix: bool
) -> tuple[pd.DataFrame, pd.Series, pd.Series | None]:
    """Stocks, ^GSPC and ^VIX on the ^GSPC calendar. ``closes`` holds the stocks and the two indices as columns."""
    index_close = closes[INDEX_SYMBOL].dropna() if INDEX_SYMBOL in closes else pd.Series(dtype=float)
    if index_close.empty:
        raise MarketDataUnavailable(f"No {INDEX_SYMBOL} prices: the forecast needs the market index.")
    calendar = index_close.index
    stocks = closes.reindex(columns=symbols).reindex(calendar).ffill(limit=MAX_FILL_DAYS)
    vix = None
    if use_vix:
        vix = closes[VIX_SYMBOL].reindex(calendar).ffill(limit=VIX_FILL_DAYS) if VIX_SYMBOL in closes else None
        if vix is None or pd.isna(vix.iloc[-1]):
            raise MarketDataUnavailable(f"No recent {VIX_SYMBOL} value: the model uses it as a feature.")
    return stocks, index_close, vix


def portfolio_prices(stocks: pd.DataFrame, weights: Mapping[str, float]) -> pd.Series:
    """Daily rebalanced value of 1 unit from the youngest stock's first close; NaN before it."""
    w = pd.Series(weights, dtype=float)
    w = w / w.sum()
    prices = stocks[list(w.index)]
    start = max(prices[symbol].first_valid_index() for symbol in w.index)
    first = prices.index.get_loc(start)
    returns = prices.pct_change(fill_method=None).to_numpy()[first + 1 :]
    daily = np.nan_to_num(returns @ w.to_numpy(), nan=0.0)
    values = np.full(len(prices), np.nan)
    values[first] = 1.0
    values[first + 1 :] = np.cumprod(1.0 + daily)
    return pd.Series(values, index=prices.index, name=PORTFOLIO_COLUMN)


def previous_metrics(prices: pd.DataFrame, horizon: int) -> pd.DataFrame:
    """Volatility, VaR 95% and maximum drawdown of each column over its last ``horizon`` daily returns, defined
    exactly as the model's targets (risk_model/targets.py), only backwards. NaN where a close is missing."""
    rows = {}
    for column in prices.columns:
        p = prices[column].to_numpy(dtype=np.float64)[-(horizon + 1) :]
        if p.size < horizon + 1 or not np.isfinite(p).all():
            rows[column] = {"vol": np.nan, "var95": np.nan, "mdd": np.nan}
            continue
        r = p[1:] / p[:-1] - 1
        rel = p / p[0]
        rows[column] = {
            "vol": float(r.std(ddof=1) * np.sqrt(ANN)),
            "var95": float(-np.quantile(r, 0.05)),
            "mdd": float((1 - rel / np.maximum.accumulate(rel)).max()),
        }
    return pd.DataFrame.from_dict(rows, orient="index")


def _number(value: object) -> float | None:
    try:
        number = float(value)  # type: ignore[arg-type]
    except (TypeError, ValueError):
        return None
    return number if np.isfinite(number) else None


def _risk(row: pd.Series, previous: pd.Series) -> RiskForecast:
    def metric(target: str) -> MetricForecast:
        return MetricForecast(
            value=_number(row[target]),
            low=_number(row.get(f"{target}_lo")),
            high=_number(row.get(f"{target}_hi")),
            previous=_number(previous[target]),
        )

    return RiskForecast(
        volatility=metric("vol"),
        value_at_risk_95=metric("var95"),
        max_drawdown=metric("mdd"),
        out_of_domain=bool(row["out_of_domain"]),
        out_of_domain_features=tuple(row["out_of_domain_features"]),
        stale_quotes=bool(row["stale_quotes"]),
    )


def forecast_portfolio(
    model: RiskModel,
    closes: pd.DataFrame,
    weights: Mapping[str, float],
    include_assets: bool = True,
) -> PortfolioForecast:
    """Forecast for the portfolio with these weights (adding up to 1) and, if asked, for each of its stocks.

    ``closes``: adjusted closes of the stocks, ^GSPC and ^VIX, one column each, from about 11 years back.
    """
    symbols = list(weights)
    missing = [symbol for symbol in symbols if symbol not in closes or closes[symbol].dropna().empty]
    if missing:
        raise SymbolNotFound(f"No price history for {', '.join(missing)}.")

    stocks, index_close, vix = market_frame(closes, symbols, model.use_vix)
    held = {symbol: weight for symbol, weight in weights.items() if weight > 0}
    first_dates = {symbol: stocks[symbol].first_valid_index() for symbol in held}
    no_recent = [symbol for symbol, first in first_dates.items() if first is None]
    if no_recent:
        raise SymbolNotFound(f"No prices for {', '.join(no_recent)} on the days the market index traded.")
    youngest = max(first_dates, key=lambda symbol: first_dates[symbol])
    trading_days = int((stocks.index > first_dates[youngest]).sum())
    if trading_days < MIN_HISTORY_DAYS:
        # The .NET API reads the symbol, date and day counts from this text to say it in Ukrainian
        # (PortfolioManager.BLL/Mapping/RiskForecastMapping.cs); it falls back to a general message if the text changes.
        raise NotEnoughHistory(
            f"Not enough price history: {youngest} has prices only from {first_dates[youngest]:%Y-%m-%d}, which gives "
            f"{trading_days} trading days; the forecast needs at least {MIN_HISTORY_DAYS}."
        )

    frame = stocks[symbols].copy() if include_assets else pd.DataFrame(index=stocks.index)
    frame[PORTFOLIO_COLUMN] = portfolio_prices(stocks, held)
    result = model.forecast(frame, index_close, vix)
    previous = previous_metrics(frame, model.horizon)

    as_of = stocks.index[-1].date()
    portfolio_row = result.loc[PORTFOLIO_COLUMN]
    if not portfolio_row["features_ok"]:
        raise ForecastFailed(
            f"The model's features for the portfolio on {as_of} are incomplete; check the price history for gaps."
        )

    assets = []
    if include_assets:
        for symbol in symbols:
            row = result.loc[symbol]
            forecast = _risk(row, previous.loc[symbol]) if row["features_ok"] else None
            assets.append(AssetForecast(symbol=symbol, weight=float(weights[symbol]), forecast=forecast))

    return PortfolioForecast(
        as_of=as_of,
        trading_days=trading_days,
        portfolio=_risk(portfolio_row, previous.loc[PORTFOLIO_COLUMN]),
        assets=assets,
    )
