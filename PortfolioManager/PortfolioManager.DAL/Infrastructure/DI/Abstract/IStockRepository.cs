using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

public interface IStockRepository:  IRepositoryBase<int, Stock>
{
    Task<IEnumerable<Stock>> GetAllAsync(int portfolioId);
}