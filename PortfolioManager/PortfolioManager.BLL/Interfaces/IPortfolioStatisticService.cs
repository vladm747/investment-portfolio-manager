using PortfolioManager.BLL.Models;

namespace PortfolioManager.BLL.Interfaces;

/// <summary>
/// Every method takes the id of the signed-in user and treats a portfolio of another user as missing
/// (<see cref="Exceptions.NotFoundException"/>).
/// </summary>
public interface IPortfolioStatisticService
{
    Task<PortfolioGrowthResponse> GetGrowthAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);

    /// <summary>Current portfolio against the max-Sharpe one. The current weights come from the live positions.</summary>
    Task<ComparisonResult> GetMetricsAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);
}
