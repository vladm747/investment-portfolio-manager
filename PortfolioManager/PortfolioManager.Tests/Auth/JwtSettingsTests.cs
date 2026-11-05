using Microsoft.Extensions.Configuration;
using PortfolioManager.BLL.Helpers;

namespace PortfolioManager.Tests.Auth;

public class JwtSettingsTests
{
    private const string ValidSecret = "a-signing-key-that-is-at-least-32-bytes";

    private static JwtSettings Read(params (string Key, string? Value)[] values) =>
        JwtSettings.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build());

    [Fact]
    public void Lifetime_in_minutes_wins_over_the_older_setting_in_hours()
    {
        var settings = Read(("JWT_ISSUER", "pm"), ("JWT_SECRET", ValidSecret),
            ("JWT_EXPIRES_MINUTES", "15"), ("JWT_EXPIRES", "1"));

        Assert.Equal(TimeSpan.FromMinutes(15), settings.AccessTokenLifetime);
    }

    [Fact]
    public void The_older_setting_in_hours_still_works()
    {
        var settings = Read(("JWT_ISSUER", "pm"), ("JWT_SECRET", ValidSecret), ("JWT_EXPIRES", "1"));

        Assert.Equal(TimeSpan.FromHours(1), settings.AccessTokenLifetime);
    }

    [Fact]
    public void Without_a_lifetime_tokens_last_15_minutes_and_7_days()
    {
        var settings = Read(("JWT_ISSUER", "pm"), ("JWT_SECRET", ValidSecret));

        Assert.Equal(TimeSpan.FromMinutes(15), settings.AccessTokenLifetime);
        Assert.Equal(TimeSpan.FromDays(7), settings.RefreshTokenLifetime);
    }

    [Theory]
    [InlineData(null, ValidSecret)]
    [InlineData("pm", null)]
    [InlineData("pm", "too-short")]
    public void Missing_issuer_or_weak_secret_stops_startup(string? issuer, string? secret)
    {
        Assert.Throws<InvalidOperationException>(() => Read(("JWT_ISSUER", issuer), ("JWT_SECRET", secret)));
    }
}
