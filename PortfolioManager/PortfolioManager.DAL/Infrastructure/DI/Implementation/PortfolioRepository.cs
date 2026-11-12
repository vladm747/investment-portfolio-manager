using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class PortfolioRepository(PortfolioManagerDbContext context)
    : RepositoryBase<int, Portfolio>(context), IPortfolioRepository
{
    public async Task<IReadOnlyList<Portfolio>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await Table.Where(item => item.UserId == userId)
            .Include(item => item.Stocks)
            .ToListAsync(cancellationToken);
    }

    public async Task<Portfolio?> GetOwnedAsync(int portfolioId, string userId, CancellationToken cancellationToken = default)
    {
        return await Table.Where(item => item.Id == portfolioId && item.UserId == userId)
            .Include(item => item.Stocks)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsOwnedByAsync(int portfolioId, string userId, CancellationToken cancellationToken = default)
    {
        return await Table.AnyAsync(item => item.Id == portfolioId && item.UserId == userId, cancellationToken);
    }
}
