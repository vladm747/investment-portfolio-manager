using Microsoft.Extensions.Logging;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.BLL.Mapping;
using PortfolioManager.BLL.Valuation;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.MathModule.Client;

namespace PortfolioManager.BLL.Services;

public class StockService: IStockService
{
    private readonly IStockRepository _repo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IMathModuleClient _mathModule;
    private readonly ILogger<StockService> _logger;

    public StockService(IStockRepository stockRepository, IPortfolioRepository portfolioRepository,
        IMathModuleClient mathModule, ILogger<StockService> logger)
    {
        _repo = stockRepository;
        _portfolioRepo = portfolioRepository;
        _mathModule = mathModule;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StockDTO>> GetAllAsync(string userId, int portfolioId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePortfolioIsOwnedAsync(userId, portfolioId, cancellationToken);

        var stocks = await _repo.GetAllAsync(portfolioId, cancellationToken);

        return stocks.Select(stock => stock.ToDto()).ToList();
    }

    public async Task<StockDTO> CreateAsync(string userId, int portfolioId, CreateStockDTO request,
        CancellationToken cancellationToken = default)
    {
        // Before anything else: a portfolio of somebody else must not even cost a market data request.
        await EnsurePortfolioIsOwnedAsync(userId, portfolioId, cancellationToken);

        // The math module is the only judge of a ticker: it throws SymbolNotFoundException for an unknown one
        // and MarketDataUnavailableException when Yahoo Finance cannot be reached. The exception filter maps both.
        var info = await _mathModule.GetStockAsync(request.Symbol, cancellationToken);

        if (info.Price is not { } currentPrice)
            throw new MarketDataUnavailableException($"Для тікера {info.Symbol} немає поточної ціни.");

        var stock = new Stock
        {
            Symbol = info.Symbol,
            Name = info.Name ?? info.Symbol,
            Currency = info.Currency ?? string.Empty,
            Sector = SectorMapper.FromYahoo(info.Sector),
            CurrentPrice = Money.FromMarketPrice(currentPrice),
            EntryDate = request.EntryDate,
            Quantity = request.Quantity,
            EntryPrice = Money.Round(request.EntryPrice),
            PortfolioId = portfolioId
        };

        var created = await _repo.CreateAsync(stock, cancellationToken);

        return created.ToDto();
    }

    public async Task<StockDTO> UpdateAsync(string userId, int stockId, int quantity,
        CancellationToken cancellationToken = default)
    {
        var stock = await _repo.FindOwnedAsync(stockId, userId, cancellationToken)
                    ?? throw NotFoundException.Position();

        stock.Quantity = quantity;

        await _repo.UpdateAsync(stock, cancellationToken);

        return stock.ToDto();
    }

    public async Task UpdatePricesAsync(string userId, int portfolioId, CancellationToken cancellationToken = default)
    {
        await EnsurePortfolioIsOwnedAsync(userId, portfolioId, cancellationToken);

        var stocks = await _repo.GetAllAsync(portfolioId, cancellationToken);

        if (stocks.Count == 0)
            return;

        // One call for the whole portfolio. Nothing is written unless every stock got a price.
        var symbols = stocks.Select(stock => stock.Symbol).Distinct().ToList();
        var response = await _mathModule.GetPricesAsync(symbols, cancellationToken);
        var prices = new Dictionary<string, double>(response.Prices, StringComparer.OrdinalIgnoreCase);

        var missing = symbols.Where(symbol => !prices.ContainsKey(symbol)).ToList();
        if (missing.Count > 0)
            throw new MarketDataUnavailableException($"Не вдалося отримати ціни для: {string.Join(", ", missing)}.");

        foreach (var stock in stocks)
            stock.CurrentPrice = Money.FromMarketPrice(prices[stock.Symbol]);

        // One save for all positions: the portfolio is never left half refreshed. Values, gains and totals are not
        // stored, so there is nothing else to recalculate.
        await _repo.UpdateRangeAsync(stocks, cancellationToken);

        _logger.LogInformation("Refreshed the prices of {Count} positions ({Symbols} tickers) in portfolio {PortfolioId}",
            stocks.Count, symbols.Count, portfolioId);
    }

    public async Task<bool> DeleteAsync(string userId, int stockId, CancellationToken cancellationToken = default)
    {
        var stock = await _repo.FindOwnedAsync(stockId, userId, cancellationToken)
                    ?? throw NotFoundException.Position();

        var deleted = await _repo.DeleteAsync(stock, cancellationToken);

        return deleted > 0;
    }

    private async Task EnsurePortfolioIsOwnedAsync(string userId, int portfolioId, CancellationToken cancellationToken)
    {
        if (!await _portfolioRepo.IsOwnedByAsync(portfolioId, userId, cancellationToken))
            throw NotFoundException.Portfolio();
    }
}
