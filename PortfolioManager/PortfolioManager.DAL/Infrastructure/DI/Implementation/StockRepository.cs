using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class StockRepository: RepositoryBase<int, Stock>, IStockRepository
{
    public StockRepository(PortfolioManagerDbContext context): base(context) { }

    public async Task<IReadOnlyList<Stock>> GetAllAsync(int portfolioId, CancellationToken cancellationToken = default)
    {
        return await Table.Where(item => item.PortfolioId == portfolioId).ToListAsync(cancellationToken);
    }

    public async Task<Stock?> FindOwnedAsync(int stockId, string userId, CancellationToken cancellationToken = default)
    {
        return await Table.Where(item => item.Id == stockId && item.Portfolio!.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
