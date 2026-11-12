# Portfolio Manager: backend

## Configuration and secrets

No secrets are committed. `appsettings*.json` and `launchSettings.json` hold only non-secret defaults.
Locally the API reads its secrets from `dotnet user-secrets` (loaded only when `ASPNETCORE_ENVIRONMENT=Development`,
which both launch profiles set); on Azure it reads App Service settings.

| Setting | Required | Local | Azure App Service |
|---|---|---|---|
| `ConnectionStrings:PortfolioManagerDb` | yes, startup fails without it | user-secrets | Connection string `PortfolioManagerDb`, type SQLAzure |
| `JWT_ISSUER` | yes | user-secrets | App setting `JWT_ISSUER` |
| `JWT_SECRET` | yes, at least 32 bytes | user-secrets | App setting `JWT_SECRET` |
| `JWT_EXPIRES_MINUTES` | no, default 15 | user-secrets | App setting `JWT_EXPIRES_MINUTES` |
| `JWT_REFRESH_EXPIRES_DAYS` | no, default 7 | | App setting `JWT_REFRESH_EXPIRES_DAYS` |
| `Cors:Origins:0`, `:1`, ... | no, defaults to the two Azure sites | | App settings `Cors__Origins__0`, ... |
| `MathModule:BaseUrl` | yes, startup fails without it | `http://localhost:8011` in `appsettings.Development.json` | App setting `MathModule__BaseUrl`: the private address of the Container App (see [Hosting](#hosting-the-math-service-on-azure)) |
| `MathModule:ApiKey` | must equal `MATH_API_KEY` of the math service; leave unset only while the service has none (never on Azure) | user-secrets | App setting `MathModule__ApiKey` |
| `MathModule:Timeout`, `:RetryCount`, `:RetryDelay` | no, default `00:01:30`, `2`, `00:00:01` | | App settings `MathModule__Timeout`, ... |

Sign-in is by e-mail and password only: the API issues its own JWT access and refresh tokens. (There used to be an
unfinished Google sign-in; it never issued a token and is removed, together with its settings.)

Swagger (`/swagger`) exists only when `ASPNETCORE_ENVIRONMENT=Development`; in any other environment it is neither
registered nor served.

Set or change a local value (from `PortfolioManager/`):

```bash
dotnet user-secrets set "JWT_SECRET" "<at least 32 random characters>" --project PortfolioManager.API
dotnet user-secrets list --project PortfolioManager.API
```

Environment variables override user-secrets, for example to run against LocalDB instead of Azure:

```bash
ConnectionStrings__PortfolioManagerDb="Server=(localdb)\MSSQLLocalDB;Database=PortfolioManager_Dev;Trusted_Connection=True;TrustServerCertificate=True" dotnet run --project PortfolioManager.API --launch-profile http
```

The API applies EF Core migrations at startup to whichever database the connection string names.

**Back up before the first start on a database that matters.** `20261110145910_SchemaCleanup` drops the tables
`StockDataHistory` and `PortfolioStatisticForOptimization` and the derived columns `Portfolios.TotalEntryPrice`,
`TotalCurrenPrice`, `TotalGain`, `TotalGainPercentage` and `Stocks.TotalCurrentPrice`, `Gain`, `GainPercentage`
(all of them are worked out from quantity and prices when read, so no information is lost), and turns
`Stocks.EntryPrice` and `CurrentPrice` from `float` into `decimal(18,4)` (rounded to four decimals). Its `Down`
restores the columns and recalculates them. The API and the client changed their routes in the same release
(see [API routes](#api-routes)), so deploy them together.

## Checks

```bash
dotnet test PortfolioManager.sln
```

`ModelSnapshotTests` fails when the EF Core model and the migrations snapshot differ, the same check as
`dotnet ef migrations has-pending-model-changes` (tool manifest in `PortfolioManager.API/.config`).
The roles are seeded with the fixed ids in `PortfolioManager.DAL/Context/RoleSeed.cs`; never change them.

Math service (`PortfolioManager.MathModule`, Python 3.12):

```bash
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements-dev.txt
.venv/Scripts/ruff check .
.venv/Scripts/python -m pytest
```

No test can reach Yahoo Finance: pytest runs with `--disable-socket` (`pyproject.toml`), and `tests/conftest.py`
also blocks yfinance's own HTTP client (curl_cffi), which pytest-socket cannot see. API tests use a fake provider.

CI (`.github/workflows/ci.yml` at the repository root) runs the .NET, Python and client checks and builds the math
service image on pushes to `master` and on pull requests. The .NET job treats warnings as errors
(`TreatWarningsAsErrors`; only the NuGet audit warnings for low and moderate vulnerabilities, NU1901 and NU1902, stay
warnings), and the solution builds with none, so the same command locally shows what CI will say:

```bash
dotnet build PortfolioManager.sln --no-incremental -warnaserror
```

There is no object mapper: the three mappings between entities and DTOs are plain methods in
`PortfolioManager.BLL/Mapping` (`Stock.ToDto()`, `Portfolio.ToDto()`, `User.ToDto(roles)`).

## Math service

`PortfolioManager.MathModule/app`:

| Module | Contents |
|---|---|
| `metrics.py` | The nine comparison metrics with their formulas (checked against quantstats in the tests) |
| `optimisation.py` | Weight validation, common history window, max-Sharpe weights (PyPortfolioOpt) |
| `growth.py` | Daily portfolio value from positions and closes |
| `providers.py` | `MarketDataProvider` and `YahooProvider` (adjusted closes, in-memory caches) |
| `service.py`, `main.py` | Use cases and the FastAPI app; `legacy.py` keeps the old response shapes |

The maths modules take DataFrames and return plain values; only `providers.py` talks to Yahoo Finance.

Run it with `uvicorn app.main:app --port 8011` (or `docker compose up --build`). Settings are environment variables,
all optional (see `app/config.py`): `MATH_API_KEY`, `RISK_FREE_RATE` (annual fraction, default 0, used by the
optimiser and the metrics alike), `HISTORY_START` (default 2010-07-01), `MIN_HISTORY_DAYS` (default 252),
`HISTORY_CACHE_SECONDS` (12 h), `QUOTE_CACHE_SECONDS` (5 min), `LOG_LEVEL`, and two for hosting:
`REQUIRE_API_KEY=true` (the service refuses to start unless `MATH_API_KEY` is set to 32 characters or more) and
`ENABLE_DOCS=false` (no `/docs`, `/redoc` or `/openapi.json`). A start that fails on a bad setting names it in the log.

| Endpoint | Purpose |
|---|---|
| `GET /health` | Liveness; no API key, no Yahoo call |
| `POST /v1/optimisation` | `{symbols, weights}` → period, risk-free rate, base and optimised weights and metrics |
| `POST /v1/growth` | `{positions: [{symbol, quantity, entryDate}]}` → `{points: [{date, value}]}` |
| `POST /v1/prices` | `{symbols}` → `{prices, missing}`, one Yahoo request for all uncached symbols |
| `GET /v1/stocks/{symbol}` | name, currency, sector, price |
| `POST /api/get-portfolio-opt`, `POST /api/portfolio-growth`, `GET /yfinance/{symbol}`, `GET /yfinance/{symbol}/price` | Legacy shapes; the .NET API no longer calls them, so they can be removed |

Errors return `{"code", "message"}`: 400 `invalid_weights` / `not_enough_history`, 401 `unauthorized`,
404 `symbol_not_found`, 422 `validation_error` / `optimisation_failed`, 502 `market_data_unavailable`,
500 `internal_error` (details only in the log). With `MATH_API_KEY` set, every endpoint except `/health` needs
the `X-API-Key` header; the .NET API sends `MathModule:ApiKey` there, so set both to the same value.

## Hosting the math service on Azure

On Azure the math service runs as an Azure Container App in a private network (no public address); the API reaches it
through VNet integration, and the API key is the second lock. `infra/main.bicep` creates the network, registry,
environment and app. Publishing is by hand for now (no workflow deploys to Azure). Step by step, with the checks and
the reasons for Container Apps over App Service for Containers, in [`infra/README.md`](../infra/README.md).
The API's two settings are `MathModule__BaseUrl` and `MathModule__ApiKey`.

## Math service client (.NET)

`PortfolioManager.MathModule.Client` is the only code that talks to the math service: `IMathModuleClient`
(`GetStockAsync`, `GetPricesAsync`, `GetGrowthAsync`, `OptimiseAsync`) over a typed `HttpClient`, with C# records for
the v1 contract (`Contract/MathModuleContract.cs`, mirroring `app/schemas.py`). The BLL services call it; nothing else
makes HTTP calls. Each attempt has a timeout and a 5xx or a refused connection is retried with exponential backoff
(`Microsoft.Extensions.Http.Resilience`); 4xx answers and timeouts are not retried.

Every failure becomes one of three exceptions, which the exception filter (`PortfolioManagerExceptionFilterAttribute`) turns into a response:

| The math service | Exception | API response |
|---|---|---|
| 404 `symbol_not_found` | `SymbolNotFoundException` ("Такий тікер не знайдено") | 400 |
| 400 / 422 with an error body (`not_enough_history`, `optimisation_failed`, ...) | `MathModuleRejectedException` (its code and English message) | 400 |
| unreachable, timeout, 5xx after the retries, 401 (wrong key), a body that breaks the contract | `MarketDataUnavailableException` (generic Ukrainian message, details in the log) | 502 |

Yahoo's sector text ("Financial Services") is mapped explicitly by `SectorMapper`; ETFs and funds, which have none,
are stored as `SectorEnum.Unknown` (11, shown as "Інше"). The old `StockSymbols` whitelist is no longer consulted:
the math service decides whether a ticker exists.

## API routes

All routes are under `api/` and need `Authorization: Bearer <access token>` except `api/auth/*`. Ids in the address
are numbers: `api/portfolios/abc` is a 404.

| Method and route | Purpose | Success |
|---|---|---|
| `GET api/portfolios` | The signed-in user's portfolios, with positions and totals | 200 |
| `POST api/portfolios` | Create a portfolio `{name}` | 201, `Location: /api/portfolios/{id}` |
| `GET api/portfolios/{id}` | One portfolio | 200 |
| `DELETE api/portfolios/{id}` | Delete it with its positions | 204 |
| `GET api/portfolios/{id}/positions` | The positions of a portfolio | 200 |
| `POST api/portfolios/{id}/positions` | Add a position `{symbol, quantity, entryPrice, entryDate}`; the ticker is checked and priced by the math service | 201, `Location: /api/positions/{id}` |
| `POST api/portfolios/{id}/prices/refresh` | Refresh the current prices of every position (one market request, one save; nothing is saved if a price is missing) | 204 |
| `PUT api/positions/{id}` | Change the number of shares `{quantity}` | 200, the position |
| `DELETE api/positions/{id}` | Remove a position | 204 |
| `GET api/portfolios/{id}/growth` | Daily value of the portfolio since its first purchase | 200 |
| `GET api/portfolios/{id}/optimisation` | The portfolio against the max-Sharpe one: nine metrics, each with a `metricKey`, and both sets of weights | 200 |
| `GET api/user/me`, `GET api/user/users` (admin), `DELETE api/user/user` | The signed-in user, all users, delete own account | 200 |
| `POST api/auth/signup`, `signin`, `refresh-token`, `sign-out` | Account and tokens | 200 / 204 |

The routes before this release (`api/portfolio/{id}`, `api/stock/...`, `api/stock/info/{id}` as a GET that wrote data,
`api/statistics/opt|growth/{id}`, and a `POST api/portfolios` that listed) are gone: they answer 404.

## API rules

- **Ownership.** Portfolio, position and statistics endpoints take the user from the access token, never from the
  request. A record of another user is answered with 404, exactly like one that does not exist
  (`NotFoundException`). The repositories have no lookup by id alone: `GetOwnedAsync`, `IsOwnedByAsync` and
  `FindOwnedAsync` always filter by the owner in the SQL `WHERE`.
- **Roles.** `GET api/user/users` is `[Authorize(Roles = "admin")]` (403 for an investor). Everything else that needs
  sign-in only needs a valid token.
- **Valuation.** `PositionValuation` (BLL/Valuation) is the one place that calculates value, gain and gain %, and it
  guards against a zero entry value. Nothing derived is stored: a position keeps its quantity, entry price and current
  price, and its value and gain, like a portfolio's totals, are calculated on every read (`Stock.ToDto()`,
  `Portfolio.ToDto()`), so they cannot go stale.
- **Money** is `decimal`, stored as `decimal(18,4)`. `Money.Round` rounds the way SQL Server does (half away from
  zero), so the number a request returns is the number a later request reads back; a price from the Python service (a
  binary float) goes through `Money.FromMarketPrice`. Gain % is rounded to four decimals. Only the weights sent to the
  optimiser are floating point, because it works in floating point.
- **Optimisation** builds its weights from the live positions (current value per ticker, tickers added up), so it always
  sees the portfolio as it is now. A price refresh makes one price request and one `SaveChanges`.
- **Validation** (`[ApiController]` on every controller): quantity above 0, price above 0, ticker 1 to 10 characters of
  letters, digits, `.` and `-`, entry date not in the future (one day of slack for time zones), portfolio name 1 to 100
  characters. A 400 has the same body as every other error: `{"message": "...", "errors": ["..."]}`; the JSON
  parser's own messages are not passed on.
- **Errors.** Typed exceptions are mapped by the filter (400, 401, 403, 404, 502). Anything else is a 500 whose body
  is a generic message (the real one goes to the log; stack traces only in Development). A request the client
  abandoned (`RequestAborted`) is not an error and is not logged as one. Every async call passes the request's
  `CancellationToken` down to EF Core and to the math service client.
- **Comparison.** Each entry of `GET api/portfolios/{id}/optimisation` carries a stable `metricKey` (`sharpe`,
  `maxDrawdown`, ...) and `lowerIsBetter` next to the Ukrainian `metricName`. The client keys its rules and
  descriptions by `metricKey` and shows `metricName`. For `maxDrawdown`, which is negative, "lower" means smaller in size.

The HTTP tests (`PortfolioManager.Tests/Api`) start the real API from `Program` over EF Core's in-memory database and a
fake math module, so they check these rules end to end without SQL Server, Yahoo or the Python service.
