using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class StockRepository: RepositoryBase<int, Stock>, IStockRepository
{
    public StockRepository(PortfolioManagerDbContext context): base(context) { }
    
    public async Task<IEnumerable<Stock>> GetAllAsync(int portfolioId)
    {
        return await Table.Select(item => item).Where(item => item.PortfolioId == portfolioId).ToListAsync();
    }
}