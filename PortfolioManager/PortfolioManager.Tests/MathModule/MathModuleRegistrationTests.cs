using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using PortfolioManager.MathModule.Client;

namespace PortfolioManager.Tests.MathModule;

/// <summary>
/// <c>AddMathModuleClient</c> as the API uses it: configuration, the X-API-Key header, and the retry and timeout
/// pipeline, with the stub in place of the network. Retry delays are a millisecond so the tests stay fast.
/// </summary>
public class MathModuleRegistrationTests
{
    private const string Ok = """{"symbol":"AAPL","name":"Apple Inc.","currency":"USD","sector":"Technology","price":1}""";

    private static Dictionary<string, string?> Settings(params (string Key, string Value)[] more)
    {
        var settings = new Dictionary<string, string?>
        {
            ["MathModule:BaseUrl"] = "http://math.test",
            ["MathModule:RetryDelay"] = "00:00:00.001",
        };
        foreach (var (key, value) in more)
            settings["MathModule:" + key] = value;
        return settings;
    }

    private static ServiceProvider Provider(Dictionary<string, string?> settings, StubHandler handler)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMathModuleClient(configuration);
        // Replace only the innermost handler: the resilience pipeline stays in front of it.
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));
        return services.BuildServiceProvider();
    }

    private static IMathModuleClient ClientOf(ServiceProvider provider) =>
        provider.GetRequiredService<IMathModuleClient>();

    // ---- configuration ----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_base_url_keeps_its_path_and_the_api_key_travels_in_X_API_Key()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK, Ok);
        using var provider = Provider(Settings(("BaseUrl", "http://math.test/internal"), ("ApiKey", "s3cret")), handler);

        await ClientOf(provider).GetStockAsync("AAPL");

        Assert.Equal("http://math.test/internal/v1/stocks/AAPL", handler.Single.Uri.ToString());
        Assert.Equal("s3cret", handler.Single.Headers["X-API-Key"]);
    }

    [Fact]
    public async Task Without_an_api_key_no_header_is_sent()
    {
        var handler = new StubHandler().Json(HttpStatusCode.OK, Ok);
        using var provider = Provider(Settings(), handler);

        await ClientOf(provider).GetStockAsync("AAPL");

        Assert.False(handler.Single.Headers.ContainsKey("X-API-Key"));
    }

    [Theory]
    [InlineData("", "MathModule:BaseUrl is not configured.")]
    [InlineData("not a url", "MathModule:BaseUrl must be an http(s) URL")]
    [InlineData("ftp://math.test", "MathModule:BaseUrl must be an http(s) URL")]
    public async Task The_application_does_not_start_with_a_bad_base_url(string baseUrl, string expectedMessage)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(Settings(("BaseUrl", baseUrl)));
        builder.Services.AddMathModuleClient(builder.Configuration);
        using var host = builder.Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(expectedMessage, error.Message);
    }

    // ---- retry and timeout ------------------------------------------------------------------------------------

    [Fact]
    public async Task A_5xx_is_retried_until_the_service_recovers()
    {
        var handler = new StubHandler()
            .Error(HttpStatusCode.BadGateway, "market_data_unavailable", "Yahoo is down")
            .Error(HttpStatusCode.InternalServerError, "internal_error", "oops")
            .Json(HttpStatusCode.OK, Ok);
        using var provider = Provider(Settings(("RetryCount", "2")), handler);

        var stock = await ClientOf(provider).GetStockAsync("AAPL");

        Assert.Equal("AAPL", stock.Symbol);
        Assert.Equal(3, handler.Calls.Count);
    }

    [Fact]
    public async Task A_5xx_that_never_goes_away_stops_after_the_configured_retries()
    {
        var handler = new StubHandler().Error(HttpStatusCode.BadGateway, "market_data_unavailable", "Yahoo is down");
        using var provider = Provider(Settings(("RetryCount", "2")), handler);

        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientOf(provider).GetStockAsync("AAPL"));

        Assert.Equal(3, handler.Calls.Count); // the first attempt and two retries
    }

    [Fact]
    public async Task A_connection_failure_is_retried()
    {
        var handler = new StubHandler().Throw(new HttpRequestException("connection refused")).Json(HttpStatusCode.OK, Ok);
        using var provider = Provider(Settings(), handler);

        await ClientOf(provider).GetStockAsync("AAPL");

        Assert.Equal(2, handler.Calls.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "symbol_not_found")]
    [InlineData(HttpStatusCode.BadRequest, "not_enough_history")]
    [InlineData(HttpStatusCode.Unauthorized, "unauthorized")]
    public async Task A_4xx_is_never_retried(HttpStatusCode status, string code)
    {
        var handler = new StubHandler().Error(status, code, "no");
        using var provider = Provider(Settings(), handler);

        await Assert.ThrowsAnyAsync<Exception>(() => ClientOf(provider).GetStockAsync("AAPL"));

        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task A_slow_attempt_times_out_without_a_retry()
    {
        var handler = new StubHandler().Hang();
        using var provider = Provider(Settings(("Timeout", "00:00:00.100")), handler);

        var error = await Assert.ThrowsAsync<MarketDataUnavailableException>(() => ClientOf(provider).GetStockAsync("AAPL"));

        Assert.Equal(MarketDataUnavailableException.DefaultMessage, error.Message);
        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task A_retried_POST_sends_the_same_body_again()
    {
        var handler = new StubHandler()
            .Error(HttpStatusCode.BadGateway, "market_data_unavailable", "Yahoo is down")
            .Json(HttpStatusCode.OK, """{"prices":{"AAPL":1},"missing":[]}""");
        using var provider = Provider(Settings(), handler);

        await ClientOf(provider).GetPricesAsync(new[] { "AAPL" });

        Assert.Equal(2, handler.Calls.Count);
        Assert.Equal(handler.Calls[0].Body, handler.Calls[1].Body);
        Assert.Contains("AAPL", handler.Calls[1].Body);
    }
}
