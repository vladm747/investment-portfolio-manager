using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class PortfolioRepository: RepositoryBase<int, Portfolio>, IPortfolioRepository
{
    public PortfolioRepository(PortfolioManagerDbContext context): base(context) { }
    
    public async Task<IEnumerable<Portfolio>> GetAllAsync()
    {
        return await Table.Select(item => item).Include(item => item.User)
            .Include(item => item.Stocks).ToListAsync();
    }
}