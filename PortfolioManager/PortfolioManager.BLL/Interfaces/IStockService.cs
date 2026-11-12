using PortfolioManager.Common.DTO;

namespace PortfolioManager.BLL.Interfaces;

/// <summary>
/// Every method takes the id of the signed-in user and treats a portfolio or position of another user as missing
/// (<see cref="Exceptions.NotFoundException"/>).
/// </summary>
public interface IStockService
{
    Task<IReadOnlyList<StockDTO>> GetAllAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);
    /// <summary>Adds a position to the portfolio. The ticker is checked, and its current price fetched, by the math module.</summary>
    Task<StockDTO> CreateAsync(string userId, int portfolioId, CreateStockDTO request, CancellationToken cancellationToken = default);
    Task<StockDTO> UpdateAsync(string userId, int stockId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Refreshes the current price of every position of the portfolio: one price request, one save.</summary>
    Task UpdatePricesAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string userId, int stockId, CancellationToken cancellationToken = default);
}
