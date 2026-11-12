using System.Net;
using System.Net.Http.Json;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.Tests.Api;

/// <summary>Row 7 of the overview: nobody can read, change or delete another user's data by guessing an id.</summary>
public class OwnershipApiTests : ApiTestBase
{
    public OwnershipApiTests(ApiFactory api) : base(api) { }

    [Fact]
    public async Task Every_endpoint_answers_404_for_the_data_of_another_user_and_changes_nothing()
    {
        var olena = await Api.SignUpAsync("Олена Коваль");
        var bohdan = await Api.SignUpAsync("Богдан Мельник");
        MarketPrices(("AAPL", 150), ("MSFT", 300));
        using var owner = Api.ClientFor(olena);
        using var intruder = Api.ClientFor(bohdan);
        var portfolio = await CreatePortfolioAsync(owner);
        var position = await AddPositionAsync(owner, portfolio.Id, "AAPL", 2, 100);
        var callsToTheMarket = MathModule.Calls;

        var requests = new (string Name, Func<HttpClient, Task<HttpResponseMessage>> Send)[]
        {
            ("GET portfolio", c => c.GetAsync($"api/portfolios/{portfolio.Id}")),
            ("DELETE portfolio", c => c.DeleteAsync($"api/portfolios/{portfolio.Id}")),
            ("GET positions", c => c.GetAsync($"api/portfolios/{portfolio.Id}/positions")),
            ("POST position", c => c.PostAsJsonAsync($"api/portfolios/{portfolio.Id}/positions",
                new { quantity = 1, symbol = "MSFT", entryPrice = 10, entryDate = "2024-05-06" })),
            ("PUT quantity", c => c.PutAsJsonAsync($"api/positions/{position.Id}", new { quantity = 99 })),
            ("DELETE position", c => c.DeleteAsync($"api/positions/{position.Id}")),
            ("refresh prices", c => c.PostAsync($"api/portfolios/{portfolio.Id}/prices/refresh", null)),
            ("optimisation", c => c.GetAsync($"api/portfolios/{portfolio.Id}/optimisation")),
            ("growth", c => c.GetAsync($"api/portfolios/{portfolio.Id}/growth")),
        };

        foreach (var (name, send) in requests)
        {
            using var response = await send(intruder);

            Assert.True(HttpStatusCode.NotFound == response.StatusCode, $"{name}: expected 404, got {(int)response.StatusCode}");
            Assert.False(string.IsNullOrEmpty((await ReadErrorAsync(response)).Message), name);
        }

        Assert.Equal(callsToTheMarket, MathModule.Calls); // nobody else's request reached Yahoo or the math module

        var intact = await GetPortfolioAsync(owner, portfolio.Id);
        var stock = Assert.Single(intact.Stocks!);
        Assert.Equal(2, stock.Quantity);
        Assert.Equal(portfolio.Name, intact.Name);
    }

    [Fact]
    public async Task A_foreign_portfolio_and_a_missing_one_get_the_same_answer()
    {
        var olena = await Api.SignUpAsync();
        var bohdan = await Api.SignUpAsync("Богдан Мельник");
        using var owner = Api.ClientFor(olena);
        using var intruder = Api.ClientFor(bohdan);
        var portfolio = await CreatePortfolioAsync(owner);

        using var foreign = await intruder.GetAsync($"api/portfolios/{portfolio.Id}");
        using var missing = await intruder.GetAsync("api/portfolios/987654");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal((await ReadErrorAsync(foreign)).Message, (await ReadErrorAsync(missing)).Message);
    }

    [Fact]
    public async Task The_portfolio_list_holds_only_the_portfolios_of_the_signed_in_user()
    {
        var olena = await Api.SignUpAsync();
        var bohdan = await Api.SignUpAsync("Богдан Мельник");
        using var owner = Api.ClientFor(olena);
        using var other = Api.ClientFor(bohdan);
        var mine = await CreatePortfolioAsync(owner, "Моя");
        await CreatePortfolioAsync(other, "Чужа");

        var listed = await ReadAsync<List<PortfolioDTO>>(await owner.GetAsync("api/portfolios"));

        Assert.Equal(mine.Id, Assert.Single(listed).Id);
        Assert.Equal(olena.Id, listed[0].UserId);
    }

    [Fact]
    public async Task An_owner_sent_in_the_body_of_a_new_portfolio_is_ignored()
    {
        var olena = await Api.SignUpAsync();
        var bohdan = await Api.SignUpAsync("Богдан Мельник");
        using var owner = Api.ClientFor(olena);
        using var other = Api.ClientFor(bohdan);

        var created = await ReadAsync<PortfolioDTO>(
            await owner.PostAsJsonAsync("api/portfolios", new { name = "Підкинутий", userId = bohdan.Id }));

        Assert.Equal(olena.Id, created.UserId);
        using var theirView = await other.GetAsync($"api/portfolios/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, theirView.StatusCode);
        Assert.Empty(await ReadAsync<List<PortfolioDTO>>(await other.GetAsync("api/portfolios")));
    }

    [Fact]
    public async Task The_owner_can_still_do_everything_with_their_own_data()
    {
        var olena = await Api.SignUpAsync();
        MarketPrices(("AAPL", 150));
        MathModule.Prices = symbols => new(symbols.ToDictionary(s => s.ToUpperInvariant(), _ => 160.0), Array.Empty<string>());
        using var owner = Api.ClientFor(olena);
        var portfolio = await CreatePortfolioAsync(owner);
        var position = await AddPositionAsync(owner, portfolio.Id, "AAPL", 2, 100);

        Assert.Single(await ReadAsync<List<StockDTO>>(await owner.GetAsync($"api/portfolios/{portfolio.Id}/positions")));
        Assert.Equal(5, (await ReadAsync<StockDTO>(await owner.PutAsJsonAsync($"api/positions/{position.Id}", new { quantity = 5 }))).Quantity);
        using var refreshed = await owner.PostAsync($"api/portfolios/{portfolio.Id}/prices/refresh", null);
        Assert.Equal(HttpStatusCode.NoContent, refreshed.StatusCode);
        using var removed = await owner.DeleteAsync($"api/positions/{position.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        using var deleted = await owner.DeleteAsync($"api/portfolios/{portfolio.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var gone = await owner.GetAsync($"api/portfolios/{portfolio.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
