using Microsoft.Extensions.Logging;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces;
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

        return comparison;
    }

    private async Task<Portfolio> GetOwnedAsync(string userId, int portfolioId, CancellationToken cancellationToken)
    {
        var portfolio = await _portRepo.GetOwnedAsync(portfolioId, userId, cancellationToken)
                        ?? throw NotFoundException.Portfolio();

        portfolio.Stocks ??= new List<Stock>();
        return portfolio;
    }
}
