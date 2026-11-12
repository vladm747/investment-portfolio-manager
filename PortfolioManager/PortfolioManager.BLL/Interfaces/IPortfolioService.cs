using PortfolioManager.Common.DTO;

namespace PortfolioManager.BLL.Interfaces;

/// <summary>
/// Every method takes the id of the signed-in user and treats a portfolio of another user as missing
/// (<see cref="Exceptions.NotFoundException"/>).
/// </summary>
public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioDTO>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
    Task<PortfolioDTO> GetAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(string userId, int portfolioId, CancellationToken cancellationToken = default);
    Task<PortfolioDTO> CreatePortfolioAsync(string userId, CreatePortfolioDTO createPortfolio, CancellationToken cancellationToken = default);
}
