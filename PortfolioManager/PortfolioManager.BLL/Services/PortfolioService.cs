using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.BLL.Mapping;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;

namespace PortfolioManager.BLL.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;

    public PortfolioService(IPortfolioRepository portfolioRepository)
    {
        _portfolioRepository = portfolioRepository;
    }

    public async Task<IReadOnlyList<PortfolioDTO>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var portfolios = await _portfolioRepository.GetAllAsync(userId, cancellationToken);

        return portfolios.Select(portfolio => portfolio.ToDto()).ToList();
    }

    public async Task<PortfolioDTO> GetAsync(string userId, int portfolioId, CancellationToken cancellationToken = default)
    {
        var portfolio = await _portfolioRepository.GetOwnedAsync(portfolioId, userId, cancellationToken)
                        ?? throw NotFoundException.Portfolio();

        return portfolio.ToDto();
    }

    public async Task<int> DeleteAsync(string userId, int portfolioId, CancellationToken cancellationToken = default)
    {
        var portfolio = await _portfolioRepository.GetOwnedAsync(portfolioId, userId, cancellationToken)
                        ?? throw NotFoundException.Portfolio();

        return await _portfolioRepository.DeleteAsync(portfolio, cancellationToken);
    }

    public async Task<PortfolioDTO> CreatePortfolioAsync(string userId, CreatePortfolioDTO createPortfolio,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await _portfolioRepository.CreateAsync(
            new Portfolio { Name = createPortfolio.Name.Trim(), UserId = userId }, cancellationToken);

        return portfolio.ToDto();
    }
}
