"""Response shapes of the pre-v1 endpoints, which the .NET API still parses.

``PortfolioStatisticService.GetMetrics`` reads the optimisation response by position (two reports, then the weights)
and looks up every key in ``LEGACY_METRIC_KEYS`` in each report's "Strategy" and "Benchmark"; a missing key throws.
Only the nine metrics in ``LEGACY_NAMES`` reach the comparison screen, plus the period and the risk-free rate, so the
rest are sent as null (the API turns null into 0). The S&P 500 benchmark is no longer downloaded: the API parses the
"Benchmark" column but never shows it. Remove this module when the API calls the v1 endpoints.
"""

from __future__ import annotations

import math
from typing import Any

import pandas as pd

from .optimisation import OptimisationResult

# Every key PortfolioStatisticService.ParseMetrics reads, in its order.
LEGACY_METRIC_KEYS = (
    "Start Period", "End Period", "Risk-Free Rate", "Time in Market", "Cumulative Return", "CAGR﹪", "Sharpe",
    "Prob. Sharpe Ratio", "Sortino", "Max Drawdown", "Longest DD Days", "Volatility (ann.)", "Information Ratio",
    "Calmar", "Expected Daily", "Expected Monthly", "Expected Yearly", "Daily Value-at-Risk", "Max Consecutive Wins",
    "Max Consecutive Losses", "Gain/Pain Ratio", "Gain/Pain (1M)", "Payoff Ratio", "MTD", "3M", "6M", "YTD", "1Y",
    "3Y (ann.)", "5Y (ann.)", "10Y (ann.)", "All-time (ann.)", "Best Day", "Worst Day", "Best Month", "Worst Month",
    "Best Year", "Worst Year", "Avg. Drawdown", "Avg. Drawdown Days", "Recovery Factor", "Avg. Up Month",
    "Avg. Down Month", "Win Days", "Win Month", "Win Quarter", "Win Year",
)  # fmt: skip

# Our metric names -> the quantstats labels the API looks up.
LEGACY_NAMES = {
    "cumulative_return": "Cumulative Return",
    "cagr": "CAGR﹪",
    "sharpe": "Sharpe",
    "sortino": "Sortino",
    "calmar": "Calmar",
    "max_drawdown": "Max Drawdown",
    "volatility": "Volatility (ann.)",
    "expected_monthly": "Expected Monthly",
    "expected_yearly": "Expected Yearly",
}


def finite_or_none(value: float | None) -> float | None:
    """JSON has no NaN or infinity; undefined metrics are sent as null."""
    if value is None or not math.isfinite(value):
        return None
    return float(value)


def _report(values: dict[str, float] | None, result: OptimisationResult) -> dict[str, Any]:
    report: dict[str, Any] = dict.fromkeys(LEGACY_METRIC_KEYS)
    report["Start Period"] = result.start.isoformat()
    report["End Period"] = result.end.isoformat()
    report["Risk-Free Rate"] = result.risk_free_rate
    for name, label in LEGACY_NAMES.items():
        report[label] = finite_or_none(values[name]) if values else None
    return report


def optimisation_response(result: OptimisationResult) -> list[dict[str, Any]]:
    """[current vs (no benchmark), optimised vs current, weights]: the order the API reads them in.

    BaseWeights is a list in the order of OptimizedWeights' keys; the client pairs them by position.
    """
    return [
        {"Strategy": _report(result.base_metrics, result), "Benchmark": _report(None, result)},
        {"Strategy": _report(result.optimised_metrics, result), "Benchmark": _report(result.base_metrics, result)},
        {
            "Weights": {
                "BaseWeights": [result.base_weights[symbol] for symbol in result.optimised_weights],
                "OptimizedWeights": result.optimised_weights,
            }
        },
    ]


def growth_response(series: pd.Series) -> dict[str, float]:
    """{"2024-01-02T00:00:00": value, ...}, the format the API deserialises into Dictionary<DateTime, decimal>."""
    return {f"{day:%Y-%m-%d}T00:00:00": float(value) for day, value in series.items()}
