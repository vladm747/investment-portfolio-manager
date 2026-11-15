using PortfolioManager.BLL.Models;

namespace PortfolioManager.BLL.Interfaces;

/// <summary>
/// Every method takes the id of the signed-in user and treats a portfolio of another user as missing
/// (<see cref="Exceptions.NotFoundException"/>).
/// </summary>
public interface IPortfolioStatisticService
{
    Task<PortfolioGrowthResponse> GetGrowthAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Current portfolio against the max-Sharpe one. The current weights come from the live positions. The forecast risk
    /// of both is added when the model can make it.
    /// </summary>
    Task<ComparisonResult> GetMetricsAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Volatility, VaR 95 % and maximum drawdown the trained model expects over the next three months, for the
    /// portfolio (weighted by what its positions are worth now) and each of its stocks. An empty portfolio or a stock
    /// listed less than a year ago is a <see cref="Exceptions.ValidationFailedException"/> in Ukrainian.
    /// </summary>
    Task<PortfolioRiskForecast> GetForecastAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);
}
