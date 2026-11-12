"""Value of a portfolio over time, from its positions and adjusted closes. No network access."""

from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from datetime import date

import pandas as pd


@dataclass(frozen=True)
class Position:
    symbol: str
    quantity: float
    entry_date: date


def portfolio_value_series(positions: Sequence[Position], closes: pd.DataFrame, end: date) -> pd.Series:
    """V(d) = sum over positions bought on or before d of q_i * C_i(d), for every calendar day d from the first purchase to end.

    C_i(d) is the last close of position i's symbol on or before d, so weekends and holidays carry the previous close;
    a position adds 0 until its first close on or after its purchase date, and a symbol with no prices adds 0.
    Current quantities are applied to the whole history: the line shows what this basket was worth, not past trades.
    """
    if not positions:
        return pd.Series(dtype=float)

    days = pd.date_range(min(position.entry_date for position in positions), end, freq="D")
    total = pd.Series(0.0, index=days)
    for position in positions:
        if position.symbol not in closes.columns:
            continue
        prices = closes[position.symbol].dropna().sort_index()
        prices = prices[prices.index >= pd.Timestamp(position.entry_date)]
        if prices.empty:
            continue
        daily = prices.reindex(days, method="ffill").fillna(0.0)
        total = total.add(daily * position.quantity, fill_value=0.0)
    return total
