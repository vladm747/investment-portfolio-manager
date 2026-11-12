using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

/// <summary>
/// Every lookup is by the owner's id as well, so a service cannot read somebody else's portfolio by forgetting
/// a check: there is no query that takes only the portfolio id.
/// </summary>
public interface IPortfolioRepository: IRepositoryBase<int, Portfolio>
{
    /// <summary>The user's portfolios with their positions.</summary>
    Task<IReadOnlyList<Portfolio>> GetAllAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>The portfolio with its positions, or null when it does not exist or belongs to another user.</summary>
    Task<Portfolio?> GetOwnedAsync(int portfolioId, string userId, CancellationToken cancellationToken = default);

    Task<bool> IsOwnedByAsync(int portfolioId, string userId, CancellationToken cancellationToken = default);
}
