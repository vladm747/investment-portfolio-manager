using PortfolioManager.BLL.Models;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;

namespace PortfolioManager.BLL.Interfaces;

public interface IPortfolioStatisticService
{
    Task<PortfolioGrowthResponse> GetPortfolioGrowsStats(int portfolioId);
    Task<IEnumerable<StockDTO>> GetDataForGrowthStats(int portfolioId);
    Task<PortfolioStatisticForOpt> GetDataForOpt(int portfolioId);
    ComparisonResult GetMetrics(int portfolioId);
}