"""YahooProvider with a stand-in for yf.download: what it asks for, how it caches, how it reads the answer."""

from __future__ import annotations

from datetime import date
from typing import Any

import pandas as pd
import pytest

from app.config import Settings
from app.errors import MarketDataUnavailable, SymbolNotFound
from app.providers import TtlCache, YahooProvider
from tests.conftest import NetworkAccessInTests
from tests.fakes import random_walk_prices

FIELDS = ["Close", "High", "Low", "Open", "Volume"]


def yfinance_frame(closes: pd.DataFrame, tz: str | None = None) -> pd.DataFrame:
    """The shape yf.download returns with multi_level_index=True: columns (Price, Ticker)."""
    frame = pd.concat({field: closes for field in FIELDS}, axis=1)
    frame.columns.names = ["Price", "Ticker"]
    frame.index.name = "Date"
    if tz:
        frame.index = frame.index.tz_localize(tz)
    return frame


class FakeDownload:
    def __init__(self, closes: pd.DataFrame, tz: str | None = None):
        self.closes = closes
        self.tz = tz
        self.calls: list[dict[str, Any]] = []

    def __call__(self, **kwargs: Any) -> pd.DataFrame:
        self.calls.append(kwargs)
        known = [symbol for symbol in kwargs["tickers"] if symbol in self.closes.columns]
        # Like yfinance: unknown symbols come back as all-NaN columns.
        frame = self.closes.reindex(columns=kwargs["tickers"])
        if "start" in kwargs:
            frame = frame.loc[frame.index >= pd.Timestamp(kwargs["start"])]
        if kwargs.get("period") == "5d":
            frame = frame.iloc[-5:]
        return yfinance_frame(frame if known else frame.iloc[0:0], self.tz)


class Clock:
    def __init__(self) -> None:
        self.now = 1000.0

    def __call__(self) -> float:
        return self.now


SETTINGS = Settings(history_start=date(2015, 1, 1), history_cache_seconds=12 * 3600, quote_cache_seconds=300)


def provider(download: FakeDownload, clock: Clock | None = None, ticker=None) -> YahooProvider:
    return YahooProvider(SETTINGS, download=download, ticker=ticker or (lambda s: None), clock=clock or Clock())


def test_history_is_downloaded_once_for_all_symbols_with_adjusted_closes() -> None:
    download = FakeDownload(random_walk_prices(["AAA", "BBB"], days=400))

    frame = provider(download).close_prices(["AAA", "BBB"], start=date(2015, 6, 1))

    assert len(download.calls) == 1
    call = download.calls[0]
    assert call["tickers"] == ["AAA", "BBB"]
    assert call["auto_adjust"] is True
    assert call["start"] == "2015-01-01"  # always from HISTORY_START, so later requests reuse it
    assert list(frame.columns) == ["AAA", "BBB"]
    assert frame.index[0] >= pd.Timestamp("2015-06-01")


def test_history_is_reused_for_12_hours_then_downloaded_again() -> None:
    download = FakeDownload(random_walk_prices(["AAA"], days=400))
    clock = Clock()
    yahoo = provider(download, clock)

    yahoo.close_prices(["AAA"], start=date(2015, 3, 1))
    clock.now += 12 * 3600 - 1
    yahoo.close_prices(["AAA"], start=date(2015, 2, 1))
    assert len(download.calls) == 1

    clock.now += 2
    yahoo.close_prices(["AAA"], start=date(2015, 2, 1))
    assert len(download.calls) == 2


def test_only_symbols_missing_from_the_cache_are_downloaded() -> None:
    download = FakeDownload(random_walk_prices(["AAA", "BBB"], days=400))
    yahoo = provider(download)

    yahoo.close_prices(["AAA"], start=date(2015, 3, 1))
    yahoo.close_prices(["AAA", "BBB"], start=date(2015, 3, 1))

    assert [call["tickers"] for call in download.calls] == [["AAA"], ["BBB"]]


def test_an_earlier_start_than_the_cached_history_downloads_again() -> None:
    download = FakeDownload(random_walk_prices(["AAA"], start="2012-01-02", days=1500))
    yahoo = provider(download)

    yahoo.close_prices(["AAA"], start=date(2015, 3, 1))
    frame = yahoo.close_prices(["AAA"], start=date(2013, 1, 1))  # growth of a position bought in 2013

    assert download.calls[1]["start"] == "2013-01-01"
    assert frame.index[0] < pd.Timestamp("2013-01-08")


def test_unknown_symbols_come_back_as_empty_columns_and_are_not_cached() -> None:
    download = FakeDownload(random_walk_prices(["AAA"], days=400))
    yahoo = provider(download)

    frame = yahoo.close_prices(["AAA", "NOPE"], start=date(2015, 1, 1))
    yahoo.close_prices(["NOPE"], start=date(2015, 1, 1))

    assert frame["NOPE"].isna().all()
    assert frame["AAA"].notna().all()
    assert [call["tickers"] for call in download.calls] == [["AAA", "NOPE"], ["NOPE"]]


def test_timezone_aware_dates_become_naive_midnights() -> None:
    download = FakeDownload(random_walk_prices(["AAA"], days=50), tz="America/New_York")

    frame = provider(download).close_prices(["AAA"], start=date(2015, 1, 1))

    assert frame.index.tz is None
    assert (frame.index == frame.index.normalize()).all()


def test_latest_prices_use_one_download_and_a_5_minute_cache() -> None:
    closes = random_walk_prices(["AAA", "BBB", "CCC"], days=30)
    download = FakeDownload(closes)
    clock = Clock()
    yahoo = provider(download, clock)

    first = yahoo.latest_prices(["AAA", "BBB", "NOPE"])
    clock.now += 299
    second = yahoo.latest_prices(["AAA", "BBB"])
    clock.now += 2
    yahoo.latest_prices(["AAA"])

    assert first == {"AAA": closes["AAA"].iloc[-1], "BBB": closes["BBB"].iloc[-1]}
    assert second == first
    assert [(call["tickers"], call["period"], call["auto_adjust"]) for call in download.calls] == [
        (["AAA", "BBB", "NOPE"], "5d", True),
        (["AAA"], "5d", True),
    ]


def test_stock_info_is_cached_and_unknown_symbols_are_not_found() -> None:
    class Ticker:
        def __init__(self, symbol: str):
            self.info = {"shortName": "Apple Inc.", "currency": "USD"} if symbol == "AAPL" else {"trailingPegRatio": None}

    asked: list[str] = []

    def ticker(symbol: str) -> Ticker:
        asked.append(symbol)
        return Ticker(symbol)

    yahoo = provider(FakeDownload(pd.DataFrame()), ticker=ticker)

    assert yahoo.stock_info("AAPL")["shortName"] == "Apple Inc."
    yahoo.stock_info("AAPL")
    with pytest.raises(SymbolNotFound):
        yahoo.stock_info("NOPE")
    assert asked == ["AAPL", "NOPE"]


def test_download_failures_become_market_data_unavailable() -> None:
    def failing(**_: Any) -> pd.DataFrame:
        raise ConnectionError("timed out")

    yahoo = YahooProvider(SETTINGS, download=failing)

    with pytest.raises(MarketDataUnavailable):
        yahoo.close_prices(["AAA"], start=date(2015, 1, 1))


def test_the_default_provider_goes_through_yfinance_which_tests_block() -> None:
    # Proves the guard in conftest.py: the real yfinance.download is never reached from a test.
    with pytest.raises(MarketDataUnavailable) as raised:
        YahooProvider(SETTINGS).close_prices(["AAPL"], start=date(2015, 1, 1))

    assert isinstance(raised.value.__cause__, NetworkAccessInTests)


def test_ttl_cache_expires_entries() -> None:
    clock = Clock()
    cache = TtlCache(10, clock)
    cache.set("k", 1)

    clock.now += 9.9
    assert cache.get("k") == 1
    clock.now += 0.2
    assert cache.get("k") is None
