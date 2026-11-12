using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.Tests.Api;

/// <summary>
/// The numbers the user sees after every change to a position: value, gain and the portfolio's totals are
/// calculated from the positions when they are read, so they cannot be stale; and the optimisation sees the same portfolio.
/// </summary>
public class ValuationApiTests : ApiTestBase
{
    public ValuationApiTests(ApiFactory api) : base(api) { }

    private void PricesAre(double price) =>
        MathModule.Prices = symbols => new(symbols.ToDictionary(s => s.ToUpperInvariant(), _ => price), Array.Empty<string>());

    [Fact]
    public async Task The_totals_are_right_after_every_change_to_a_position()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));
        var portfolio = await CreatePortfolioAsync(client);

        var empty = await GetPortfolioAsync(client, portfolio.Id);
        Assert.Equal((0m, 0m, 0m, 0m), (empty.TotalEntryPrice, empty.TotalCurrentPrice, empty.TotalGain, empty.TotalGainPercentage));

        // bought 2 at 100, worth 150 now
        var position = await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);
        var afterCreate = await GetPortfolioAsync(client, portfolio.Id);
        Assert.Equal(200, afterCreate.TotalEntryPrice);
        Assert.Equal(300, afterCreate.TotalCurrentPrice);
        Assert.Equal(100, afterCreate.TotalGain);
        Assert.Equal(50, afterCreate.TotalGainPercentage);

        // the quantity changes: before, the totals stayed as they were
        var updated = await ReadAsync<StockDTO>(await client.PutAsJsonAsync($"api/positions/{position.Id}", new { quantity = 4 }));
        Assert.Equal(600, updated.TotalCurrentPrice);
        var afterUpdate = await GetPortfolioAsync(client, portfolio.Id);
        Assert.Equal(400, afterUpdate.TotalEntryPrice);
        Assert.Equal(600, afterUpdate.TotalCurrentPrice);
        Assert.Equal(200, afterUpdate.TotalGain);
        Assert.Equal(50, afterUpdate.TotalGainPercentage);

        // the price moves to 200
        PricesAre(200);
        using var refresh = await client.PostAsync($"api/portfolios/{portfolio.Id}/prices/refresh", null);
        Assert.Equal(HttpStatusCode.NoContent, refresh.StatusCode);
        var afterRefresh = await GetPortfolioAsync(client, portfolio.Id);
        Assert.Equal(800, afterRefresh.TotalCurrentPrice);
        Assert.Equal(400, afterRefresh.TotalGain);
        Assert.Equal(100, afterRefresh.TotalGainPercentage);
        Assert.Equal(800, Assert.Single(afterRefresh.Stocks!).TotalCurrentPrice);

        // and the position is sold
        using var removed = await client.DeleteAsync($"api/positions/{position.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        var afterDelete = await GetPortfolioAsync(client, portfolio.Id);
        Assert.Equal((0m, 0m, 0m, 0m), (afterDelete.TotalEntryPrice, afterDelete.TotalCurrentPrice, afterDelete.TotalGain, afterDelete.TotalGainPercentage));
    }

    [Fact]
    public async Task The_portfolio_list_carries_the_same_totals_as_the_single_portfolio()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150), ("MSFT", 330));
        var portfolio = await CreatePortfolioAsync(client);
        await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);
        await AddPositionAsync(client, portfolio.Id, "MSFT", 1, 300);

        var single = await GetPortfolioAsync(client, portfolio.Id);
        var listed = Assert.Single(await ReadAsync<List<PortfolioDTO>>(await client.GetAsync("api/portfolios")));

        Assert.Equal(500, single.TotalEntryPrice);
        Assert.Equal(630, single.TotalCurrentPrice);
        Assert.Equal(single.TotalEntryPrice, listed.TotalEntryPrice);
        Assert.Equal(single.TotalCurrentPrice, listed.TotalCurrentPrice);
        Assert.Equal(single.TotalGain, listed.TotalGain);
        Assert.Equal(single.TotalGainPercentage, listed.TotalGainPercentage);
    }

    [Fact]
    public async Task Refreshing_the_prices_asks_the_market_once_for_the_whole_portfolio()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150), ("MSFT", 330), ("TSLA", 250));
        var portfolio = await CreatePortfolioAsync(client);
        await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);
        await AddPositionAsync(client, portfolio.Id, "MSFT", 1, 300);
        await AddPositionAsync(client, portfolio.Id, "TSLA", 3, 200);
        PricesAre(10);

        using var refresh = await client.PostAsync($"api/portfolios/{portfolio.Id}/prices/refresh", null);

        Assert.Equal(HttpStatusCode.NoContent, refresh.StatusCode);
        Assert.Equal(3, Assert.Single(MathModule.PriceCalls).Count);
        Assert.All(await ReadAsync<List<StockDTO>>(await client.GetAsync($"api/portfolios/{portfolio.Id}/positions")),
            stock => Assert.Equal(10, stock.CurrentPrice));
    }

    [Fact]
    public async Task A_failed_refresh_changes_no_price()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150), ("MSFT", 330));
        var portfolio = await CreatePortfolioAsync(client);
        await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);
        await AddPositionAsync(client, portfolio.Id, "MSFT", 1, 300);
        MathModule.Prices = _ => new(new Dictionary<string, double> { ["AAPL"] = 999 }, new[] { "MSFT" });

        using var refresh = await client.PostAsync($"api/portfolios/{portfolio.Id}/prices/refresh", null);

        Assert.Equal(HttpStatusCode.BadGateway, refresh.StatusCode);
        var stocks = await ReadAsync<List<StockDTO>>(await client.GetAsync($"api/portfolios/{portfolio.Id}/positions"));
        Assert.Equal(new[] { 150m, 330m }, stocks.OrderBy(s => s.Symbol).Select(s => s.CurrentPrice));
    }

    // ---- optimisation -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_optimisation_starts_from_the_portfolio_as_it_is_now()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 100), ("MSFT", 100));
        EchoOptimisation();
        var portfolio = await CreatePortfolioAsync(client);
        var apple = await AddPositionAsync(client, portfolio.Id, "AAPL", 1, 90);
        await AddPositionAsync(client, portfolio.Id, "MSFT", 1, 90);

        using var first = await client.GetAsync($"api/portfolios/{portfolio.Id}/optimisation");
        await client.PutAsJsonAsync($"api/positions/{apple.Id}", new { quantity = 3 }); // the user buys more Apple
        using var second = await client.GetAsync($"api/portfolios/{portfolio.Id}/optimisation");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(new[] { "AAPL", "MSFT" }, MathModule.OptimisationCalls[0].Symbols);
        Assert.Equal(new[] { 0.5, 0.5 }, MathModule.OptimisationCalls[0].Weights);
        Assert.Equal(new[] { 0.75, 0.25 }, MathModule.OptimisationCalls[1].Weights);
    }

    [Fact]
    public async Task Optimising_an_empty_portfolio_is_a_400_in_Ukrainian()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);

        using var response = await client.GetAsync($"api/portfolios/{portfolio.Id}/optimisation");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("У портфелі немає позицій, які можна оптимізувати.", (await ReadErrorAsync(response)).Message);
        Assert.Equal(0, MathModule.Calls);
    }

    [Fact]
    public async Task The_comparison_names_each_metric_by_a_key_and_says_which_way_is_better()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 100));
        EchoOptimisation();
        var portfolio = await CreatePortfolioAsync(client);
        await AddPositionAsync(client, portfolio.Id, "AAPL", 1, 90);

        using var response = await client.GetAsync($"api/portfolios/{portfolio.Id}/optimisation");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var comparisons = json.RootElement.GetProperty("comparisons").EnumerateArray().ToList();
        Assert.Equal(9, comparisons.Count);
        Assert.Equal("cumulativeReturn", comparisons[0].GetProperty("metricKey").GetString());
        Assert.Equal("Загальний дохід", comparisons[0].GetProperty("metricName").GetString()); // the old field is still there
        Assert.Equal(
            new[] { "maxDrawdown", "volatility" },
            comparisons.Where(c => c.GetProperty("lowerIsBetter").GetBoolean()).Select(c => c.GetProperty("metricKey").GetString()));
        Assert.Equal(2, comparisons[0].GetProperty("strategyOptimizedValue").GetDouble());
    }
}
