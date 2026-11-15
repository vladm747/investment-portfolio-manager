using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.Tests.MathModule;

/// <summary>The client against a stub handler: requests it builds, responses it reads, failures it translates.</summary>
public class MathModuleClientTests
{
    private static MathModuleClient ClientFor(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://math.test/") }, NullLogger<MathModuleClient>.Instance);

    private static void AssertJsonEqual(string expected, string? actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual!)), $"Expected {expected}, got {actual}");

    // ---- requests and responses -------------------------------------------------------------------------------

    [Fact]
    public async Task GetStock_calls_v1_stocks_and_reads_the_response()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK,
            """{"symbol":"AAPL","name":"Apple Inc.","currency":"USD","sector":"Technology","price":189.5}""");

        var stock = await ClientFor(handler).GetStockAsync(" AAPL ");

        Assert.Equal(HttpMethod.Get, handler.Single.Method);
        Assert.Equal("/v1/stocks/AAPL", handler.Single.Uri.AbsolutePath);
        Assert.Equal(new StockResponse("AAPL", "Apple Inc.", "USD", "Technology", 189.5), stock);
    }

    [Fact]
    public async Task GetStock_accepts_the_nulls_an_ETF_comes_with()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK,
            """{"symbol":"SPY","name":null,"currency":"USD","sector":null,"price":null}""");

        var stock = await ClientFor(handler).GetStockAsync("SPY");

        Assert.Equal(new StockResponse("SPY", null, "USD", null, null), stock);
    }

    [Fact]
    public async Task GetPrices_posts_the_symbols_and_reads_prices_and_missing()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK, """{"prices":{"AAPL":189.5},"missing":["XXXX"]}""");

        var prices = await ClientFor(handler).GetPricesAsync(new[] { "AAPL", "XXXX" });

        Assert.Equal(HttpMethod.Post, handler.Single.Method);
        Assert.Equal("/v1/prices", handler.Single.Uri.AbsolutePath);
        AssertJsonEqual("""{"symbols":["AAPL","XXXX"]}""", handler.Single.Body);
        Assert.Equal(189.5, prices.Prices["AAPL"]);
        Assert.Equal(new[] { "XXXX" }, prices.Missing);
    }

    [Fact]
    public async Task GetGrowth_posts_camelCase_positions_with_ISO_dates()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK,
            """{"points":[{"date":"2024-05-06","value":1000.5},{"date":"2024-05-07","value":1010}]}""");

        var growth = await ClientFor(handler).GetGrowthAsync(
            new GrowthRequest(new[] { new PositionRequest("AAPL", 3, new DateOnly(2024, 5, 6)) }));

        Assert.Equal("/v1/growth", handler.Single.Uri.AbsolutePath);
        AssertJsonEqual("""{"positions":[{"symbol":"AAPL","quantity":3,"entryDate":"2024-05-06"}]}""", handler.Single.Body);
        Assert.Equal(
            new[] { new GrowthPoint(new DateOnly(2024, 5, 6), 1000.5), new GrowthPoint(new DateOnly(2024, 5, 7), 1010) },
            growth.Points);
    }

    [Fact]
    public async Task Optimise_reads_weights_in_order_and_null_metrics()
    {
        const string response = """
            {
              "period": {"start": "2020-01-02", "end": "2024-05-06", "tradingDays": 1090},
              "riskFreeRate": 0.0,
              "weights": {"base": {"MSFT": 0.4, "aapl": 0.6}, "optimised": {"MSFT": 0.7, "aapl": 0.3}},
              "metrics": {
                "base": {"cumulativeReturn": 1.5, "cagr": 0.2, "sharpe": 1.1, "sortino": null, "calmar": 0.9,
                         "maxDrawdown": -0.3, "volatility": 0.25, "expectedMonthly": 0.01, "expectedYearly": 0.12},
                "optimised": {"cumulativeReturn": 1.8, "cagr": 0.22, "sharpe": 1.3, "sortino": 1.9, "calmar": 1.0,
                              "maxDrawdown": -0.25, "volatility": 0.2, "expectedMonthly": 0.012, "expectedYearly": 0.14}
              }
            }
            """;
        var handler = new StubHandler().Json(HttpStatusCode.OK, response);

        var result = await ClientFor(handler).OptimiseAsync(new OptimisationRequest(new[] { "MSFT", "AAPL" }, new[] { 0.4, 0.6 }));

        Assert.Equal("/v1/optimisation", handler.Single.Uri.AbsolutePath);
        AssertJsonEqual("""{"symbols":["MSFT","AAPL"],"weights":[0.4,0.6]}""", handler.Single.Body);
        Assert.Equal(new Period(new DateOnly(2020, 1, 2), new DateOnly(2024, 5, 6), 1090), result.Period);
        Assert.Equal(new[] { "MSFT", "aapl" }, result.Weights.Optimised.Keys); // order and case kept
        Assert.Equal(0.3, result.Weights.Optimised["aapl"]);
        Assert.Null(result.Metrics.Base.Sortino);
        Assert.Equal(1.9, result.Metrics.Optimised.Sortino);
        Assert.Equal(-0.3, result.Metrics.Base.MaxDrawdown);
    }

    // A real answer of the math service: POST /v1/forecast on tests/data/forecast_prices.csv of the MathModule tests,
    // with ten days of BBB's prices removed, so BBB gets no forecast of its own.
    private const string ForecastAnswer = """
        {"model": {"version": "risk-v1", "trainedThrough": "2026-11-13", "horizonDays": 63, "intervalLevel": 0.8,
                   "ageDays": 0, "staleAfterDays": 60, "stale": false},
         "asOf": "2026-11-13", "tradingDays": 899,
         "portfolio": {"volatility": {"value": 0.19072639451457588, "low": 0.13741224158896861, "high": 0.25199660034255744, "previous": 0.171001216127174},
                       "valueAtRisk95": {"value": 0.020012388234976208, "low": 0.012099241818700555, "high": 0.025487032830466858, "previous": 0.01728680792648556},
                       "maxDrawdown": {"value": 0.1325217168273489, "low": 0.062254510320913205, "high": 0.23663287768678837, "previous": 0.05902844316068523},
                       "outOfDomain": false, "outOfDomainFeatures": [], "staleQuotes": false},
         "assets": [{"symbol": "AAA", "weight": 0.6,
                     "forecast": {"volatility": {"value": 0.2628374499623479, "low": 0.18936594101082546, "high": 0.347273087197991, "previous": 0.23752119495295557},
                                  "valueAtRisk95": {"value": 0.0265174671534612, "low": 0.016032131884611004, "high": 0.03377165923354852, "previous": 0.02068975046045851},
                                  "maxDrawdown": {"value": 0.15501054728138236, "low": 0.07281905144763189, "high": 0.2767892897341909, "previous": 0.11363571123422656},
                                  "outOfDomain": false, "outOfDomainFeatures": [], "staleQuotes": false}},
                    {"symbol": "BBB", "weight": 0.4, "forecast": null}],
         "warnings": ["assets_without_forecast"]}
        """;

    [Fact]
    public async Task Forecast_posts_the_weights_and_reads_the_services_answer()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK, ForecastAnswer);

        var forecast = await ClientFor(handler).ForecastAsync(new ForecastRequest(new[] { "AAA", "BBB" }, new[] { 0.6, 0.4 }));

        Assert.Equal(HttpMethod.Post, handler.Single.Method);
        Assert.Equal("/v1/forecast", handler.Single.Uri.AbsolutePath);
        AssertJsonEqual("""{"symbols":["AAA","BBB"],"weights":[0.6,0.4],"includeAssets":true}""", handler.Single.Body);
        Assert.Equal(new ForecastModelInfo("risk-v1", new DateOnly(2026, 11, 13), 63, 0.8, 0, 60, false), forecast.Model);
        Assert.Equal(new DateOnly(2026, 11, 13), forecast.AsOf);
        Assert.Equal(899, forecast.TradingDays);
        Assert.Equal(
            new MetricForecast(0.020012388234976208, 0.012099241818700555, 0.025487032830466858, 0.01728680792648556),
            forecast.Portfolio.ValueAtRisk95);
        Assert.Equal(0.1325217168273489, forecast.Portfolio.MaxDrawdown.Value);
        Assert.False(forecast.Portfolio.OutOfDomain);
        Assert.Empty(forecast.Portfolio.OutOfDomainFeatures);
        Assert.Equal(new[] { ("AAA", 0.6), ("BBB", 0.4) }, forecast.Assets.Select(asset => (asset.Symbol, asset.Weight)));
        Assert.Equal(0.2628374499623479, forecast.Assets[0].Forecast!.Volatility.Value);
        Assert.Null(forecast.Assets[1].Forecast);
        Assert.Equal(new[] { "assets_without_forecast" }, forecast.Warnings);
    }

    [Fact]
    public async Task Forecast_of_the_portfolio_alone_says_so_and_reads_unusual_stocks_and_missing_figures()
    {
        const string answer = """
            {"model": {"version": "risk-v1", "trainedThrough": "2026-11-13", "horizonDays": 63, "intervalLevel": 0.8,
                       "ageDays": 95, "staleAfterDays": 60, "stale": true},
             "asOf": "2027-02-16", "tradingDays": 300,
             "portfolio": {"volatility": {"value": 0.9, "low": 0.6, "high": 1.2, "previous": null},
                           "valueAtRisk95": {"value": 0.08, "low": null, "high": null, "previous": 0.07},
                           "maxDrawdown": {"value": 0.5, "low": 0.2, "high": 0.9, "previous": 0.4},
                           "outOfDomain": true, "outOfDomainFeatures": ["vol_252", "rel_vol_63"], "staleQuotes": false},
             "assets": [], "warnings": ["model_stale", "portfolio_out_of_domain"]}
            """;
        var handler = new StubHandler().Json(HttpStatusCode.OK, answer);

        var forecast = await ClientFor(handler).ForecastAsync(
            new ForecastRequest(new[] { "GME" }, new[] { 1.0 }, IncludeAssets: false));

        AssertJsonEqual("""{"symbols":["GME"],"weights":[1],"includeAssets":false}""", handler.Single.Body);
        Assert.True(forecast.Model.Stale);
        Assert.Equal(95, forecast.Model.AgeDays);
        Assert.True(forecast.Portfolio.OutOfDomain);
        Assert.Equal(new[] { "vol_252", "rel_vol_63" }, forecast.Portfolio.OutOfDomainFeatures);
        Assert.Null(forecast.Portfolio.Volatility.Previous);
        Assert.Null(forecast.Portfolio.ValueAtRisk95.Low);
        Assert.Empty(forecast.Assets);
        Assert.Equal(new[] { "model_stale", "portfolio_out_of_domain" }, forecast.Warnings);
    }

    [Fact]
    public async Task Forecast_of_a_recently_listed_stock_is_a_refusal_with_the_services_text()
    {
        const string text = "Not enough price history: CCC has prices only from 2026-06-29, which gives 99 trading days; the forecast needs at least 252.";
        var handler = new StubHandler().Error(HttpStatusCode.BadRequest, "not_enough_history", text);

        var error = await Assert.ThrowsAsync<MathModuleRejectedException>(() =>
            ClientFor(handler).ForecastAsync(new ForecastRequest(new[] { "AAA", "CCC" }, new[] { 0.6, 0.4 })));

        Assert.Equal("not_enough_history", error.Code);
        Assert.Equal(text, error.Message);
    }

    // ---- failures ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Unknown_ticker_is_SymbolNotFoundException_with_the_agreed_text()
    {
        var handler = new StubHandler().Error(HttpStatusCode.NotFound, "symbol_not_found", "Yahoo Finance knows no symbol XXXX.");

        var error = await Assert.ThrowsAsync<SymbolNotFoundException>(() => ClientFor(handler).GetStockAsync("XXXX"));

        Assert.Equal("Такий тікер не знайдено", error.Message);
    }

    [Fact]
    public async Task A_404_that_is_not_symbol_not_found_means_a_wrong_address_and_is_unavailable()
    {
        var handler = new StubHandler().Error(HttpStatusCode.NotFound, "not_found", "Not Found");

        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientFor(handler).GetStockAsync("AAPL"));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "not_enough_history")]
    [InlineData(HttpStatusCode.BadRequest, "invalid_weights")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "optimisation_failed")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "validation_error")]
    public async Task A_refusal_keeps_the_services_code_and_message(HttpStatusCode status, string code)
    {
        var handler = new StubHandler().Error(status, code, "the service's text");

        var error = await Assert.ThrowsAsync<MathModuleRejectedException>(() =>
            ClientFor(handler).OptimiseAsync(new OptimisationRequest(new[] { "AAPL" }, new[] { 1.0 })));

        Assert.Equal(code, error.Code);
        Assert.Equal("the service's text", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "unauthorized")]
    [InlineData(HttpStatusCode.InternalServerError, "internal_error")]
    [InlineData(HttpStatusCode.BadGateway, "market_data_unavailable")]
    public async Task Everything_else_is_MarketDataUnavailable_without_the_services_details(HttpStatusCode status, string code)
    {
        var handler = new StubHandler().Error(status, code, "details for the log only");

        var error = await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientFor(handler).GetStockAsync("AAPL"));

        Assert.Equal(MarketDataUnavailableException.DefaultMessage, error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task A_body_that_is_not_the_services_error_is_unavailable(HttpStatusCode status)
    {
        var handler = new StubHandler().Text(status, "<html>proxy error</html>");

        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientFor(handler).GetStockAsync("AAPL"));
    }

    [Theory]
    [InlineData("""{"unexpected": true""")] // cut off
    [InlineData("""{"symbol": 5}""")] // wrong type
    [InlineData("null")]
    public async Task A_success_body_that_breaks_the_contract_is_unavailable(string body)
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK, body);

        var error = await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientFor(handler).GetStockAsync("AAPL"));

        Assert.Equal(MarketDataUnavailableException.DefaultMessage, error.Message);
    }

    [Fact]
    public async Task A_connection_failure_is_unavailable_and_keeps_the_cause()
    {
        var cause = new HttpRequestException("connection refused");
        var handler = new StubHandler().Throw(cause);

        var error = await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientFor(handler).GetStockAsync("AAPL"));

        Assert.Same(cause, error.InnerException);
    }

    [Fact]
    public async Task A_cancelled_caller_gets_OperationCanceledException_not_a_math_module_failure()
    {
        var handler = new StubHandler().Hang();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ClientFor(handler).GetStockAsync("AAPL", cancellation.Token));
    }
}
