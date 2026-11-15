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
| `Authentication:Google:ClientId` | yes, startup fails without it | user-secrets | App setting `Authentication__Google__ClientId` |
| `Auth:AdminEmails:0`, `:1`, ... | no; nobody is an admin without it | user-secrets | App settings `Auth__AdminEmails__0`, ... |
| `Cors:Origins:0`, `:1`, ... | no, defaults to the two Azure sites | | App settings `Cors__Origins__0`, ... |
| `MathModule:BaseUrl` | yes, startup fails without it | `http://localhost:8011` in `appsettings.Development.json` | App setting `MathModule__BaseUrl`: the private address of the Container App (see [Hosting](#hosting-the-math-service-on-azure)) |
| `MathModule:ApiKey` | must equal `MATH_API_KEY` of the math service; leave unset only while the service has none (never on Azure) | user-secrets | App setting `MathModule__ApiKey` |
| `MathModule:Timeout`, `:RetryCount`, `:RetryDelay` | no, default `00:01:30`, `2`, `00:00:01` | | App settings `MathModule__Timeout`, ... |

### Sign-in: Google only

There are no passwords. The page shows Google's "Sign in with Google" button and sends the ID token it gets to
`POST api/auth/google` `{credential}`. The API checks the token with Google (signature, issuer, expiry and that its
audience is `Authentication:Google:ClientId`; `IGoogleTokenValidator`, which the tests replace) and answers with its own
JWT access token and rotating refresh token, as before. No client secret and no redirect URI are involved, so the
only Google setting is the public client ID.

- **Accounts.** Google's account id (`sub`) is the key, kept in `AspNetUserLogins` (provider `Google`). A first
  sign-in creates an `investor` account (`UserName = Email = the verified Google e-mail`, name from Google, cut to 50
  characters). An older account with the same e-mail (made when passwords existed) is linked to the Google account
  and its password is removed. If that e-mail is already linked to a *different* Google account, the sign-in is refused
  (401): the address has changed hands, and the old owner's data is not handed over. An e-mail Google has not verified
  is refused too.
- **Admins.** An e-mail in `Auth:AdminEmails` gets the `admin` role when it signs in, so the role is already in the first
  access token. It is only ever granted: taking an e-mail off the list does not remove the role. Case is ignored.
- **Switching an account off.** Set `LockoutEnd` of the user far in the future (there is no password to lock out any
  more); sign-in and refresh then answer 401.
- **Local setup.** Create an OAuth client ID of type *Web application* in Google Cloud (authorized JavaScript origins
  `http://localhost:3000`, `http://localhost` and the deployed client; no redirect URIs), then:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<id>.apps.googleusercontent.com" --project PortfolioManager.API
dotnet user-secrets set "Auth:AdminEmails:0" "<your Google e-mail>" --project PortfolioManager.API
```

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

## Run the backend in Docker

`docker-compose.yml` (next to the solution) starts SQL Server 2022 (`db`), the math service (`math`) and the API (`api`).
Nothing in it touches Azure: the database is the container, and user-secrets are not read (the API runs as
Production, so every setting comes from the compose file and `.env`).

```bash
cp .env.example .env     # then fill in the empty values; git ignores .env
docker compose up --build
```

- The API listens on `http://localhost:5050` (`API_PORT`), the address the client's `.env.local` already uses, so
  `npm run dev` keeps working unchanged. Stop a host-run API first.
- The client is not in the compose file. Google sign-in works only from the origins the OAuth client allows
  (`http://localhost:3000`, `http://localhost` and the deployed client), and the page keeps loading from
  `npm run dev` on the first of them. The API only has to let that origin through CORS (`CLIENT_ORIGIN`, default
  `http://localhost:3000`; Production does not add it by itself).
- The math service has no published port: only the API reaches it, with `MATH_API_KEY` as the second lock. Anything
  that needs prices still reaches Yahoo Finance (caches: see [Math service](#math-service)).
- Data lives in the `sqldata` volume. `docker compose down` keeps it, `docker compose down -v` erases it. That is also
  how to change `MSSQL_SA_PASSWORD`, which SQL Server reads only when it first creates the volume.
- To look at the data: `127.0.0.1,14330`, login `sa` (`SQL_PORT`).
- `PortfolioManager.MathModule/docker-compose.yml` still builds the math service alone.

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
| `forecast/` | The risk forecast of the trained model: `features.py` (the model's 24 features, the same code as in training), `garch.py`, `model.py` (loads the model folder), `portfolio.py` (calendar, portfolio series, per-stock forecasts) |
| `providers.py` | `MarketDataProvider` and `YahooProvider` (adjusted closes, in-memory caches) |
| `service.py`, `main.py` | Use cases and the FastAPI app; `legacy.py` keeps the old response shapes |

The maths modules take DataFrames and return plain values; only `providers.py` talks to Yahoo Finance.

### Risk forecast (model risk-v1)

`models/risk-v1` holds the trained model: three LightGBM boosters in LightGBM's text format (`vol.txt`, `var95.txt`,
`mdd.txt`, never a pickle) and `meta.json` (features, horizon, calibration, 80% interval ratios, the feature ranges seen
in training, the walk-forward results). It is trained offline with `train.py --mode final` of the training code, which
is not in this repository yet, and goes into git and the Docker image as it is; the service only reads it. The service loads it once at start and refuses to start
if the folder is missing, incomplete or lists other features than `app/forecast/features.py` computes.

For a portfolio it downloads about 11 years of closes of its stocks, `^GSPC` and `^VIX`, builds the daily-rebalanced
portfolio series on the `^GSPC` calendar and forecasts it directly (in the walk-forward test that beat combining the
stocks' forecasts), then each stock on its own. Over the next 63 trading days: annualised volatility (the model's median
turned into a mean, averaged with GARCH(1,1)), one-day VaR 95% (calibrated so 5% of past forecasts were broken) and
maximum drawdown, each with an 80% interval and the same metric over the last 63 days. `tests/test_forecast.py` checks
the numbers against the training code's own predictions for the same prices (`tests/data/forecast_expected.json`).

- A stock with less than 252 trading days of prices: 400 `not_enough_history` naming it (the API translates it).
- A feature outside the range seen in training, or stale quotes: the forecast is made and flagged `outOfDomain`.
- A model older than `staleAfterDays` (60) from its last training price: `stale: true` and the warning `model_stale`.
  Retrain and replace the folder (or point `FORECAST_MODEL_DIR` at the new one).
- LightGBM needs GNU OpenMP: the Dockerfile installs `libgomp1`.

Run it with `uvicorn app.main:app --port 8011` (or `docker compose up --build`). Settings are environment variables,
all optional (see `app/config.py`): `MATH_API_KEY`, `RISK_FREE_RATE` (annual fraction, default 0, used by the
optimiser and the metrics alike), `HISTORY_START` (default 2010-07-01), `MIN_HISTORY_DAYS` (default 252),
`HISTORY_CACHE_SECONDS` (12 h), `QUOTE_CACHE_SECONDS` (5 min), `FORECAST_MODEL_DIR` (default the bundled
`models/risk-v1`), `LOG_LEVEL`, and two for hosting:
`REQUIRE_API_KEY=true` (the service refuses to start unless `MATH_API_KEY` is set to 32 characters or more) and
`ENABLE_DOCS=false` (no `/docs`, `/redoc` or `/openapi.json`). A start that fails on a bad setting names it in the log.

| Endpoint | Purpose |
|---|---|
| `GET /health` | Liveness; no API key, no Yahoo call |
| `POST /v1/optimisation` | `{symbols, weights}` → period, risk-free rate, base and optimised weights and metrics |
| `POST /v1/growth` | `{positions: [{symbol, quantity, entryDate}]}` → `{points: [{date, value}]}` |
| `POST /v1/prices` | `{symbols}` → `{prices, missing}`, one Yahoo request for all uncached symbols |
| `GET /v1/stocks/{symbol}` | name, currency, sector, price |
| `POST /v1/forecast` | `{symbols, weights, includeAssets?}` → model (version, trained through, age, stale), as-of date, the portfolio's and each stock's volatility, VaR 95% and maximum drawdown (`value`, `low`, `high`, `previous`), `outOfDomain` flags and warnings |
| `GET /v1/forecast/model` | What the loaded model is: features, horizon, training data, chosen variants, walk-forward evaluation |
| `POST /api/get-portfolio-opt`, `POST /api/portfolio-growth`, `GET /yfinance/{symbol}`, `GET /yfinance/{symbol}/price` | Legacy shapes; the .NET API no longer calls them, so they can be removed |

Errors return `{"code", "message"}`: 400 `invalid_weights` / `not_enough_history`, 401 `unauthorized`,
404 `symbol_not_found`, 422 `validation_error` / `optimisation_failed` / `forecast_failed`, 502 `market_data_unavailable`,
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
(`GetStockAsync`, `GetPricesAsync`, `GetGrowthAsync`, `OptimiseAsync`, `ForecastAsync`) over a typed `HttpClient`, with C# records for
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
| `GET api/portfolios/{id}/optimisation` | The portfolio against the max-Sharpe one: nine metrics, each with a `metricKey`, both sets of weights, and `riskForecast` (the forecast risk of both, or null) | 200 |
| `GET api/portfolios/{id}/forecast` | Risk over the next three months from the trained model: volatility, VaR 95% and maximum drawdown with 80% intervals and last-three-months values, for the portfolio and each stock; the losses in money; model age and warnings | 200 |
| `GET api/user/me`, `DELETE api/user/user` | The signed-in user, delete own account | 200 |
| `GET api/admin/users` (admin) | Totals (users, admins, active in the last 7 days, portfolios) and one row per user: id, name, e-mail, roles, number of portfolios, last active time (UTC; from `User.TokenCreated`, null if never signed in). Profile and counts only; read-only | 200 |
| `POST api/auth/google` `{credential}`, `POST api/auth/refresh-token`, `POST api/auth/sign-out` | Sign in with Google's ID token (the only way in), refresh the session, sign out | 200 / 204 |

The routes before this release (`api/portfolio/{id}`, `api/stock/...`, `api/stock/info/{id}` as a GET that wrote data,
`api/statistics/opt|growth/{id}`, and a `POST api/portfolios` that listed) are gone: they answer 404.

## API rules

- **Ownership.** Portfolio, position and statistics endpoints take the user from the access token, never from the
  request. A record of another user is answered with 404, exactly like one that does not exist
  (`NotFoundException`). The repositories have no lookup by id alone: `GetOwnedAsync`, `IsOwnedByAsync` and
  `FindOwnedAsync` always filter by the owner in the SQL `WHERE`.
- **Roles.** `AdminController` (`GET api/admin/users`) is `[Authorize(Roles = "admin")]` (403 for an investor). Everything
  else that needs sign-in only needs a valid token. There is no endpoint that lists users for a non-admin: the old
  `GET api/user/users` is gone.
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
- **Risk forecast** uses the same weights as the optimisation. An empty portfolio is a 400; a stock listed less than a
  year ago is a 400 in Ukrainian naming it (`RiskForecastMapping.TryExplain`, which reads the math service's text). The
  optimisation adds the forecast of the current and the optimised weights (`IncludeAssets: false`, one call after the
  other so the second finds the prices cached) and returns `riskForecast: null` instead of failing when it cannot be made.
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
