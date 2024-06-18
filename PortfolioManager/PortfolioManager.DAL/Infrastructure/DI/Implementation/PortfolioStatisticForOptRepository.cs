using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class PortfolioStatisticForOptRepository(PortfolioManagerDbContext context)
    : RepositoryBase<int, PortfolioStatisticForOpt>(context), IPortfolioStatisticForOptRepository
{
    public async Task<PortfolioStatisticForOpt?> GetByPortfolioId(int portfolioId)
    {
        return await Table.OrderByDescending(item=>item.LastUpdate).FirstOrDefaultAsync(item => item.PortfolioId == portfolioId);
    }
}