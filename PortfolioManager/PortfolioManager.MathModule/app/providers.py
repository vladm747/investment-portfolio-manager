"""Where prices come from. The maths never calls Yahoo Finance itself; it gets a MarketDataProvider.

``YahooProvider`` is the real one. Tests use a fake (``tests/fakes.py``) through FastAPI's dependency overrides.
"""

from __future__ import annotations

import logging
import threading
import time
from collections.abc import Callable, Sequence
from datetime import date
from typing import Any, Protocol

import pandas as pd

from .config import Settings
from .errors import MarketDataUnavailable, SymbolNotFound

logger = logging.getLogger(__name__)


class MarketDataProvider(Protocol):
    def close_prices(self, symbols: Sequence[str], start: date) -> pd.DataFrame:
        """Adjusted daily closes from ``start``: one column per requested symbol (all NaN when Yahoo has no data),
        a tz-naive DatetimeIndex of trading days at midnight."""
        ...

    def latest_prices(self, symbols: Sequence[str]) -> dict[str, float]:
        """Latest price per symbol; symbols without a price are left out."""
        ...

    def stock_info(self, symbol: str) -> dict[str, Any]:
        """Yahoo's description of one symbol (name, currency, sector, current price, ...)."""
        ...


class TtlCache:
    """A small thread-safe in-memory cache whose entries expire ``ttl_seconds`` after they were stored."""

    def __init__(self, ttl_seconds: float, clock: Callable[[], float] = time.monotonic):
        self._ttl = ttl_seconds
        self._clock = clock
        self._items: dict[str, tuple[float, Any]] = {}
        self._lock = threading.Lock()

    def get(self, key: str) -> Any | None:
        with self._lock:
            item = self._items.get(key)
            if item is None:
                return None
            stored_at, value = item
            if self._clock() - stored_at >= self._ttl:
                del self._items[key]
                return None
            return value

    def set(self, key: str, value: Any) -> None:
        with self._lock:
            self._items[key] = (self._clock(), value)


def _yfinance_download(**kwargs: Any) -> pd.DataFrame:
    import yfinance  # looked up on every call, so tests can block yfinance.download

    return yfinance.download(**kwargs)


def _yfinance_ticker(symbol: str) -> Any:
    import yfinance

    return yfinance.Ticker(symbol)


class YahooProvider:
    """Yahoo Finance through yfinance, with in-memory caches.

    * History: adjusted closes (``auto_adjust=True``), downloaded once per symbol from ``HISTORY_START`` (or an earlier
      requested date) and reused for ``HISTORY_CACHE_SECONDS`` (12 hours by default). Returns are derived from these
      prices; nothing else is downloaded for the optimisation.
    * Latest prices and stock info: reused for ``QUOTE_CACHE_SECONDS`` (5 minutes by default).
    * Symbols missing from a cache are fetched together in one ``yf.download`` call.
    * Empty answers are not cached: Yahoo returns no data both for unknown symbols and when it throttles.
    """

    def __init__(
        self,
        settings: Settings,
        *,
        download: Callable[..., pd.DataFrame] = _yfinance_download,
        ticker: Callable[[str], Any] = _yfinance_ticker,
        clock: Callable[[], float] = time.monotonic,
    ):
        self._download = download
        self._ticker = ticker
        self._history_start = settings.history_start
        self._history = TtlCache(settings.history_cache_seconds, clock)
        self._quotes = TtlCache(settings.quote_cache_seconds, clock)
        self._info = TtlCache(settings.quote_cache_seconds, clock)

    def close_prices(self, symbols: Sequence[str], start: date) -> pd.DataFrame:
        symbols = list(dict.fromkeys(symbols))
        found: dict[str, pd.Series] = {}
        missing: list[str] = []
        for symbol in symbols:
            cached = self._history.get(symbol)
            if cached is not None and cached[0] <= start:
                found[symbol] = cached[1]
            else:
                missing.append(symbol)

        if missing:
            fetch_from = min(start, self._history_start)
            downloaded = self._download_closes(missing, start=fetch_from.isoformat())
            for symbol, series in downloaded.items():
                self._history.set(symbol, (fetch_from, series))
                found[symbol] = series

        empty = pd.Series(dtype=float, index=pd.DatetimeIndex([]))
        frame = pd.concat({symbol: found.get(symbol, empty) for symbol in symbols}, axis=1).sort_index()
        return frame.loc[frame.index >= pd.Timestamp(start)]

    def latest_prices(self, symbols: Sequence[str]) -> dict[str, float]:
        symbols = list(dict.fromkeys(symbols))
        prices: dict[str, float] = {}
        missing: list[str] = []
        for symbol in symbols:
            cached = self._quotes.get(symbol)
            if cached is None:
                missing.append(symbol)
            else:
                prices[symbol] = cached

        if missing:
            # The last few daily bars; during trading hours the last one is today's, at the latest price.
            for symbol, series in self._download_closes(missing, period="5d").items():
                price = float(series.iloc[-1])
                self._quotes.set(symbol, price)
                prices[symbol] = price
        return {symbol: prices[symbol] for symbol in symbols if symbol in prices}

    def stock_info(self, symbol: str) -> dict[str, Any]:
        cached = self._info.get(symbol)
        if cached is not None:
            return cached
        try:
            info = dict(self._ticker(symbol).info or {})
        except Exception as error:
            logger.warning("Yahoo Finance info request for %s failed: %s", symbol, error)
            raise MarketDataUnavailable(f"Could not get information about {symbol} from Yahoo Finance.") from error
        if not (info.get("shortName") or info.get("longName")):
            raise SymbolNotFound(f"Yahoo Finance knows no symbol {symbol}.")
        self._info.set(symbol, info)
        return info

    def _download_closes(self, symbols: list[str], **range_kwargs: Any) -> dict[str, pd.Series]:
        """One yf.download call for all symbols; returns the non-empty close series by symbol."""
        try:
            raw = self._download(
                tickers=symbols,
                interval="1d",
                auto_adjust=True,  # adjusted for splits and dividends; set explicitly, the default has changed before
                actions=False,
                group_by="column",
                multi_level_index=True,  # columns (field, symbol) for one symbol as well as for many
                progress=False,
                threads=True,
                **range_kwargs,
            )
        except Exception as error:
            logger.warning("Yahoo Finance download of %s failed: %s", ", ".join(symbols), error)
            raise MarketDataUnavailable("Could not download prices from Yahoo Finance.") from error

        if raw is None or raw.empty or "Close" not in raw.columns.get_level_values(0):
            return {}
        closes = raw["Close"]
        if isinstance(closes, pd.Series):
            closes = closes.to_frame(name=symbols[0])
        index = pd.DatetimeIndex(closes.index)
        if index.tz is not None:
            index = index.tz_localize(None)
        closes = closes.set_axis(index.normalize(), axis=0)

        result: dict[str, pd.Series] = {}
        for symbol in symbols:
            if symbol in closes.columns:
                series = closes[symbol].dropna().astype(float).sort_index()
                if not series.empty:
                    result[symbol] = series.rename(symbol)
        return result
