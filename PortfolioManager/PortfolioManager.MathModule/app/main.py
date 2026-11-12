"""HTTP API of the math service.

Run locally:  uvicorn app.main:app --port 8011
Endpoints are plain ``def`` functions: FastAPI runs them in its thread pool, so the blocking yfinance and
optimiser calls do not stall the event loop.
"""

from __future__ import annotations

import logging
import secrets
import sys
from datetime import date
from functools import lru_cache
from typing import Annotated, Any

from fastapi import APIRouter, Depends, FastAPI, Header, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from starlette.exceptions import HTTPException as StarletteHTTPException

from . import legacy
from .config import Settings, get_settings
from .errors import ServiceError, SymbolNotFound, Unauthorized
from .growth import Position
from .optimisation import OptimisationResult
from .providers import MarketDataProvider, YahooProvider
from .schemas import (
    ErrorResponse,
    GrowthPoint,
    GrowthRequest,
    GrowthResponse,
    HealthResponse,
    LegacyOptimisationRequest,
    LegacyStock,
    Metrics,
    MetricsPair,
    OptimisationRequest,
    OptimisationResponse,
    Period,
    PricesRequest,
    PricesResponse,
    StockResponse,
    WeightsPair,
)
from .service import run_growth, run_optimisation

logger = logging.getLogger("app")


def configure_logging(level: str) -> None:
    """The one logging configuration for the service: plain text to stdout, where Docker and Azure collect it."""
    logging.basicConfig(
        stream=sys.stdout,
        format="%(asctime)s - %(levelname)s - %(name)s - %(message)s",
        level=level,
    )


@lru_cache
def get_provider() -> MarketDataProvider:
    """One provider for the whole process, so its caches are shared. Tests replace it with a fake."""
    return YahooProvider(get_settings())


SettingsDep = Annotated[Settings, Depends(get_settings)]
ProviderDep = Annotated[MarketDataProvider, Depends(get_provider)]


def require_api_key(
    settings: SettingsDep, x_api_key: Annotated[str | None, Header(include_in_schema=False)] = None
) -> None:
    """Callers send the shared key in X-API-Key. With MATH_API_KEY unset, every call is accepted."""
    if settings.api_key is None:
        return
    if x_api_key is None or not secrets.compare_digest(x_api_key.encode(), settings.api_key.encode()):
        raise Unauthorized("Missing or wrong X-API-Key header.")


ERROR_RESPONSES: dict[int | str, dict[str, Any]] = {
    status: {"model": ErrorResponse} for status in (400, 401, 404, 422, 502)
}

health_router = APIRouter(tags=["health"])
v1_router = APIRouter(prefix="/v1", tags=["v1"], dependencies=[Depends(require_api_key)], responses=ERROR_RESPONSES)
legacy_router = APIRouter(
    tags=["legacy (used by the .NET API until it moves to v1)"],
    dependencies=[Depends(require_api_key)],
    deprecated=True,
)


@health_router.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    """Liveness only: does not call Yahoo Finance and needs no API key."""
    return HealthResponse(status="ok")


# ---- v1 ---------------------------------------------------------------------------------------------------------


def _metrics(values: dict[str, float]) -> Metrics:
    return Metrics(**{name: legacy.finite_or_none(value) for name, value in values.items()})


def _optimisation_response(result: OptimisationResult) -> OptimisationResponse:
    return OptimisationResponse(
        period=Period(start=result.start, end=result.end, trading_days=result.trading_days),
        risk_free_rate=result.risk_free_rate,
        weights=WeightsPair(base=result.base_weights, optimised=result.optimised_weights),
        metrics=MetricsPair(base=_metrics(result.base_metrics), optimised=_metrics(result.optimised_metrics)),
    )


@v1_router.post("/optimisation", response_model=OptimisationResponse)
def optimisation(body: OptimisationRequest, provider: ProviderDep, settings: SettingsDep) -> OptimisationResponse:
    """Max-Sharpe weights for the given symbols, and the nine metrics of the current and the optimised portfolio."""
    return _optimisation_response(run_optimisation(provider, settings, body.symbols, body.weights))


@v1_router.post("/growth", response_model=GrowthResponse)
def growth(body: GrowthRequest, provider: ProviderDep) -> GrowthResponse:
    """Daily value of the positions from the first purchase to today."""
    positions = [Position(p.symbol, p.quantity, p.entry_date) for p in body.positions]
    series = run_growth(provider, positions, end=date.today())
    return GrowthResponse(points=[GrowthPoint(date=day.date(), value=float(value)) for day, value in series.items()])


@v1_router.post("/prices", response_model=PricesResponse)
def prices(body: PricesRequest, provider: ProviderDep) -> PricesResponse:
    """Latest prices for many symbols at once (one Yahoo request for all symbols not in the cache)."""
    symbols = list(dict.fromkeys(body.symbols))
    found = provider.latest_prices(symbols)
    return PricesResponse(prices=found, missing=[symbol for symbol in symbols if symbol not in found])


@v1_router.get("/stocks/{symbol}", response_model=StockResponse)
def stock(symbol: str, provider: ProviderDep) -> StockResponse:
    """Name, currency, sector and current price of one symbol."""
    symbol = symbol.strip().upper()
    info = provider.stock_info(symbol)
    price = info.get("currentPrice") or info.get("regularMarketPrice")
    return StockResponse(
        symbol=symbol,
        name=info.get("shortName") or info.get("longName"),
        currency=info.get("currency"),
        sector=info.get("sector"),
        price=float(price) if price is not None else None,
    )


# ---- legacy -----------------------------------------------------------------------------------------------------


@legacy_router.post("/api/get-portfolio-opt")
def legacy_optimisation(body: LegacyOptimisationRequest, provider: ProviderDep, settings: SettingsDep) -> list[Any]:
    result = run_optimisation(provider, settings, body.portfolio_symbols, body.base_weights)
    return legacy.optimisation_response(result)


@legacy_router.post("/api/portfolio-growth")
def legacy_growth(stocks: list[LegacyStock], provider: ProviderDep) -> dict[str, float]:
    positions = [Position(s.Symbol, s.Quantity, s.EntryDate.date()) for s in stocks]
    return legacy.growth_response(run_growth(provider, positions, end=date.today()))


@legacy_router.get("/yfinance/{symbol}")
def legacy_stock_info(symbol: str, provider: ProviderDep) -> dict[str, Any]:
    return provider.stock_info(symbol.strip().upper())


@legacy_router.get("/yfinance/{symbol}/price")
def legacy_stock_price(symbol: str, provider: ProviderDep) -> dict[str, Any]:
    symbol = symbol.strip().upper()
    found = provider.latest_prices([symbol])
    if symbol not in found:
        raise SymbolNotFound(f"No price for {symbol}.")
    return {"symbol": symbol, "price": found[symbol]}


# ---- errors -----------------------------------------------------------------------------------------------------


def _error(status: int, code: str, message: str) -> JSONResponse:
    return JSONResponse(status_code=status, content={"code": code, "message": message})


async def _service_error(_: Request, error: Exception) -> JSONResponse:
    assert isinstance(error, ServiceError)
    return _error(error.status_code, error.code, error.message)


async def _validation_error(_: Request, error: Exception) -> JSONResponse:
    assert isinstance(error, RequestValidationError)
    problems = []
    for item in error.errors():
        location = ".".join(str(part) for part in item.get("loc", ()) if part not in ("body", "query", "path"))
        problems.append(f"{location}: {item.get('msg')}" if location else str(item.get("msg")))
    return _error(422, "validation_error", "; ".join(problems) or "Invalid request.")


async def _http_error(_: Request, error: Exception) -> JSONResponse:
    assert isinstance(error, StarletteHTTPException)
    code = {404: "not_found", 405: "method_not_allowed"}.get(error.status_code, "http_error")
    return _error(error.status_code, code, str(error.detail))


async def _unexpected_error(request: Request, error: Exception) -> JSONResponse:
    # The traceback goes to the log, never to the caller.
    logger.exception("Unhandled error on %s %s", request.method, request.url.path, exc_info=error)
    return _error(500, "internal_error", "Unexpected error in the math service; see its log.")


def create_app(enable_docs: bool = True) -> FastAPI:
    """With ``enable_docs`` false (ENABLE_DOCS=false, as on Azure) /docs, /redoc and /openapi.json do not exist."""
    application = FastAPI(
        title="Portfolio Manager math service",
        version="1.0.0",
        description="Market data from Yahoo Finance, portfolio growth and max-Sharpe optimisation.",
        docs_url="/docs" if enable_docs else None,
        redoc_url="/redoc" if enable_docs else None,
        openapi_url="/openapi.json" if enable_docs else None,
    )
    application.add_exception_handler(ServiceError, _service_error)
    application.add_exception_handler(RequestValidationError, _validation_error)
    application.add_exception_handler(StarletteHTTPException, _http_error)
    application.add_exception_handler(Exception, _unexpected_error)
    application.include_router(health_router)
    application.include_router(v1_router)
    application.include_router(legacy_router)
    return application


settings_at_start = get_settings()
configure_logging(settings_at_start.log_level)
if settings_at_start.api_key is None:
    logger.warning("MATH_API_KEY is not set: every caller is accepted. Set it wherever the service is reachable.")

app = create_app(enable_docs=settings_at_start.enable_docs)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8011, log_level="info")
