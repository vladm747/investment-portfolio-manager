using Microsoft.Extensions.Configuration;

namespace PortfolioManager.BLL.Helpers;

public class GoogleAuthSettings
{
    /// <summary>The OAuth client ID of the web application in Google Cloud. Public by design: the browser sends it to Google too.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Google accounts that get the admin role when they sign in. Compared without regard to case.</summary>
    public IReadOnlySet<string> AdminEmails { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsAdmin(string? email) => !string.IsNullOrWhiteSpace(email) && AdminEmails.Contains(email.Trim());

    /// <summary>
    /// Reads the Google settings from configuration and fails at startup when the client ID is missing,
    /// instead of rejecting every sign-in later with a message the user cannot act on.
    /// <list type="bullet">
    /// <item><c>Authentication:Google:ClientId</c> (<c>Authentication__Google__ClientId</c>): required.</item>
    /// <item><c>Auth:AdminEmails</c> (<c>Auth__AdminEmails__0</c>, <c>Auth__AdminEmails__1</c>, ...): optional.
    /// A single comma- or semicolon-separated value is accepted too.</item>
    /// </list>
    /// </summary>
    public static GoogleAuthSettings FromConfiguration(IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"]?.Trim();
        if (string.IsNullOrEmpty(clientId))
            throw new InvalidOperationException(
                "Authentication:Google:ClientId is not configured. Create an OAuth client ID (type Web application) in " +
                "Google Cloud and set it with `dotnet user-secrets set \"Authentication:Google:ClientId\" \"<id>\"` " +
                "or, on Azure, the App Service setting Authentication__Google__ClientId.");

        var section = configuration.GetSection("Auth:AdminEmails");
        var listed = section.Get<string[]>() ?? Array.Empty<string>();
        if (listed.Length == 0 && !string.IsNullOrWhiteSpace(section.Value))
            listed = section.Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return new GoogleAuthSettings
        {
            ClientId = clientId,
            AdminEmails = listed
                .Select(email => email.Trim())
                .Where(email => email.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
    }
}
