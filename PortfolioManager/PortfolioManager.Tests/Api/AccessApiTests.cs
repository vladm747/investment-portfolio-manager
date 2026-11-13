using System.Net;
using System.Net.Http.Json;
using PortfolioManager.API.Filters;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.MathModule.Client;

namespace PortfolioManager.Tests.Api;

/// <summary>Who may call what, and what an error looks like from the outside.</summary>
public class AccessApiTests : ApiTestBase
{
    public AccessApiTests(ApiFactory api) : base(api) { }

    // ---- authentication ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("GET", "api/portfolios")]
    [InlineData("POST", "api/portfolios")]
    [InlineData("GET", "api/portfolios/1")]
    [InlineData("DELETE", "api/portfolios/1")]
    [InlineData("GET", "api/portfolios/1/positions")]
    [InlineData("POST", "api/portfolios/1/positions")]
    [InlineData("POST", "api/portfolios/1/prices/refresh")]
    [InlineData("PUT", "api/positions/1")]
    [InlineData("DELETE", "api/positions/1")]
    [InlineData("GET", "api/portfolios/1/optimisation")]
    [InlineData("GET", "api/portfolios/1/growth")]
    [InlineData("GET", "api/user/me")]
    [InlineData("GET", "api/admin/users")]
    public async Task Without_a_token_every_endpoint_is_401(string method, string url)
    {
        using var client = Api.ClientFor(null);

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Creating_without_a_token_is_401()
    {
        using var client = Api.ClientFor(null);

        using var portfolio = await client.PostAsJsonAsync("api/portfolios", new { name = "X" });
        using var stock = await client.PostAsJsonAsync("api/portfolios/1/positions",
            new { quantity = 1, symbol = "AAPL", entryPrice = 1, entryDate = "2024-05-06" });

        Assert.Equal(HttpStatusCode.Unauthorized, portfolio.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, stock.StatusCode);
    }

    [Theory]
    [InlineData("swagger/v1/swagger.json")]
    [InlineData("swagger/index.html")]
    [InlineData("swagger")]
    public async Task Swagger_is_not_served_outside_development(string url)
    {
        // The test host runs as Production, like Azure: the description of the API is not for the public.
        using var client = Api.ClientFor(null);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- the old routes -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("POST", "api/auth/signup")]
    [InlineData("POST", "api/auth/signin")]
    [InlineData("POST", "api/auth/signin-google")]
    [InlineData("GET", "api/auth")]
    [InlineData("GET", "api/user/users")]
    public async Task The_password_sign_in_and_the_open_user_list_are_gone(string method, string url)
    {
        // Signed in as an admin, so only a missing route (not a missing token or role) can explain the answer.
        var admin = await Api.SignUpAdminAsync();
        using var client = Api.ClientFor(admin);

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Everybody_signed_in_can_still_read_their_own_profile()
    {
        var investor = await Api.SignUpAsync("Олена Коваль");
        using var client = Api.ClientFor(investor);

        var me = await ReadAsync<UserDTO>(await client.GetAsync("api/user/me"));

        Assert.Equal(investor.Email, me.Email);
    }

    // ---- errors ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task An_unexpected_failure_is_a_500_that_does_not_reveal_what_went_wrong()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);
        MathModule.Stock = _ => throw new InvalidOperationException("Cannot open server prod-sql.database.windows.net as sa");

        using var response = await client.PostAsJsonAsync($"api/portfolios/{portfolio.Id}/positions",
            new { quantity = 1, symbol = "AAPL", entryPrice = 10, entryDate = "2024-05-06" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("prod-sql", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(PortfolioManagerExceptionFilterAttribute.InternalErrorMessage, body);
    }

    [Fact]
    public async Task An_unknown_ticker_is_a_400_in_Ukrainian()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);
        MathModule.Stock = _ => throw new SymbolNotFoundException();

        using var response = await client.PostAsJsonAsync($"api/portfolios/{portfolio.Id}/positions",
            new { quantity = 1, symbol = "NOPE", entryPrice = 10, entryDate = "2024-05-06" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Такий тікер не знайдено", (await ReadErrorAsync(response)).Message);
    }

    [Fact]
    public async Task Yahoo_being_down_is_a_502_not_a_500()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);
        var portfolio = await CreatePortfolioAsync(client);
        MathModule.Stock = _ => throw new MarketDataUnavailableException();

        using var response = await client.PostAsJsonAsync($"api/portfolios/{portfolio.Id}/positions",
            new { quantity = 1, symbol = "AAPL", entryPrice = 10, entryDate = "2024-05-06" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
