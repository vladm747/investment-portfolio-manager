using PortfolioManager.DAL.Entities;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

public interface IStockSymbolRepository
{
    public Task<StockSymbol> GetBySymbol(string symbol);
}