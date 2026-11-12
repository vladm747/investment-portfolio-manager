using System.Net;
using System.Net.Http.Json;
using System.Text;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.Tests.Api;

/// <summary>Row-by-row checks that a request with nonsense in it is a 400 before anything is stored or looked up.</summary>
public class ValidationApiTests : ApiTestBase
{
    public ValidationApiTests(ApiFactory api) : base(api) { }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    /// <summary>A valid new position; the portfolio is in the address, so it is not in the body.</summary>
    private static string Position(string overrides = "") =>
        "{" + "\"quantity\":10,\"symbol\":\"AAPL\",\"entryPrice\":150,\"entryDate\":\"2024-05-06\"" +
        (overrides.Length > 0 ? "," + overrides : "") + "}";

    private static string PositionsOf(int portfolioId) => $"api/portfolios/{portfolioId}/positions";

    // ---- positions --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("\"quantity\":0", "quantity of zero")]
    [InlineData("\"quantity\":-5", "negative quantity")]
    [InlineData("\"quantity\":2.5", "fractional quantity")]
    [InlineData("\"entryPrice\":0", "price of zero")]
    [InlineData("\"entryPrice\":-1", "negative price")]
    [InlineData("\"entryPrice\":0.00001", "price the column cannot keep")]
    [InlineData("\"symbol\":\"\"", "empty ticker")]
    [InlineData("\"symbol\":\"A B\"", "ticker with a space")]
    [InlineData("\"symbol\":\"TOOLONGSYMBOL\"", "ticker that is too long")]
    [InlineData("\"symbol\":\"AAPL;DROP TABLE\"", "ticker with punctuation")]
    [InlineData("\"entryDate\":\"2999-01-01\"", "date in the future")]
    [InlineData("\"entryDate\":\"0001-01-01\"", "missing date")]
    public async Task A_position_with_nonsense_in_it_is_a_400_and_nothing_is_stored_or_looked_up(string overrides, string why)
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);
        MarketPrices(("AAPL", 150));
        // The same JSON key twice: the last one wins, so the override replaces the valid value.
        var body = Position(overrides);

        using var response = await client.PostAsync(PositionsOf(portfolio.Id), Json(body));

        Assert.True(HttpStatusCode.BadRequest == response.StatusCode, $"{why}: expected 400, got {(int)response.StatusCode}");
        var error = await ReadErrorAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(error.Message), why);
        Assert.NotEmpty(error.Errors!);
        Assert.Equal(0, MathModule.Calls);
        Assert.Empty(await ReadAsync<List<StockDTO>>(await client.GetAsync(PositionsOf(portfolio.Id))));
    }

    [Fact]
    public async Task Every_broken_field_is_reported_at_once_in_Ukrainian()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.PostAsync(PositionsOf(1), Json(
            """{"quantity":0,"symbol":"A B","entryPrice":0,"entryDate":"2999-01-01"}"""));

        var error = await ReadErrorAsync(response);
        Assert.Equal(4, error.Errors!.Count);
        Assert.Contains("Кількість акцій має бути більшою за нуль.", error.Errors);
        Assert.Contains("Ціна купівлі має бути більшою за нуль.", error.Errors);
        Assert.Contains("Дата купівлі не може бути в майбутньому.", error.Errors);
        Assert.Contains(error.Errors, message => message.StartsWith("Тікер"));
    }

    [Theory]
    [InlineData("0.0001")] // the smallest price the column keeps
    [InlineData("150.1234")]
    [InlineData("123456.78")]
    public async Task A_valid_position_passes_the_same_checks(string entryPrice)
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);
        MarketPrices(("BRK-B", 400));

        using var response = await client.PostAsync(PositionsOf(portfolio.Id),
            Json(Position($"\"symbol\":\"brk-b\",\"entryPrice\":{entryPrice}")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task A_position_in_a_portfolio_that_does_not_exist_is_a_404_not_a_validation_error()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));

        using var response = await client.PostAsync(PositionsOf(987654), Json(Position()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, MathModule.Calls);
    }

    [Fact]
    public async Task A_broken_body_is_a_400_that_keeps_the_parser_details_to_itself()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.PostAsync(PositionsOf(1), Json("""{"quantity":"many"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("System.", body);
        Assert.DoesNotContain("LineNumber", body);
        Assert.DoesNotContain("Path:", body);
        Assert.Contains("Некоректне значення поля «quantity».", (await ReadErrorAsync(response)).Errors!);
        Assert.Equal(0, MathModule.Calls);
    }

    [Fact]
    public async Task An_empty_body_is_a_400()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var stock = await client.PostAsync(PositionsOf(1), Json(""));
        using var update = await client.PutAsync("api/positions/1", Json(""));
        using var portfolio = await client.PostAsync("api/portfolios", Json(""));

        Assert.Equal(HttpStatusCode.BadRequest, stock.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, portfolio.StatusCode);
    }

    // ---- quantity update --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("""{"quantity":0}""")]
    [InlineData("""{"quantity":-3}""")]
    [InlineData("""{"quantity":2.5}""")]
    [InlineData("""{"quantity":"abc"}""")]
    [InlineData("""{"quantity":null}""")]
    [InlineData("""{}""")]
    public async Task A_new_quantity_of_zero_or_less_or_not_a_whole_number_is_a_400_and_changes_nothing(string body)
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        MarketPrices(("AAPL", 150));
        var portfolio = await CreatePortfolioAsync(client);
        var position = await AddPositionAsync(client, portfolio.Id, "AAPL", 2, 100);

        using var response = await client.PutAsync($"api/positions/{position.Id}", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = Assert.Single(await ReadAsync<List<StockDTO>>(await client.GetAsync(PositionsOf(portfolio.Id))));
        Assert.Equal(2, stored.Quantity);
    }

    // ---- portfolios -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("""{"name":""}""")]
    [InlineData("""{"name":"   "}""")]
    [InlineData("""{}""")]
    [InlineData("""{"name":null}""")]
    public async Task A_portfolio_needs_a_name(string body)
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.PostAsync("api/portfolios", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ReadAsync<List<PortfolioDTO>>(await client.GetAsync("api/portfolios")));
    }

    [Fact]
    public async Task A_portfolio_name_of_more_than_100_characters_is_a_400()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.PostAsJsonAsync("api/portfolios", new { name = new string('я', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
