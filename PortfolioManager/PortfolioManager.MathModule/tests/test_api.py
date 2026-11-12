"""The HTTP API over a fake provider: v1 and legacy shapes, API key, error bodies, health.

TestClient runs the app in-process; no server is started. Its event loop needs a local socket pair
(on Windows built from 127.0.0.1 sockets), so these tests allow sockets to localhost only.
"""

from __future__ import annotations

from collections.abc import Iterator
from datetime import date, timedelta

import numpy as np
import pytest
from fastapi.testclient import TestClient

from app.config import Settings, get_settings
from app.legacy import LEGACY_METRIC_KEYS
from app.main import app, create_app, get_provider
from app.metrics import METRIC_NAMES
from tests.fakes import FakeProvider, random_walk_prices

pytestmark = pytest.mark.allow_hosts(["127.0.0.1", "::1"])

SYMBOLS = ["AAA", "BBB", "CCC"]


@pytest.fixture
def provider() -> FakeProvider:
    closes = random_walk_prices(SYMBOLS, start="2018-01-01", days=1200)
    infos = {"AAA": {"shortName": "Aaa Corp", "currency": "USD", "sector": "Technology", "currentPrice": 12.5}}
    return FakeProvider(closes, infos)


@pytest.fixture
def settings() -> Settings:
    return Settings(history_start=date(2010, 7, 1), risk_free_rate=0.02)


@pytest.fixture
def client(provider: FakeProvider, settings: Settings) -> Iterator[TestClient]:
    app.dependency_overrides[get_provider] = lambda: provider
    app.dependency_overrides[get_settings] = lambda: settings
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client
    app.dependency_overrides.clear()


# ---- health and auth ----------------------------------------------------------------------------------------------


def test_health_needs_no_key_and_no_market_data(client: TestClient, provider: FakeProvider) -> None:
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}
    assert provider.calls == []


@pytest.mark.parametrize("headers", [{}, {"X-API-Key": "wrong"}])
def test_api_key_is_required_when_configured(client: TestClient, settings: Settings, headers: dict) -> None:
    app.dependency_overrides[get_settings] = lambda: Settings(api_key="s3cret-for-tests")

    for path in ("/v1/stocks/AAA", "/yfinance/AAA/price"):
        response = client.get(path, headers=headers)
        assert response.status_code == 401
        assert response.json()["code"] == "unauthorized"
    assert client.get("/health").status_code == 200


def test_right_api_key_is_accepted(client: TestClient) -> None:
    app.dependency_overrides[get_settings] = lambda: Settings(api_key="s3cret-for-tests")

    response = client.get("/v1/stocks/AAA", headers={"X-API-Key": "s3cret-for-tests"})

    assert response.status_code == 200


# ---- v1 -----------------------------------------------------------------------------------------------------------


def test_v1_optimisation(client: TestClient) -> None:
    response = client.post("/v1/optimisation", json={"symbols": ["aaa", "BBB", "CCC"], "weights": [0.5, 0.3, 0.2]})

    assert response.status_code == 200
    body = response.json()
    assert body["riskFreeRate"] == 0.02
    assert body["period"]["start"] == "2018-01-02"
    assert body["period"]["tradingDays"] == 1199
    assert body["weights"]["base"] == {"AAA": 0.5, "BBB": 0.3, "CCC": 0.2}
    assert set(body["weights"]["optimised"]) == set(SYMBOLS)
    assert sum(body["weights"]["optimised"].values()) == pytest.approx(1.0)
    expected_keys = {"cumulativeReturn", "cagr", "sharpe", "sortino", "calmar", "maxDrawdown", "volatility",
                     "expectedMonthly", "expectedYearly"}  # fmt: skip
    assert len(expected_keys) == len(METRIC_NAMES)
    assert set(body["metrics"]["base"]) == expected_keys
    assert set(body["metrics"]["optimised"]) == expected_keys


@pytest.mark.parametrize(
    ("payload", "status", "code"),
    [
        ({"symbols": ["AAA", "BBB"], "weights": [0.5]}, 400, "invalid_weights"),
        ({"symbols": ["AAA", "BBB"], "weights": [0.5, 0.6]}, 400, "invalid_weights"),
        ({"symbols": ["AAA", "ZZZ"], "weights": [0.5, 0.5]}, 404, "symbol_not_found"),
        ({"symbols": [], "weights": []}, 422, "validation_error"),
        ({"weights": [1.0]}, 422, "validation_error"),
    ],
)
def test_v1_optimisation_errors(client: TestClient, payload: dict, status: int, code: str) -> None:
    response = client.post("/v1/optimisation", json=payload)

    assert response.status_code == status
    assert response.json()["code"] == code
    assert set(response.json()) == {"code", "message"}


def test_not_enough_history_is_a_400(client: TestClient, provider: FakeProvider) -> None:
    provider.closes.loc[provider.closes.index < provider.closes.index[-100], "CCC"] = np.nan

    response = client.post("/v1/optimisation", json={"symbols": SYMBOLS, "weights": [0.4, 0.3, 0.3]})

    assert response.status_code == 400
    assert response.json()["code"] == "not_enough_history"
    assert "CCC" in response.json()["message"]


def test_v1_growth(client: TestClient, provider: FakeProvider) -> None:
    entry = provider.closes.index[-10].date()

    response = client.post("/v1/growth", json={"positions": [{"symbol": "AAA", "quantity": 3, "entryDate": str(entry)}]})

    assert response.status_code == 200
    points = response.json()["points"]
    assert points[0] == {"date": str(entry), "value": pytest.approx(3 * provider.closes.loc[str(entry), "AAA"])}
    assert points[-1]["date"] == str(date.today())
    assert len(points) == (date.today() - entry).days + 1


def test_v1_growth_of_nothing_is_empty(client: TestClient) -> None:
    assert client.post("/v1/growth", json={"positions": []}).json() == {"points": []}


def test_v1_prices_reports_missing_symbols(client: TestClient, provider: FakeProvider) -> None:
    response = client.post("/v1/prices", json={"symbols": ["AAA", "BBB", "NOPE", "AAA"]})

    assert response.status_code == 200
    assert response.json() == {
        "prices": {"AAA": provider.closes["AAA"].iloc[-1], "BBB": provider.closes["BBB"].iloc[-1]},
        "missing": ["NOPE"],
    }
    assert provider.calls == [("latest_prices", ("AAA", "BBB", "NOPE"))]


def test_v1_stock(client: TestClient) -> None:
    response = client.get("/v1/stocks/aaa")

    assert response.json() == {"symbol": "AAA", "name": "Aaa Corp", "currency": "USD", "sector": "Technology", "price": 12.5}
    assert client.get("/v1/stocks/NOPE").status_code == 404


# ---- legacy (what the .NET API parses today) ---------------------------------------------------------------------


def test_legacy_optimisation_keeps_the_shape_the_dotnet_api_reads(client: TestClient) -> None:
    response = client.post(
        "/api/get-portfolio-opt",
        json={"base_weights": [0.2, 0.5, 0.3], "portfolio_symbols": ["AAA", "BBB", "AAA"]},  # AAA bought twice
    )

    assert response.status_code == 200
    base_report, optimised_report, weights = response.json()
    for report in (base_report, optimised_report):
        for column in ("Strategy", "Benchmark"):
            assert tuple(report[column]) == LEGACY_METRIC_KEYS  # every key ParseMetrics looks up
            assert isinstance(report[column]["Start Period"], str)
            assert isinstance(report[column]["End Period"], str)
    assert isinstance(base_report["Strategy"]["Sharpe"], float)
    assert base_report["Strategy"]["Risk-Free Rate"] == 0.02
    optimised = weights["Weights"]["OptimizedWeights"]
    assert list(optimised) == ["AAA", "BBB"]
    assert weights["Weights"]["BaseWeights"] == pytest.approx([0.5, 0.5])  # same order as OptimizedWeights


def test_legacy_growth_takes_the_dotnet_stock_dto(client: TestClient, provider: FakeProvider) -> None:
    entry = provider.closes.index[-3]
    stock = {
        "Id": 1, "Symbol": "BBB", "Name": "Bbb", "Quantity": 2, "EntryPrice": 1.0, "CurrentPrice": 1.0,
        "TotalCurrentPrice": 2.0, "Gain": 0.0, "GainPercentage": 0.0, "EntryDate": f"{entry:%Y-%m-%d}T00:00:00",
        "Currency": "USD", "Sector": 0, "PortfolioId": 1,
    }  # fmt: skip

    response = client.post("/api/portfolio-growth", json=[stock])

    assert response.status_code == 200
    body = response.json()
    first_key = f"{entry:%Y-%m-%d}T00:00:00"
    assert next(iter(body)) == first_key
    assert body[first_key] == pytest.approx(2 * provider.closes.loc[entry, "BBB"])
    assert f"{date.today():%Y-%m-%d}T00:00:00" in body


def test_legacy_stock_info_and_price(client: TestClient, provider: FakeProvider) -> None:
    assert client.get("/yfinance/AAA").json()["shortName"] == "Aaa Corp"
    assert client.get("/yfinance/AAA/price").json() == {"symbol": "AAA", "price": provider.closes["AAA"].iloc[-1]}
    assert client.get("/yfinance/NOPE/price").status_code == 404


# ---- errors -------------------------------------------------------------------------------------------------------


def test_unexpected_errors_hide_the_traceback(client: TestClient, provider: FakeProvider) -> None:
    def broken(*_args, **_kwargs):
        raise RuntimeError("secret internal detail")

    provider.latest_prices = broken  # type: ignore[method-assign]

    response = client.post("/v1/prices", json={"symbols": ["AAA"]})

    assert response.status_code == 500
    assert response.json() == {"code": "internal_error", "message": "Unexpected error in the math service; see its log."}


def test_unknown_route_uses_the_error_shape(client: TestClient) -> None:
    response = client.get("/nope")

    assert response.status_code == 404
    assert response.json()["code"] == "not_found"


def test_openapi_lists_v1_legacy_and_health(client: TestClient) -> None:
    paths = client.get("/openapi.json").json()["paths"]

    assert {"/health", "/v1/optimisation", "/v1/growth", "/v1/prices", "/v1/stocks/{symbol}"} <= set(paths)
    assert {"/api/get-portfolio-opt", "/api/portfolio-growth", "/yfinance/{symbol}", "/yfinance/{symbol}/price"} <= set(paths)
    assert paths["/api/get-portfolio-opt"]["post"]["deprecated"] is True


def test_settings_from_environment() -> None:
    settings = Settings.from_env({"RISK_FREE_RATE": "0.04", "MATH_API_KEY": " k ", "HISTORY_START": "2012-01-01"})

    assert (settings.risk_free_rate, settings.api_key, settings.history_start) == (0.04, "k", date(2012, 1, 1))
    assert Settings.from_env({}) == Settings()
    with pytest.raises(ValueError, match="RISK_FREE_RATE"):
        Settings.from_env({"RISK_FREE_RATE": "4"})


def test_require_api_key_refuses_to_start_without_a_long_enough_key() -> None:
    with pytest.raises(ValueError, match="REQUIRE_API_KEY"):
        Settings.from_env({"REQUIRE_API_KEY": "true"})
    with pytest.raises(ValueError, match="at least 32"):
        Settings.from_env({"REQUIRE_API_KEY": "true", "MATH_API_KEY": "too-short"})

    key = "k" * 32
    settings = Settings.from_env({"REQUIRE_API_KEY": "true", "MATH_API_KEY": key, "ENABLE_DOCS": "false"})
    assert (settings.require_api_key, settings.api_key, settings.enable_docs) == (True, key, False)
    # Off by default, so a laptop needs no configuration.
    assert Settings.from_env({"MATH_API_KEY": "short"}).api_key == "short"


@pytest.mark.parametrize("name", ["REQUIRE_API_KEY", "ENABLE_DOCS"])
def test_a_flag_must_be_true_or_false(name: str) -> None:
    with pytest.raises(ValueError, match=name):
        Settings.from_env({name: "maybe"})


def test_docs_can_be_switched_off() -> None:
    with TestClient(create_app(enable_docs=False), raise_server_exceptions=False) as hosted:
        assert hosted.get("/health").status_code == 200
        for path in ("/docs", "/redoc", "/openapi.json"):
            assert hosted.get(path).status_code == 404
    with TestClient(create_app(), raise_server_exceptions=False) as local:
        assert local.get("/docs").status_code == 200


def test_dates_in_growth_are_calendar_days(client: TestClient, provider: FakeProvider) -> None:
    friday = next(day for day in provider.closes.index[-30:] if day.dayofweek == 4)

    body = client.post(
        "/v1/growth", json={"positions": [{"symbol": "AAA", "quantity": 1, "entryDate": f"{friday:%Y-%m-%d}"}]}
    ).json()
    values = {point["date"]: point["value"] for point in body["points"]}

    saturday = (friday.date() + timedelta(days=1)).isoformat()
    assert values[saturday] == values[f"{friday:%Y-%m-%d}"]
