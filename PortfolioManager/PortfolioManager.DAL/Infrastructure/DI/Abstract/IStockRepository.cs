using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

public interface IStockRepository:  IRepositoryBase<int, Stock>
{
    /// <summary>The positions of one portfolio. The caller has already checked that the portfolio is the user's.</summary>
    Task<IReadOnlyList<Stock>> GetAllAsync(int portfolioId, CancellationToken cancellationToken = default);

    /// <summary>The position, or null when it does not exist or its portfolio belongs to another user.</summary>
    Task<Stock?> FindOwnedAsync(int stockId, string userId, CancellationToken cancellationToken = default);
}
