using AutoMapper;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using YahooFinance.Client.Interfaces;

namespace PortfolioManager.BLL.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IMapper _mapper;

    public PortfolioService(IPortfolioRepository portfolioRepository, IMapper mapper)
    {
        _mapper = mapper;
        _portfolioRepository = portfolioRepository;
    }

    public async Task<IEnumerable<PortfolioDTO>> GetAllAsync()
    {
        var portfolios = await _portfolioRepository.GetAllAsync();

        return _mapper.Map<IEnumerable<PortfolioDTO>>(portfolios).ToList();
    }

    public async Task<PortfolioDTO> GetAsync(int portfolioId)
    {
        var portfolio = await _portfolioRepository.GetAsync(portfolioId);

        if (portfolio == null)
            throw new InvalidOperationException("There is not portfolio with this id");

        return _mapper.Map<PortfolioDTO>(portfolio);
    }

    public async Task UpdateTotalPortfolioValue(int portfolioId)
    {
        var portfolio = await _portfolioRepository.GetAsync(portfolioId);

        if (portfolio == null)
            throw new InvalidOperationException("There is not portfolio with this id");

        portfolio.TotalEntryPrice = portfolio.Stocks.Sum(s => s.EntryPrice * s.Quantity);
        portfolio.TotalCurrenPrice = portfolio.Stocks.Sum(s => s.TotalValue);
        portfolio.TotalGain = portfolio.TotalCurrenPrice - portfolio.TotalEntryPrice;
        
        var result = await _portfolioRepository.UpdateAsync(portfolio);
    }
}