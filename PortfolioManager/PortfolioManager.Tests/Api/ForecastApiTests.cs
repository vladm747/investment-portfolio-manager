using System.Net;
using System.Text.Json;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;
using PortfolioManager.Tests.Services;

namespace PortfolioManager.Tests.Api;

/// <summary>
/// <c>GET api/portfolios/{id}/forecast</c> and the risk forecast on the optimisation, as the React client reads them:
/// the JSON names, the weights the forecast starts from, and the errors a user can see.
/// </summary>
public class ForecastApiTests : ApiTestBase
{
    public ForecastApiTests(ApiFactory api) : base(api) { }

    private static async Task<JsonDocument> JsonOfAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body);
    }

    private async Task<(HttpClient Client, int PortfolioId)> PortfolioWorth1000Async()
    {
        var investor = await Api.SignUpAsync();
        var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 200), ("MSFT", 400));
        var portfolio = await CreatePortfolioAsync(client);
        await AddPositionAsync(client, portfolio.Id, "AAPL", 3, 150); // worth 600
        await AddPositionAsync(client, portfolio.Id, "MSFT", 1, 350); // worth 400
        return (client, portfolio.Id);
    }

    [Fact]
    public async Task The_forecast_of_a_portfolio_in_the_shape_the_client_reads()
    {
        var (client, portfolioId) = await PortfolioWorth1000Async();
        MathModule.Forecast = request => SampleForecast.For(request) with { Warnings = new[] { "model_stale" } };

        using var response = await client.GetAsync($"api/portfolios/{portfolioId}/forecast");
        using var json = await JsonOfAsync(response);

        var root = json.RootElement;
        Assert.Equal(
            new[] { "model", "asOf", "tradingDays", "portfolioValue", "portfolio", "losses", "assets", "warnings" },
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal("risk-v1", root.GetProperty("model").GetProperty("version").GetString());
        Assert.Equal("2026-11-13", root.GetProperty("model").GetProperty("trainedThrough").GetString());
        Assert.Equal(63, root.GetProperty("model").GetProperty("horizonDays").GetInt32());
        Assert.False(root.GetProperty("model").GetProperty("stale").GetBoolean());
        Assert.Equal("2026-11-15", root.GetProperty("asOf").GetString());
        Assert.Equal(1000m, root.GetProperty("portfolioValue").GetDecimal());

        var portfolio = root.GetProperty("portfolio");
        Assert.Equal(
            new[] { "volatility", "valueAtRisk95", "maxDrawdown", "outOfDomain", "outOfDomainFeatures", "staleQuotes" },
            portfolio.EnumerateObject().Select(property => property.Name));
        Assert.Equal(
            new[] { "value", "low", "high", "previous" },
            portfolio.GetProperty("valueAtRisk95").EnumerateObject().Select(property => property.Name));
        Assert.Equal(0.025, portfolio.GetProperty("valueAtRisk95").GetProperty("value").GetDouble());
        Assert.Equal(25m, root.GetProperty("losses").GetProperty("valueAtRisk95").GetProperty("value").GetDecimal());
        Assert.Equal(250m, root.GetProperty("losses").GetProperty("maxDrawdown").GetProperty("high").GetDecimal());

        var assets = root.GetProperty("assets").EnumerateArray().ToList();
        Assert.Equal(new[] { "AAPL", "MSFT" }, assets.Select(asset => asset.GetProperty("symbol").GetString()));
        Assert.Equal(0.6, assets[0].GetProperty("weight").GetDouble());
        Assert.Equal(0.3, assets[0].GetProperty("forecast").GetProperty("volatility").GetProperty("value").GetDouble());
        Assert.Equal(new[] { "model_stale" }, root.GetProperty("warnings").EnumerateArray().Select(w => w.GetString()));

        var request = Assert.Single(MathModule.ForecastCalls);
        Assert.Equal(new[] { 0.6, 0.4 }, request.Weights); // what the positions are worth now, not what was paid
    }

    [Fact]
    public async Task A_stock_without_a_forecast_of_its_own_is_null_not_left_out()
    {
        var (client, portfolioId) = await PortfolioWorth1000Async();
        MathModule.Forecast = request => SampleForecast.For(request) with
        {
            Assets = new[] { new AssetForecast("AAPL", 0.6, null), new AssetForecast("MSFT", 0.4, SampleForecast.Risk()) }
        };

        using var response = await client.GetAsync($"api/portfolios/{portfolioId}/forecast");
        using var json = await JsonOfAsync(response);

        var apple = json.RootElement.GetProperty("assets")[0];
        Assert.Equal(JsonValueKind.Null, apple.GetProperty("forecast").ValueKind);
    }

    [Fact]
    public async Task A_recently_listed_stock_is_a_400_that_names_it_in_Ukrainian()
    {
        var (client, portfolioId) = await PortfolioWorth1000Async();
        MathModule.Forecast = _ => throw new MathModuleRejectedException("not_enough_history",
            "Not enough price history: MSFT has prices only from 2026-06-29, which gives 99 trading days; the forecast needs at least 252.");

        using var response = await client.GetAsync($"api/portfolios/{portfolioId}/forecast");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadErrorAsync(response);
        Assert.Contains("MSFT має ціни лише з 29.06.2026", error.Message);
        Assert.Equal(error.Message, Assert.Single(error.Errors!));
    }

    [Fact]
    public async Task An_empty_portfolio_is_a_400_in_Ukrainian_and_asks_the_math_module_nothing()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);

        using var response = await client.GetAsync($"api/portfolios/{portfolio.Id}/forecast");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("У портфелі немає позицій для прогнозу ризику.", (await ReadErrorAsync(response)).Message);
        Assert.Equal(0, MathModule.Calls);
    }

    [Fact]
    public async Task The_math_module_being_down_is_a_502()
    {
        var (client, portfolioId) = await PortfolioWorth1000Async();
        MathModule.Forecast = _ => throw new MarketDataUnavailableException();

        using var response = await client.GetAsync($"api/portfolios/{portfolioId}/forecast");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task The_optimisation_carries_the_forecast_risk_of_both_portfolios()
    {
        var (client, portfolioId) = await PortfolioWorth1000Async();
        EchoOptimisation(); // the optimised portfolio is equal weights
        MathModule.Forecast = request =>
            SampleForecast.For(request, portfolioVolatility: request.Weights[0] == 0.5 ? 0.15 : 0.2);

        using var response = await client.GetAsync($"api/portfolios/{portfolioId}/optimisation");
        using var json = await JsonOfAsync(response);

        var risk = json.RootElement.GetProperty("riskForecast");
        Assert.Equal("risk-v1", risk.GetProperty("model").GetProperty("version").GetString());
        Assert.Equal(0.2, risk.GetProperty("base").GetProperty("volatility").GetProperty("value").GetDouble());
        Assert.Equal(0.15, risk.GetProperty("optimised").GetProperty("volatility").GetProperty("value").GetDouble());
        Assert.Equal(new[] { 0.6, 0.5 }, MathModule.ForecastCalls.Select(call => call.Weights[0]));
    }

    [Fact]
    public async Task The_optimisation_without_a_forecast_still_answers_with_riskForecast_null()
    {
        var (client, portfolioId) = await PortfolioWorth1000Async();
        EchoOptimisation(); // and the fake's forecast is unavailable

        using var response = await client.GetAsync($"api/portfolios/{portfolioId}/optimisation");
        using var json = await JsonOfAsync(response);

        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("riskForecast").ValueKind);
        Assert.Equal(9, json.RootElement.GetProperty("comparisons").GetArrayLength());
    }
}
