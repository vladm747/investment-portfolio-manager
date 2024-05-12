﻿using AutoMapper;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using YahooFinance.Client.Interfaces;

namespace PortfolioManager.BLL.Services;

public class StockService: IStockService
{
    private readonly IStockRepository _repo;
    private readonly IStockSymbolRepository _stockSymbolRepo;
    private readonly IPortfolioService _portfolioService;
    private readonly IMapper _mapper;

    private readonly IYahooFinanceService _yahooFinanceService;
    public StockService(IStockRepository stockRepository, IMapper mapper, IYahooFinanceService yahooFinanceService,
        IStockSymbolRepository stockSymbolRepository, IPortfolioService portfolioService)
    {
        _repo = stockRepository;
        _yahooFinanceService = yahooFinanceService;
        _stockSymbolRepo = stockSymbolRepository;
        _mapper = mapper;
        _portfolioService = portfolioService;
    }

    public async Task<IEnumerable<StockDTO>> GetAllAsync(int portfolioId)
    {
        var stocks = _repo.GetAllAsync(portfolioId);

        return _mapper.Map<IEnumerable<StockDTO>>(stocks).ToList();
    }

    public async Task<StockDTO> CreateAsync(int portfolioId, int quantity, string symbol, decimal entryPrice)
    {
        var stock = await _stockSymbolRepo.GetBySymbol(symbol);
        
        if (stock == null || stock.Symbol != symbol)
            throw new Exception("Stock symbol is invalid");
        
        var stockInfo = await _yahooFinanceService.GetInfoAsync(symbol);
           if (stockInfo == null)
                throw new Exception("Stock info not found");
    
        stockInfo.StockDTO.Symbol = symbol;
        stockInfo.StockDTO.Quantity = quantity;
        stockInfo.StockDTO.PortfolioId = portfolioId;
        stockInfo.StockDTO.EntryPrice = entryPrice;
        stockInfo.StockDTO.TotalValue = stockInfo.StockDTO.Quantity * stockInfo.StockDTO.CurrentPrice;
        stockInfo.StockDTO.Gain = (decimal)(stockInfo.StockDTO.TotalValue - stockInfo.StockDTO.Quantity * stockInfo.StockDTO.EntryPrice);
      
        var model = _mapper.Map<Stock>(stockInfo.StockDTO);
        var result = await _repo.CreateAsync(model);
       
        if (result == null)
            throw new Exception("Stock not created");
        
        await _portfolioService.UpdateTotalPortfolioValue(portfolioId);

        return _mapper.Map<StockDTO>(result);
    }

    public async Task<StockDTO> UpdateAsync(int stockId, int quantity)
    {
        var entity = await _repo.FindByKeyAsync(stockId);
        
        if (entity == null)
            throw new InvalidOperationException("The entity you are trying to update does not exist in database!");

        entity.Quantity = quantity;
        entity.TotalValue = entity.Quantity * entity.CurrentPrice;
        
        var result = await _repo.UpdateAsync(entity);

        if (result == 0)
            throw new Exception("Stock not updated!");
        
        var updatedStock = await _repo.FindByKeyAsync(stockId);

        return _mapper.Map<StockDTO>(updatedStock);
    }

    public async Task UpdateInfo(int portfolioId)
    {
        var stocks = await _repo.GetAllAsync(portfolioId);
        
        foreach (var stock in stocks)
        {
            var stockPriceResponseDto = _yahooFinanceService.GetPriceAsync(stock.Symbol);
            
            if(!stockPriceResponseDto.IsSuccess)
                throw new Exception("Stock price not found");
            
            stock.CurrentPrice = (decimal)stockPriceResponseDto.CurrentPrice!;
            stock.TotalValue = stock.Quantity * stock.CurrentPrice;
            stock.Gain = stock.Quantity * stock.CurrentPrice - stock.Quantity * stock.EntryPrice;
            var result = await _repo.UpdateAsync(stock);
            await _portfolioService.UpdateTotalPortfolioValue(portfolioId);
        }
    }

    public async Task<bool> DeleteAsync(int stockId)
    {
        var entity = await _repo.FindByKeyAsync(stockId);

        if (entity == null)
            throw new InvalidOperationException("The entity you are trying to delete does not exist in database!");
        
        var stock = _mapper.Map<Stock>(entity);
        var result = await _repo.DeleteAsync(stock);
        
        return result > 0;
    }   
}