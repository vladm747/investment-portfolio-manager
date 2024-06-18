using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;

namespace PortfolioManager.BLL.Interfaces;

public interface IPortfolioService
{
    Task<IEnumerable<PortfolioDTO>> GetAllAsync(string userId);
    Task<PortfolioDTO> GetAsync(int portfolioId);
    Task<int> DeleteAsync(int portfolioId);
    Task<PortfolioDTO> CreatePortfolioAsync(CreatePortfolioDTO createPortfolio);
    Task UpdateTotalPortfolioValue(int portfolioId);
}