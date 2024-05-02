using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

public interface IStockDataHistoryRepository: IRepositoryBase<int, StockDataHistory>
{
    
}