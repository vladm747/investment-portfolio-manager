"""Synthetic prices and a fake MarketDataProvider, so tests never need Yahoo Finance."""

from __future__ import annotations

from collections.abc import Sequence
from datetime import date
from typing import Any

import numpy as np
import pandas as pd

from app.errors import SymbolNotFound


def random_walk_prices(
    symbols: Sequence[str],
    start: str = "2015-01-01",
    days: int = 1500,
    seed: int = 7,
    drifts: Sequence[float] | None = None,
    volatilities: Sequence[float] | None = None,
) -> pd.DataFrame:
    """Geometric random walks on business days, starting at 100: one column per symbol."""
    rng = np.random.default_rng(seed)
    index = pd.bdate_range(start, periods=days)
    drifts = drifts if drifts is not None else np.linspace(0.0002, 0.0008, len(symbols))
    volatilities = volatilities if volatilities is not None else np.linspace(0.010, 0.020, len(symbols))
    columns = {}
    for symbol, drift, vol in zip(symbols, drifts, volatilities, strict=True):
        returns = rng.normal(drift, vol, size=days)
        columns[symbol] = 100.0 * np.cumprod(1.0 + returns)
    return pd.DataFrame(columns, index=index)


class FakeProvider:
    """Serves fixed data and records what was asked."""

    def __init__(self, closes: pd.DataFrame | None = None, infos: dict[str, dict[str, Any]] | None = None):
        self.closes = closes if closes is not None else pd.DataFrame()
        self.infos = infos or {}
        self.calls: list[tuple[str, Any]] = []

    def close_prices(self, symbols: Sequence[str], start: date) -> pd.DataFrame:
        self.calls.append(("close_prices", (tuple(symbols), start)))
        frame = self.closes.reindex(columns=list(dict.fromkeys(symbols)))
        return frame.loc[frame.index >= pd.Timestamp(start)]

    def latest_prices(self, symbols: Sequence[str]) -> dict[str, float]:
        self.calls.append(("latest_prices", tuple(symbols)))
        result = {}
        for symbol in symbols:
            if symbol in self.closes.columns and not self.closes[symbol].dropna().empty:
                result[symbol] = float(self.closes[symbol].dropna().iloc[-1])
        return result

    def stock_info(self, symbol: str) -> dict[str, Any]:
        self.calls.append(("stock_info", symbol))
        if symbol not in self.infos:
            raise SymbolNotFound(f"Yahoo Finance knows no symbol {symbol}.")
        return self.infos[symbol]
