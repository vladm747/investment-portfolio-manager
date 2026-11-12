using System.Net.Http.Json;
using PortfolioManager.API.Filters;
using PortfolioManager.Common.DTO;
using PortfolioManager.MathModule.Client.Contract;
using PortfolioManager.Tests.Services;

namespace PortfolioManager.Tests.Api;

/// <summary>One API host for every HTTP test: the classes of the collection run one after another.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase
{
    protected ApiFactory Api { get; }

    protected FakeMathModule MathModule => Api.MathModule;

    protected ApiTestBase(ApiFactory api)
    {
        Api = api;
        api.MathModule.Reset(); // the host is shared, so each test starts from a fake that knows nothing
    }

    /// <summary>Teaches the fake market the current price of these tickers.</summary>
    protected void MarketPrices(params (string Symbol, double Price)[] prices) =>
        MathModule.Stock = symbol =>
        {
            var (name, price) = prices.Single(p => p.Symbol == symbol.ToUpperInvariant());
            return new StockResponse(name, name + " Inc.", "USD", "Technology", price);
        };

    /// <summary>An optimisation that answers with the weights it was given and equal weights for the optimised portfolio.</summary>
    protected void EchoOptimisation() =>
        MathModule.Optimisation = request => new OptimisationResponse(
            new Period(new DateOnly(2020, 1, 2), new DateOnly(2024, 5, 6), 1090), 0,
            new WeightsPair(
                request.Symbols.Zip(request.Weights).ToDictionary(pair => pair.First, pair => pair.Second),
                request.Symbols.ToDictionary(symbol => symbol, _ => 1.0 / request.Symbols.Count)),
            new MetricsPair(
                new MetricSet(1, 2, 3, 4, 5, -6, 7, 8, 9),
                new MetricSet(2, 4, 6, 8, 10, -3, 5, 16, 18)));

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    protected static async Task<ErrorDTO> ReadErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ErrorDTO>())!;

    protected static async Task<PortfolioDTO> CreatePortfolioAsync(HttpClient client, string name = "Основний") =>
        await ReadAsync<PortfolioDTO>(await client.PostAsJsonAsync("api/portfolios", new { name }));

    protected static async Task<StockDTO> AddPositionAsync(
        HttpClient client, int portfolioId, string symbol, int quantity, decimal entryPrice) =>
        await ReadAsync<StockDTO>(await client.PostAsJsonAsync($"api/portfolios/{portfolioId}/positions",
            new { quantity, symbol, entryPrice, entryDate = "2024-05-06" }));

    protected static async Task<PortfolioDTO> GetPortfolioAsync(HttpClient client, int portfolioId) =>
        await ReadAsync<PortfolioDTO>(await client.GetAsync($"api/portfolios/{portfolioId}"));
}
