using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class StockSymbolRepository(PortfolioManagerDbContext context) : IStockSymbolRepository
{
    private readonly PortfolioManagerDbContext _context = context;

    public async Task<StockSymbol> GetBySymbol(string symbol)
    {
        return await _context.StockSymbols.Where(item => item.Symbol == symbol).FirstOrDefaultAsync();
    }
}