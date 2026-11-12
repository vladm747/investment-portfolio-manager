using Microsoft.Extensions.Options;

namespace PortfolioManager.MathModule.Client;

/// <summary>The <c>MathModule</c> configuration section.</summary>
public sealed class MathModuleOptions
{
    public const string SectionName = "MathModule";

    /// <summary>Where the Python math service listens, for example <c>http://localhost:8011</c>. Required.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Sent as the <c>X-API-Key</c> header. Must equal MATH_API_KEY of the math service; the service accepts every
    /// caller while that is unset, so leave this empty only then. A secret: user-secrets locally, App Service settings on Azure.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>How long one attempt may take. Optimisation downloads years of prices, so the default is generous.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(90);

    /// <summary>How many times a failed attempt (5xx or no connection) is repeated. A timeout is not repeated.</summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>Wait before the first retry; it doubles for each further one, with some jitter.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>Fails at startup, naming the setting, instead of failing on the first request.</summary>
internal sealed class MathModuleOptionsValidator : IValidateOptions<MathModuleOptions>
{
    public ValidateOptionsResult Validate(string? name, MathModuleOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            errors.Add($"{MathModuleOptions.SectionName}:{nameof(options.BaseUrl)} is not configured.");
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                 || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            errors.Add($"{MathModuleOptions.SectionName}:{nameof(options.BaseUrl)} must be an http(s) URL, got '{options.BaseUrl}'.");

        if (options.Timeout <= TimeSpan.Zero)
            errors.Add($"{MathModuleOptions.SectionName}:{nameof(options.Timeout)} must be positive.");

        if (options.RetryCount is < 0 or > 10)
            errors.Add($"{MathModuleOptions.SectionName}:{nameof(options.RetryCount)} must be between 0 and 10.");

        if (options.RetryDelay < TimeSpan.Zero)
            errors.Add($"{MathModuleOptions.SectionName}:{nameof(options.RetryDelay)} must not be negative.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
