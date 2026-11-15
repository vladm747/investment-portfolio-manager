using Microsoft.Extensions.Logging;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.BLL.Mapping;
using PortfolioManager.BLL.Models;
using PortfolioManager.BLL.Valuation;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.BLL.Services;

public class PortfolioStatisticService: IPortfolioStatisticService
{
    private readonly IPortfolioRepository _portRepo;
    private readonly IMathModuleClient _mathModule;
    private readonly ILogger<PortfolioStatisticService> _logger;

    public PortfolioStatisticService(IPortfolioRepository portfolioRepo, IMathModuleClient mathModule,
        ILogger<PortfolioStatisticService> logger)
    {
        _portRepo = portfolioRepo;
        _mathModule = mathModule;
        _logger = logger;
    }

    public async Task<PortfolioGrowthResponse> GetGrowthAsync(string userId, int portfolioId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetOwnedAsync(userId, portfolioId, cancellationToken);

        // A position of zero shares adds nothing to the value, and the math module refuses it.
        var positions = portfolio.Stocks!
            .Where(stock => stock.Quantity > 0)
            .Select(stock => new PositionRequest(stock.Symbol, stock.Quantity, DateOnly.FromDateTime(stock.EntryDate)))
            .ToList();

        if (positions.Count == 0)
            return new PortfolioGrowthResponse { GrowthData = new Dictionary<DateTime, decimal>() };

        var growth = await _mathModule.GetGrowthAsync(new GrowthRequest(positions), cancellationToken);

        return new PortfolioGrowthResponse
        {
            GrowthData = growth.Points.ToDictionary(
                point => point.Date.ToDateTime(TimeOnly.MinValue),
                point => (decimal)point.Value)
        };
    }

    public async Task<ComparisonResult> GetMetricsAsync(string userId, int portfolioId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetOwnedAsync(userId, portfolioId, cancellationToken);

        // The current weights are what the positions are worth right now, so they always match the portfolio
        // as it is: a stored copy would go stale on the first change. Weights are ratios for the optimiser,
        // which works in floating point, so this is where money stops being decimal.
        var (symbols, baseWeights) = PortfolioWeights.FromValues(
            portfolio.Stocks!.Select(stock => (stock.Symbol, (double)stock.Valuation().CurrentValue)));

        if (symbols.Count == 0)
            throw new ValidationFailedException(new[] { "У портфелі немає позицій, які можна оптимізувати." });

        _logger.LogInformation("Optimising portfolio {PortfolioId} with {Count} tickers", portfolioId, symbols.Count);

        var result = await _mathModule.OptimiseAsync(new OptimisationRequest(symbols, baseWeights), cancellationToken);

        var comparison = PortfolioComparer.Compare(result.Metrics.Base, result.Metrics.Optimised);

        // The client pairs the two lists by position, so the current weights follow the order of the optimised ones.
        comparison.Weights = new Weights
        {
            OptimizedWeights = new Dictionary<string, double>(result.Weights.Optimised),
            BaseWeights = result.Weights.Optimised.Keys
                .Select(symbol => result.Weights.Base.GetValueOrDefault(symbol))
                .ToArray()
        };

        comparison.RiskForecast = await TryForecastOptimisationAsync(portfolioId, symbols, baseWeights,
            result.Weights.Optimised, cancellationToken);

        return comparison;
    }

    public async Task<PortfolioRiskForecast> GetForecastAsync(string userId, int portfolioId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetOwnedAsync(userId, portfolioId, cancellationToken);

        // The same weights as the optimisation: what each position is worth now.
        var valuations = portfolio.Stocks!.Select(stock => (stock.Symbol, Valuation: stock.Valuation())).ToList();
        var (symbols, weights) = PortfolioWeights.FromValues(
            valuations.Select(position => (position.Symbol, (double)position.Valuation.CurrentValue)));

        if (symbols.Count == 0)
            throw new ValidationFailedException(new[] { "У портфелі немає позицій для прогнозу ризику." });

        _logger.LogInformation("Forecasting the risk of portfolio {PortfolioId} with {Count} tickers", portfolioId,
            symbols.Count);

        ForecastResponse forecast;
        try
        {
            forecast = await _mathModule.ForecastAsync(new ForecastRequest(symbols, weights), cancellationToken);
        }
        catch (MathModuleRejectedException rejection) when (RiskForecastMapping.TryExplain(rejection, out var message))
        {
            throw new ValidationFailedException(new[] { message });
        }

        var value = PositionValuation.Sum(valuations.Select(position => position.Valuation)).CurrentValue;
        return forecast.ToModel(value);
    }

    /// <summary>
    /// The forecast risk of the current and the optimised weights, or null when it cannot be made: the optimisation is
    /// worth showing without it.
    /// </summary>
    private async Task<OptimisationRiskForecast?> TryForecastOptimisationAsync(int portfolioId,
        IReadOnlyList<string> symbols, IReadOnlyList<double> weights, IReadOnlyDictionary<string, double> optimised,
        CancellationToken cancellationToken)
    {
        // The optimiser can leave a stock at 0 (or a rounding error below it); the forecast needs only the rest.
        var (optimisedSymbols, optimisedWeights) = PortfolioWeights.FromValues(
            optimised.Select(weight => (weight.Key, weight.Value)));
        if (optimisedSymbols.Count == 0)
            return null;

        try
        {
            // One after the other: the first call downloads what the service has not cached (the index and VIX),
            // the second finds it there.
            var current = await _mathModule.ForecastAsync(
                new ForecastRequest(symbols, weights, IncludeAssets: false), cancellationToken);
            var best = await _mathModule.ForecastAsync(
                new ForecastRequest(optimisedSymbols, optimisedWeights, IncludeAssets: false), cancellationToken);

            return new OptimisationRiskForecast(current.Model.ToModel(), current.AsOf, current.Portfolio.ToFigures(),
                best.Portfolio.ToFigures());
        }
        catch (Exception e) when (e is MathModuleRejectedException or MarketDataUnavailableException
                                      or SymbolNotFoundException)
        {
            // The client has logged the details of the failure already.
            _logger.LogWarning("The optimisation of portfolio {PortfolioId} is returned without a risk forecast: {Reason}",
                portfolioId, e.Message);
            return null;
        }
    }

    private async Task<Portfolio> GetOwnedAsync(string userId, int portfolioId, CancellationToken cancellationToken)
    {
        var portfolio = await _portRepo.GetOwnedAsync(portfolioId, userId, cancellationToken)
                        ?? throw NotFoundException.Portfolio();

        portfolio.Stocks ??= new List<Stock>();
        return portfolio;
    }
}
