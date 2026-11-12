from __future__ import annotations

from datetime import date

import pandas as pd
import pytest

from app.growth import Position, portfolio_value_series


def closes() -> pd.DataFrame:
    # Thursday 2024-01-04 to Tuesday 2024-01-09; no prices at the weekend.
    index = pd.to_datetime(["2024-01-04", "2024-01-05", "2024-01-08", "2024-01-09"])
    return pd.DataFrame({"AAA": [10.0, 11.0, 12.0, 13.0], "BBB": [100.0, 101.0, 102.0, 103.0]}, index=index)


def test_value_is_quantity_times_last_close_with_weekends_carried_forward() -> None:
    positions = [Position("AAA", 2, date(2024, 1, 4)), Position("BBB", 1, date(2024, 1, 8))]

    series = portfolio_value_series(positions, closes(), end=date(2024, 1, 10))

    assert series.index[0] == pd.Timestamp("2024-01-04")
    assert series.index[-1] == pd.Timestamp("2024-01-10")
    assert series.to_dict() == pytest.approx(
        {
            pd.Timestamp("2024-01-04"): 20.0,  # AAA only
            pd.Timestamp("2024-01-05"): 22.0,
            pd.Timestamp("2024-01-06"): 22.0,  # Saturday: Friday's close
            pd.Timestamp("2024-01-07"): 22.0,
            pd.Timestamp("2024-01-08"): 24.0 + 102.0,  # BBB bought
            pd.Timestamp("2024-01-09"): 26.0 + 103.0,
            pd.Timestamp("2024-01-10"): 26.0 + 103.0,  # no close yet today
        }
    )


def test_position_bought_at_the_weekend_counts_from_the_next_close() -> None:
    series = portfolio_value_series([Position("AAA", 1, date(2024, 1, 6))], closes(), end=date(2024, 1, 9))

    assert series[pd.Timestamp("2024-01-06")] == 0.0
    assert series[pd.Timestamp("2024-01-08")] == 12.0


def test_symbol_without_prices_adds_nothing() -> None:
    frame = closes().assign(GONE=float("nan"))
    positions = [Position("AAA", 1, date(2024, 1, 4)), Position("GONE", 5, date(2024, 1, 4))]

    series = portfolio_value_series(positions, frame, end=date(2024, 1, 5))

    assert series.tolist() == [10.0, 11.0]


def test_no_positions_give_an_empty_series() -> None:
    assert portfolio_value_series([], closes(), end=date(2024, 1, 9)).empty
