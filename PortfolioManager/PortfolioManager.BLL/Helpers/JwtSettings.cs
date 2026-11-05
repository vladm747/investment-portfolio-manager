using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PortfolioManager.BLL.Helpers;

public class JwtSettings
{
    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    public const int MinSecretBytes = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Reads the JWT settings from configuration (environment variables locally and on Azure)
    /// and fails at startup when a required value is missing, instead of issuing broken tokens later.
    /// <list type="bullet">
    /// <item><c>JWT_ISSUER</c>, <c>JWT_SECRET</c>: required.</item>
    /// <item><c>JWT_EXPIRES_MINUTES</c>: access token lifetime in minutes. Falls back to the older
    /// <c>JWT_EXPIRES</c> (hours), then to 15 minutes.</item>
    /// <item><c>JWT_REFRESH_EXPIRES_DAYS</c>: refresh token lifetime, 7 days by default.</item>
    /// </list>
    /// </summary>
    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = new JwtSettings
        {
            Issuer = configuration["JWT_ISSUER"] ?? string.Empty,
            Secret = configuration["JWT_SECRET"] ?? string.Empty,
        };

        if (TryReadPositive(configuration["JWT_EXPIRES_MINUTES"], out var minutes))
            settings.AccessTokenLifetime = TimeSpan.FromMinutes(minutes);
        else if (TryReadPositive(configuration["JWT_EXPIRES"], out var hours))
            settings.AccessTokenLifetime = TimeSpan.FromHours(hours);

        if (TryReadPositive(configuration["JWT_REFRESH_EXPIRES_DAYS"], out var days))
            settings.RefreshTokenLifetime = TimeSpan.FromDays(days);

        if (string.IsNullOrWhiteSpace(settings.Issuer))
            throw new InvalidOperationException("JWT_ISSUER is not configured.");

        if (Encoding.UTF8.GetByteCount(settings.Secret) < MinSecretBytes)
            throw new InvalidOperationException(
                $"JWT_SECRET is not configured or is shorter than {MinSecretBytes} bytes.");

        return settings;
    }

    private static bool TryReadPositive(string? value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result > 0;
}
