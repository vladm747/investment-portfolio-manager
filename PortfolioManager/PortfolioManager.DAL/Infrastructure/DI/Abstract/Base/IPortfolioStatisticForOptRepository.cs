using PortfolioManager.DAL.Entities;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

public interface IPortfolioStatisticForOptRepository:IRepositoryBase<int, PortfolioStatisticForOpt>
{
    Task<PortfolioStatisticForOpt?> GetByPortfolioId(int portfolioId);
}