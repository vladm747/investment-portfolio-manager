using Microsoft.Extensions.Configuration;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Services.Auth;

namespace PortfolioManager.Tests.Auth;

public class GoogleAuthSettingsTests
{
    private const string ClientId = "123-abc.apps.googleusercontent.com";

    private static GoogleAuthSettings Read(params (string Key, string? Value)[] values) =>
        GoogleAuthSettings.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_client_id_stops_startup_and_names_the_setting(string? clientId)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Read(("Authentication:Google:ClientId", clientId)));

        Assert.Contains("Authentication:Google:ClientId", error.Message);
    }

    [Fact]
    public void Without_any_google_section_startup_is_stopped_too()
    {
        Assert.Throws<InvalidOperationException>(() => Read());
    }

    [Fact]
    public void The_client_id_is_read_and_trimmed()
    {
        var settings = Read(("Authentication:Google:ClientId", "  " + ClientId + " "));

        Assert.Equal(ClientId, settings.ClientId);
    }

    [Fact]
    public void Without_an_admin_list_nobody_is_an_admin()
    {
        var settings = Read(("Authentication:Google:ClientId", ClientId));

        Assert.Empty(settings.AdminEmails);
        Assert.False(settings.IsAdmin("anyone@example.com"));
    }

    [Fact]
    public void Admin_emails_are_read_from_the_indexed_settings_without_regard_to_case_or_spaces()
    {
        var settings = Read(
            ("Authentication:Google:ClientId", ClientId),
            ("Auth:AdminEmails:0", "Vlad@Example.com"),
            ("Auth:AdminEmails:1", "  second@example.com "),
            ("Auth:AdminEmails:2", "   "));

        Assert.Equal(2, settings.AdminEmails.Count);
        Assert.True(settings.IsAdmin("vlad@example.com"));
        Assert.True(settings.IsAdmin(" SECOND@example.com"));
        Assert.False(settings.IsAdmin("third@example.com"));
        Assert.False(settings.IsAdmin(null));
        Assert.False(settings.IsAdmin(""));
    }

    [Theory]
    [InlineData("a@example.com,b@example.com")]
    [InlineData("a@example.com; b@example.com")]
    public void A_single_separated_value_works_too_so_one_azure_setting_is_enough(string value)
    {
        var settings = Read(("Authentication:Google:ClientId", ClientId), ("Auth:AdminEmails", value));

        Assert.True(settings.IsAdmin("a@example.com"));
        Assert.True(settings.IsAdmin("b@example.com"));
    }
}

/// <summary>The real validator, with tokens that are refused before any key is fetched from Google: no network is used.</summary>
public class GoogleTokenValidatorTests
{
    private readonly GoogleTokenValidator _validator = new(new GoogleAuthSettings { ClientId = "test.apps.googleusercontent.com" });

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("a.b")]
    [InlineData("a.b.c")]
    [InlineData("a.b.c.d")]
    [InlineData("...")]
    [InlineData("!!!.@@@.###")]
    // Valid base64url, but the header and the payload are "not-json", and a three-letter "signature".
    [InlineData("bm90LWpzb24.bm90LWpzb24.c2ln")]
    // Valid JSON in both parts ("{}"), signature missing.
    [InlineData("e30.e30.")]
    public async Task A_token_that_is_not_a_google_id_token_is_an_authentication_failure(string credential)
    {
        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() => _validator.ValidateAsync(credential));

        Assert.Equal(AuthService.InvalidGoogleTokenMessage, error.Message);
    }
}
