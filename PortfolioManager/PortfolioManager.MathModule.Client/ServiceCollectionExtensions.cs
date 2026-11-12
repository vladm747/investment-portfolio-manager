using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace PortfolioManager.MathModule.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMathModuleClient"/> from the <c>MathModule</c> section. A missing or malformed
    /// <c>BaseUrl</c> stops the application at startup.
    /// </summary>
    public static IServiceCollection AddMathModuleClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MathModuleOptions>()
            .Bind(configuration.GetSection(MathModuleOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MathModuleOptions>, MathModuleOptionsValidator>();

        services.AddHttpClient<IMathModuleClient, MathModuleClient>((provider, http) =>
            {
                var options = provider.GetRequiredService<IOptions<MathModuleOptions>>().Value;

                // The trailing slash keeps a path in BaseUrl (https://host/math) when relative paths are appended.
                http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                http.Timeout = System.Threading.Timeout.InfiniteTimeSpan; // the pipeline below owns the timeouts
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                    http.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            })
            .AddResilienceHandler("math-module", (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<MathModuleOptions>>().Value;

                // First added is outermost: retry wraps the per-attempt timeout.
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryCount,
                    Delay = options.RetryDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    // Retry 5xx and connection failures only. The calls only read and compute, so a repeat is safe;
                    // a timeout already cost a full Timeout, so repeating it would not help.
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Exception is HttpRequestException
                        || (args.Outcome.Result is { } result && (int)result.StatusCode >= 500))
                });
                pipeline.AddTimeout(options.Timeout);
            });

        return services;
    }
}
