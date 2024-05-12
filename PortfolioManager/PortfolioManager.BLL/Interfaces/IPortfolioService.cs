using PortfolioManager.Common.DTO;

namespace PortfolioManager.BLL.Interfaces;

public interface IPortfolioService
{
    Task<IEnumerable<PortfolioDTO>> GetAllAsync();
    Task<PortfolioDTO> GetAsync(int portfolioId);
    Task UpdateTotalPortfolioValue(int portfolioId);
}