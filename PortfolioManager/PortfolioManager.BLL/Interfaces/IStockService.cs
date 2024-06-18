using PortfolioManager.Common;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.BLL.Interfaces;

public interface IStockService
{
    Task<IEnumerable<StockDTO>> GetAllAsync(int portfolioId);
    Task<StockDTO> CreateAsync(int portfolioId, int quantity, string symbol, double entryPrice, DateTime entryDate);
    Task<StockDTO> UpdateAsync(int portfolioId, int quantity);
    Task UpdateInfo(int portfolioId);
    Task<bool> DeleteAsync(int stockId);
}