"""POST /v1/forecast and GET /v1/forecast/model over a fake provider: the contract the .NET API reads
(PortfolioManager.MathModule.Client/Contract/MathModuleContract.cs), errors, staleness and the API key."""

from __future__ import annotations

from collections.abc import Iterator
from datetime import date, timedelta
from pathlib import Path

import numpy as np
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from app.config import Settings, get_settings
from app.main import app, get_provider
from app.service import FORECAST_HISTORY_DAYS
from tests.fakes import FakeProvider

pytestmark = pytest.mark.allow_hosts(["127.0.0.1", "::1"])

PRICES = Path(__file__).resolve().parent / "data" / "forecast_prices.csv"
METRIC_KEYS = {"value", "low", "high", "previous"}
RISK_KEYS = {"volatility", "valueAtRisk95", "maxDrawdown", "outOfDomain", "outOfDomainFeatures", "staleQuotes"}


@pytest.fixture
def provider() -> FakeProvider:
    return FakeProvider(pd.read_csv(PRICES, index_col="Date", parse_dates=["Date"]))


@pytest.fixture
def client(provider: FakeProvider) -> Iterator[TestClient]:
    app.dependency_overrides[get_provider] = lambda: provider
    app.dependency_overrides[get_settings] = lambda: Settings()
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client
    app.dependency_overrides.clear()


def test_forecast_of_a_portfolio_and_its_stocks(client: TestClient, provider: FakeProvider) -> None:
    response = client.post("/v1/forecast", json={"symbols": ["aaa", "BBB"], "weights": [0.6, 0.4]})

    assert response.status_code == 200
    body = response.json()
    assert set(body) == {"model", "asOf", "tradingDays", "portfolio", "assets", "warnings"}
    assert body["model"] == {
        "version": "risk-v1",
        "trainedThrough": "2026-11-13",
        "horizonDays": 63,
        "intervalLevel": 0.8,
        "ageDays": 0,
        "staleAfterDays": 60,
        "stale": False,
    }
    assert body["asOf"] == "2026-11-13"
    assert body["tradingDays"] == 899
    assert body["warnings"] == []

    portfolio = body["portfolio"]
    assert set(portfolio) == RISK_KEYS
    for key in ("volatility", "valueAtRisk95", "maxDrawdown"):
        metric = portfolio[key]
        assert set(metric) == METRIC_KEYS
        assert 0 < metric["low"] < metric["value"] < metric["high"]
        assert metric["previous"] > 0
    assert portfolio["outOfDomain"] is False and portfolio["outOfDomainFeatures"] == []

    assert [(asset["symbol"], asset["weight"]) for asset in body["assets"]] == [("AAA", 0.6), ("BBB", 0.4)]
    assert all(set(asset["forecast"]) == RISK_KEYS for asset in body["assets"])

    # Stocks, the index and VIX in one request, from about 11 years back (GARCH looks at 10 years).
    symbols, start = provider.calls[0][1]
    assert symbols == ("AAA", "BBB", "^GSPC", "^VIX")
    assert start == date.today() - timedelta(days=FORECAST_HISTORY_DAYS)


def test_the_portfolio_alone(client: TestClient) -> None:
    body = client.post("/v1/forecast", json={"symbols": ["AAA", "BBB"], "weights": [0.6, 0.4], "includeAssets": False})

    assert body.status_code == 200
    assert body.json()["assets"] == []


def test_a_stock_held_twice_is_added_up(client: TestClient) -> None:
    body = client.post("/v1/forecast", json={"symbols": ["AAA", "BBB", "AAA"], "weights": [0.3, 0.4, 0.3]}).json()

    assert [(asset["symbol"], asset["weight"]) for asset in body["assets"]] == [("AAA", 0.6), ("BBB", 0.4)]


def test_an_old_model_is_flagged_stale(client: TestClient, provider: FakeProvider) -> None:
    # The same prices, moved 100 business days later: the model is then about five months old.
    provider.closes = provider.closes.set_axis(provider.closes.index + pd.offsets.BDay(100))

    body = client.post("/v1/forecast", json={"symbols": ["AAA"], "weights": [1]}).json()

    assert body["model"]["stale"] is True
    assert body["model"]["ageDays"] > 60
    assert body["warnings"] == ["model_stale"]


def test_unusual_stocks_are_forecast_with_a_warning(client: TestClient, provider: FakeProvider) -> None:
    closes = provider.closes
    closes["BBB"] = 50 * np.cumprod(1 + 12 * closes["BBB"].pct_change().fillna(0).clip(-0.08, 0.08))
    closes.loc[closes.index[-100:-90], "AAA"] = np.nan  # too long a gap: AAA gets no forecast of its own

    body = client.post("/v1/forecast", json={"symbols": ["AAA", "BBB"], "weights": [0.5, 0.5]}).json()

    assert body["assets"][0]["forecast"] is None
    assert body["assets"][1]["forecast"]["outOfDomain"] is True
    assert "assets_out_of_domain" in body["warnings"] and "assets_without_forecast" in body["warnings"]


@pytest.mark.parametrize(
    ("payload", "status", "code"),
    [
        ({"symbols": ["AAA", "BBB"], "weights": [0.5]}, 400, "invalid_weights"),
        ({"symbols": ["AAA"], "weights": [0.7]}, 400, "invalid_weights"),
        ({"symbols": ["AAA", "ZZZ"], "weights": [0.5, 0.5]}, 404, "symbol_not_found"),
        ({"symbols": [], "weights": []}, 422, "validation_error"),
        ({"symbols": ["AAA"], "weights": [1], "includeAssets": "maybe"}, 422, "validation_error"),
    ],
)
def test_forecast_errors(client: TestClient, payload: dict, status: int, code: str) -> None:
    response = client.post("/v1/forecast", json=payload)

    assert response.status_code == status
    assert response.json()["code"] == code
    assert set(response.json()) == {"code", "message"}


def test_a_recently_listed_stock_is_a_400_naming_it(client: TestClient, provider: FakeProvider) -> None:
    provider.closes.loc[provider.closes.index[:-100], "CCC"] = np.nan

    response = client.post("/v1/forecast", json={"symbols": ["AAA", "CCC"], "weights": [0.5, 0.5]})

    assert response.status_code == 400
    assert response.json()["code"] == "not_enough_history"
    assert "CCC" in response.json()["message"]


def test_no_market_index_is_a_502(client: TestClient, provider: FakeProvider) -> None:
    provider.closes = provider.closes.drop(columns="^VIX")

    response = client.post("/v1/forecast", json={"symbols": ["AAA"], "weights": [1]})

    assert response.status_code == 502
    assert response.json()["code"] == "market_data_unavailable"


def test_model_description(client: TestClient, provider: FakeProvider) -> None:
    response = client.get("/v1/forecast/model")

    assert response.status_code == 200
    body = response.json()
    assert (body["version"], body["horizonDays"], body["trainedThrough"], body["lastSample"]) == (
        "risk-v1",
        63,
        "2026-11-13",
        "2026-08-17",
    )
    assert body["features"][-1] == "vix" and len(body["features"]) == 24
    assert body["trainingStocks"] == 100
    assert body["chosenVariants"]["vol_objective"] == "log"
    assert body["evaluation"]["testStart"] == "2000-01-01"
    assert provider.calls == []  # no market data needed


def test_forecast_needs_the_api_key_when_one_is_set(client: TestClient) -> None:
    app.dependency_overrides[get_settings] = lambda: Settings(api_key="s3cret-for-tests")

    assert client.post("/v1/forecast", json={"symbols": ["AAA"], "weights": [1]}).status_code == 401
    assert client.get("/v1/forecast/model").status_code == 401
    assert client.get("/v1/forecast/model", headers={"X-API-Key": "s3cret-for-tests"}).status_code == 200


def test_openapi_lists_the_forecast(client: TestClient) -> None:
    paths = client.get("/openapi.json").json()["paths"]

    assert {"/v1/forecast", "/v1/forecast/model"} <= set(paths)


def test_forecast_model_dir_setting() -> None:
    assert Settings.from_env({"FORECAST_MODEL_DIR": "/models/risk-202612"}).forecast_model_dir == Path(
        "/models/risk-202612"
    )
    assert Settings.from_env({}).forecast_model_dir.parts[-2:] == ("models", "risk-v1")
