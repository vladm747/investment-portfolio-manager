using System.Net;
using System.Net.Http.Json;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.Tests.Api;

/// <summary>
/// The shape of the public API: resources in the address, the method says what happens, the status says what came
/// of it. The client calls exactly these routes (PortfolioManager.Client/src/Services).
/// </summary>
public class RestRoutesApiTests : ApiTestBase
{
    public RestRoutesApiTests(ApiFactory api) : base(api) { }

    [Fact]
    public async Task Creating_a_portfolio_is_a_201_with_the_address_of_the_new_portfolio()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.PostAsJsonAsync("api/portfolios", new { name = "Основний" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<PortfolioDTO>(response);
        Assert.EndsWith($"/api/portfolios/{created.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal("Основний", (await GetPortfolioAsync(client, created.Id)).Name);
    }

    [Fact]
    public async Task Creating_a_position_is_a_201_and_the_portfolio_comes_from_the_address()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));
        var portfolio = await CreatePortfolioAsync(client);

        using var response = await client.PostAsJsonAsync($"api/portfolios/{portfolio.Id}/positions",
            new { quantity = 2, symbol = "AAPL", entryPrice = 100, entryDate = "2024-05-06" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<StockDTO>(response);
        Assert.Equal(portfolio.Id, created.PortfolioId);
        // An absolute path, so a client resolves it to the position and not to something under .../positions.
        Assert.Equal($"/api/positions/{created.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task A_portfolio_id_in_the_body_of_a_new_position_is_ignored_the_address_decides()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));
        var first = await CreatePortfolioAsync(client, "Перший");
        var second = await CreatePortfolioAsync(client, "Другий");

        var created = await ReadAsync<StockDTO>(await client.PostAsJsonAsync($"api/portfolios/{first.Id}/positions",
            new { portfolioId = second.Id, quantity = 1, symbol = "AAPL", entryPrice = 100, entryDate = "2024-05-06" }));

        Assert.Equal(first.Id, created.PortfolioId);
        Assert.Empty(await ReadAsync<List<StockDTO>>(await client.GetAsync($"api/portfolios/{second.Id}/positions")));
    }

    [Fact]
    public async Task Changing_a_position_sends_the_new_quantity_in_the_body()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));
        var portfolio = await CreatePortfolioAsync(client);
        var position = await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);

        var updated = await ReadAsync<StockDTO>(await client.PutAsJsonAsync($"api/positions/{position.Id}", new { quantity = 7 }));

        Assert.Equal(7, updated.Quantity);
        Assert.Equal(1050, updated.TotalCurrentPrice); // 7 * 150
    }

    [Fact]
    public async Task Refreshing_prices_changes_data_so_a_GET_is_refused_and_asks_nobody()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));
        var portfolio = await CreatePortfolioAsync(client);
        await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);
        var callsToTheMarket = MathModule.Calls;

        using var response = await client.GetAsync($"api/portfolios/{portfolio.Id}/prices/refresh");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(callsToTheMarket, MathModule.Calls);
    }

    [Theory]
    [InlineData("GET", "api/portfolio/1")]
    [InlineData("POST", "api/portfolio")]
    [InlineData("GET", "api/stock/1")]
    [InlineData("POST", "api/stock")]
    [InlineData("PUT", "api/stock/1/5")]
    [InlineData("GET", "api/stock/info/1")]
    [InlineData("DELETE", "api/stock/1")]
    [InlineData("GET", "api/statistics/opt/1")]
    [InlineData("GET", "api/statistics/growth/1")]
    public async Task The_old_routes_are_gone(string method, string url)
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "api/portfolios/abc")]
    [InlineData("GET", "api/portfolios/abc/positions")]
    [InlineData("POST", "api/portfolios/abc/prices/refresh")]
    [InlineData("GET", "api/portfolios/abc/growth")]
    [InlineData("GET", "api/portfolios/abc/optimisation")]
    [InlineData("DELETE", "api/positions/abc")]
    public async Task An_id_that_is_not_a_number_is_a_404_not_a_server_error(string method, string url)
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>Swagger lists the routes while developing and does not exist anywhere else.</summary>
public class SwaggerDevelopmentTests : IClassFixture<DevelopmentApiFactory>
{
    private readonly DevelopmentApiFactory _api;

    public SwaggerDevelopmentTests(DevelopmentApiFactory api) => _api = api;

    [Fact]
    public async Task In_development_swagger_describes_the_rest_routes()
    {
        using var client = _api.CreateClient();

        using var response = await client.GetAsync("swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"/api/portfolios\"", document);
        Assert.Contains("\"/api/portfolios/{id}\"", document);
        Assert.Contains("\"/api/portfolios/{portfolioId}/positions\"", document);
        Assert.Contains("\"/api/portfolios/{portfolioId}/prices/refresh\"", document);
        Assert.Contains("\"/api/positions/{id}\"", document);
        Assert.Contains("\"/api/portfolios/{portfolioId}/growth\"", document);
        Assert.Contains("\"/api/portfolios/{portfolioId}/optimisation\"", document);
        Assert.DoesNotContain("/api/stock", document);
        Assert.DoesNotContain("/api/statistics", document);
    }

    [Fact]
    public async Task In_development_the_swagger_page_is_served()
    {
        using var client = _api.CreateClient();

        using var response = await client.GetAsync("swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
