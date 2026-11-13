using System.Net;
using System.Text.Json;
using PortfolioManager.Common.DTO.Admin;

namespace PortfolioManager.Tests.Api;

/// <summary><c>GET api/admin/users</c>: who may call it and what it tells them.</summary>
public class AdminApiTests : ApiTestBase
{
    public AdminApiTests(ApiFactory api) : base(api) { }

    [Fact]
    public async Task An_investor_gets_403()
    {
        var investor = await Api.SignUpAsync();
        using var client = Api.ClientFor(investor);

        using var response = await client.GetAsync("api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_gets_the_totals_and_every_user()
    {
        var investor = await Api.SignUpAsync("Олена Коваль");
        var admin = await Api.SignUpAdminAsync();
        using var client = Api.ClientFor(admin);

        var result = await ReadAsync<AdminUsersDTO>(await client.GetAsync("api/admin/users"));

        var olena = Assert.Single(result.Users, user => user.Email == investor.Email);
        Assert.Equal("Олена Коваль", olena.FullName);
        Assert.Equal(new[] { "investor" }, olena.Roles);
        var adminRow = Assert.Single(result.Users, user => user.Email == admin.Email);
        Assert.Contains("admin", adminRow.Roles);
        // The host is shared with other tests, so only the relations between the numbers are fixed.
        Assert.Equal(result.Users.Count, result.Stats.TotalUsers);
        Assert.Equal(result.Users.Count(user => user.Roles.Contains("admin")), result.Stats.Admins);
        Assert.Equal(result.Users.Sum(user => user.PortfolioCount), result.Stats.TotalPortfolios);
        Assert.True(result.Stats.ActiveLast7Days >= 2);
    }

    [Fact]
    public async Task The_portfolio_count_and_the_last_active_time_come_from_the_users_data_and_sign_in()
    {
        var investor = await Api.SignUpAsync();
        using (var investorClient = Api.ClientFor(investor))
        {
            await CreatePortfolioAsync(investorClient, "Перший");
            await CreatePortfolioAsync(investorClient, "Другий");
        }
        var admin = await Api.SignUpAdminAsync();
        using var client = Api.ClientFor(admin);

        var result = await ReadAsync<AdminUsersDTO>(await client.GetAsync("api/admin/users"));

        var row = Assert.Single(result.Users, user => user.Email == investor.Email);
        Assert.Equal(2, row.PortfolioCount);
        Assert.NotNull(row.LastActiveAt);
        Assert.InRange(DateTime.UtcNow - row.LastActiveAt!.Value, TimeSpan.FromSeconds(-5), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task The_json_has_profile_and_counts_only_and_dates_are_marked_utc()
    {
        var investor = await Api.SignUpAsync();
        var admin = await Api.SignUpAdminAsync();
        using var client = Api.ClientFor(admin);

        using var response = await client.GetAsync("api/admin/users");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(new[] { "stats", "users" }, document.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.Equal(new[] { "activeLast7Days", "admins", "totalPortfolios", "totalUsers" },
            document.RootElement.GetProperty("stats").EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        var row = document.RootElement.GetProperty("users").EnumerateArray()
            .Single(user => user.GetProperty("email").GetString() == investor.Email);
        Assert.Equal(new[] { "email", "fullName", "id", "lastActiveAt", "portfolioCount", "roles" },
            row.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.EndsWith("Z", row.GetProperty("lastActiveAt").GetString());
        Assert.DoesNotContain("refresh", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_token_whose_role_claim_was_edited_is_401()
    {
        // A token for an investor whose role claim was edited does not validate: the signature covers the claims.
        var investor = await Api.SignUpAsync();
        var parts = investor.Token.Split('.');
        var payload = System.Text.Encoding.UTF8.GetString(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(parts[1]))
            .Replace("\"investor\"", "\"admin\"");
        var forged = parts[0] + "." + Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(payload) + "." + parts[2];
        using var client = Api.ClientFor(investor with { Token = forged });

        using var response = await client.GetAsync("api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
