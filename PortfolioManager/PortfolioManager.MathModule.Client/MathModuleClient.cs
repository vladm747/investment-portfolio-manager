using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly.Timeout;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.MathModule.Client;

/// <summary>
/// Typed <see cref="HttpClient"/> for the math service. Base address, API key, retry and timeout are set where it is
/// registered (<see cref="ServiceCollectionExtensions.AddMathModuleClient"/>); this class only builds requests and
/// turns every failure into one of the three exceptions of <see cref="IMathModuleClient"/>.
/// </summary>
public sealed class MathModuleClient : IMathModuleClient
{
    // Web defaults: camelCase names, case-insensitive reading. Dictionary keys (tickers) keep their case.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<MathModuleClient> _logger;

    public MathModuleClient(HttpClient http, ILogger<MathModuleClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<StockResponse> GetStockAsync(string symbol, CancellationToken cancellationToken = default) =>
        SendAsync<StockResponse>(HttpMethod.Get, $"v1/stocks/{Uri.EscapeDataString(symbol.Trim())}", body: null,
            cancellationToken);

    public Task<PricesResponse> GetPricesAsync(IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken = default) =>
        SendAsync<PricesResponse>(HttpMethod.Post, "v1/prices", new PricesRequest(symbols.ToList()), cancellationToken);

    public Task<GrowthResponse> GetGrowthAsync(GrowthRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<GrowthResponse>(HttpMethod.Post, "v1/growth", request, cancellationToken);

    public Task<OptimisationResponse> OptimiseAsync(OptimisationRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<OptimisationResponse>(HttpMethod.Post, "v1/optimisation", request, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var operation = $"{method} {path}";
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // The caller gave up; that is not a failure of the math service.
        }
        catch (Exception e) when (e is HttpRequestException or TimeoutRejectedException or OperationCanceledException)
        {
            _logger.LogWarning(e, "Math module call {Operation} failed before a response arrived", operation);
            throw new MarketDataUnavailableException(MarketDataUnavailableException.DefaultMessage, e);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw await MapFailureAsync(response, operation, cancellationToken);

            try
            {
                var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                if (result is not null)
                    return result;

                _logger.LogError("Math module call {Operation} returned an empty body", operation);
            }
            catch (Exception e) when (e is JsonException or NotSupportedException)
            {
                _logger.LogError(e, "Math module call {Operation} returned a body that does not match the v1 contract",
                    operation);
                throw new MarketDataUnavailableException(MarketDataUnavailableException.DefaultMessage, e);
            }

            throw new MarketDataUnavailableException();
        }
    }

    private async Task<Exception> MapFailureAsync(HttpResponseMessage response, string operation,
        CancellationToken cancellationToken)
    {
        var error = await TryReadErrorAsync(response, cancellationToken);
        var status = response.StatusCode;

        // 404 only means "unknown ticker" when the service says so: a 404 from a wrong BaseUrl has another code.
        if (status == HttpStatusCode.NotFound && error?.Code == "symbol_not_found")
        {
            _logger.LogInformation("Math module call {Operation}: {Message}", operation, error.Message);
            return new SymbolNotFoundException();
        }

        if (status is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity && error is not null)
        {
            _logger.LogWarning("Math module call {Operation} was refused ({Status} {Code}): {Message}",
                operation, (int)status, error.Code, error.Message);
            return new MathModuleRejectedException(error.Code, error.Message);
        }

        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            _logger.LogError(
                "Math module call {Operation} was rejected with {Status}: MathModule:ApiKey does not match MATH_API_KEY of the math service",
                operation, (int)status);
        else
            _logger.LogWarning("Math module call {Operation} failed with {Status} {Code}: {Message}",
                operation, (int)status, error?.Code, error?.Message);

        return new MarketDataUnavailableException();
    }

    private static async Task<ErrorResponse?> TryReadErrorAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, cancellationToken);
            return error is { Code: not null } ? error : null;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return null; // Not our error body: a proxy's HTML page, for example.
        }
    }
}
