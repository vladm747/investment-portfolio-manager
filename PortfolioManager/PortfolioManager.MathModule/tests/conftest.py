"""Shared test setup. No test may reach Yahoo Finance or any other host.

pytest runs with --disable-socket (pyproject.toml), which blocks Python sockets. yfinance, however, makes its HTTP
requests through curl_cffi, a binding to libcurl that opens its sockets in C, where pytest-socket cannot see them.
The autouse fixture below therefore also replaces yfinance's entry points and both HTTP session classes with a
function that fails the test.
"""

from __future__ import annotations

import pytest


class NetworkAccessInTests(RuntimeError):
    pass


def _blocked(*_args, **_kwargs):
    raise NetworkAccessInTests("A test tried to reach the network (Yahoo Finance or another host).")


@pytest.fixture(autouse=True)
def no_market_data_requests(monkeypatch: pytest.MonkeyPatch) -> None:
    import requests
    import yfinance

    monkeypatch.setattr(yfinance, "download", _blocked)
    monkeypatch.setattr(yfinance, "Ticker", _blocked)
    monkeypatch.setattr(requests.Session, "request", _blocked)
    try:
        from curl_cffi import requests as curl_requests
    except ImportError:  # yfinance falls back to requests without curl_cffi
        return
    monkeypatch.setattr(curl_requests.Session, "request", _blocked)
    monkeypatch.setattr(curl_requests.AsyncSession, "request", _blocked)
