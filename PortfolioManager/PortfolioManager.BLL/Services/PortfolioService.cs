using AutoMapper;
using Newtonsoft.Json;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;
using YahooFinance.Client.Interfaces;

namespace PortfolioManager.BLL.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioStatisticForOptRepository _portfolioStatRepository;
    private readonly IMapper _mapper;

    public PortfolioService(IPortfolioRepository portfolioRepository, IMapper mapper, 
        IPortfolioStatisticForOptRepository portfolioStatRepository)
    {
        _mapper = mapper;
        _portfolioRepository = portfolioRepository;
        _portfolioStatRepository = portfolioStatRepository;
    }

    public async Task<IEnumerable<PortfolioDTO>> GetAllAsync(string userId)
    {
        var portfolios = await _portfolioRepository.GetAllAsync(userId);
        var dtos = _mapper.Map<IEnumerable<PortfolioDTO>>(portfolios).ToList();
        foreach (var dto in dtos)
        {
            if(dto.Stocks == null || dto.Stocks.Count == 0)
                continue;
            dto.TotalCurrentPrice = dto.Stocks.Sum(s => s.TotalCurrentPrice);
            dto.TotalGain = dto.TotalCurrentPrice - dto.TotalEntryPrice;
            dto.TotalGainPercentage = (dto.TotalGain / dto.TotalEntryPrice) * 100;
        }

        return dtos;
    }

    public async Task<PortfolioDTO> GetAsync(int portfolioId)
    {
        var portfolio = await _portfolioRepository.GetAsync(portfolioId);

        if (portfolio == null)
            throw new InvalidOperationException("There is not portfolio with this id");

        return _mapper.Map<PortfolioDTO>(portfolio);
    }

    public async Task<int> DeleteAsync(int portfolioId)
    {
        var entity = await _portfolioRepository.GetAsync(portfolioId);
        
        if (entity == null)
            throw new InvalidOperationException("There is no portfolio with this id");
        
        return await _portfolioRepository.DeleteAsync(entity);
    }

    public async Task<PortfolioDTO> CreatePortfolioAsync(CreatePortfolioDTO createPortfolio)
    {
        var model = _mapper.Map<Portfolio>(createPortfolio);
        var createdModel = await _portfolioRepository.CreateAsync(model);
        
        if (createdModel == null)
            throw new Exception("Can't create a createPortfolio");
        
        return _mapper.Map<PortfolioDTO>(createdModel);
    }
    
    public async Task UpdateTotalPortfolioValue(int portfolioId)
    {
        var portfolio = await _portfolioRepository.GetAsync(portfolioId);

        if (portfolio == null)
            throw new InvalidOperationException("There is no portfolio with this id");

        portfolio.TotalEntryPrice = portfolio.Stocks.Sum(s => s.EntryPrice * s.Quantity);
        portfolio.TotalCurrenPrice = portfolio.Stocks.Sum(s => s.TotalCurrentPrice);
        portfolio.TotalGain = portfolio.TotalCurrenPrice - portfolio.TotalEntryPrice;
        portfolio.TotalGainPercentage = portfolio.TotalGain / portfolio.TotalEntryPrice * 100;
        
        List<string> symbols = new List<string>();
        List<double> weights = new List<double>();
        foreach (var stock in portfolio.Stocks)
        {
            weights.Add((double)(stock.TotalCurrentPrice / portfolio.TotalCurrenPrice));
            symbols.Add(stock.Symbol);
        }

        var portfolioStat = new PortfolioStatisticForOpt
        {
            LastUpdate = DateTime.Now,
            PortfolioId = portfolioId,
            Symbols = JsonConvert.SerializeObject(symbols.ToArray()),
            Weights = JsonConvert.SerializeObject(weights.ToArray())
        };
        
        await _portfolioStatRepository.CreateAsync(portfolioStat);
        
        var result = await _portfolioRepository.UpdateAsync(portfolio);
    }
}