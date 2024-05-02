using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class StockDataHistoryRepository: RepositoryBase<int, StockDataHistory>, IStockDataHistoryRepository
{
    public StockDataHistoryRepository(PortfolioManagerDbContext context): base(context) { }

}