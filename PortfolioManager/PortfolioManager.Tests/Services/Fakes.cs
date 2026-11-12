using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.Tests.Services;

/// <summary>The math module with answers set per test; every call is recorded.</summary>
public sealed class FakeMathModule : IMathModuleClient
{
    public Func<string, StockResponse> Stock { get; set; } = _ => throw new InvalidOperationException("GetStock not set up");
    public Func<IReadOnlyCollection<string>, PricesResponse> Prices { get; set; } = _ => throw new InvalidOperationException("GetPrices not set up");
    public Func<GrowthRequest, GrowthResponse> Growth { get; set; } = _ => throw new InvalidOperationException("GetGrowth not set up");
    public Func<OptimisationRequest, OptimisationResponse> Optimisation { get; set; } = _ => throw new InvalidOperationException("Optimise not set up");

    public List<string> StockCalls { get; } = new();
    public List<IReadOnlyCollection<string>> PriceCalls { get; } = new();
    public List<GrowthRequest> GrowthCalls { get; } = new();
    public List<OptimisationRequest> OptimisationCalls { get; } = new();

    /// <summary>Every call to the math module, of any kind.</summary>
    public int Calls => StockCalls.Count + PriceCalls.Count + GrowthCalls.Count + OptimisationCalls.Count;

    /// <summary>Forgets the recorded calls and the answers, for a fake that lives longer than one test.</summary>
    public void Reset()
    {
        Stock = _ => throw new InvalidOperationException("GetStock not set up");
        Prices = _ => throw new InvalidOperationException("GetPrices not set up");
        Growth = _ => throw new InvalidOperationException("GetGrowth not set up");
        Optimisation = _ => throw new InvalidOperationException("Optimise not set up");
        StockCalls.Clear();
        PriceCalls.Clear();
        GrowthCalls.Clear();
        OptimisationCalls.Clear();
    }

    public Task<StockResponse> GetStockAsync(string symbol, CancellationToken cancellationToken = default)
    {
        StockCalls.Add(symbol);
        return Task.FromResult(Stock(symbol));
    }

    public Task<PricesResponse> GetPricesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken = default)
    {
        PriceCalls.Add(symbols);
        return Task.FromResult(Prices(symbols));
    }

    public Task<GrowthResponse> GetGrowthAsync(GrowthRequest request, CancellationToken cancellationToken = default)
    {
        GrowthCalls.Add(request);
        return Task.FromResult(Growth(request));
    }

    public Task<OptimisationResponse> OptimiseAsync(OptimisationRequest request, CancellationToken cancellationToken = default)
    {
        OptimisationCalls.Add(request);
        return Task.FromResult(Optimisation(request));
    }
}

/// <summary>Portfolios in memory. Like the real repository, it only ever finds a portfolio together with its owner.</summary>
public sealed class FakePortfolioRepository : IPortfolioRepository
{
    public List<Portfolio> Portfolios { get; } = new();

    /// <summary>Adds a portfolio of the user with positions; the positions get this portfolio's id.</summary>
    public Portfolio Add(int id, string userId, params Stock[] stocks)
    {
        var portfolio = new Portfolio { Id = id, UserId = userId, Name = "P" + id, Stocks = stocks.ToList() };
        foreach (var stock in stocks)
        {
            stock.PortfolioId = id;
            stock.Portfolio = portfolio;
        }
        Portfolios.Add(portfolio);
        return portfolio;
    }

    public Task<IReadOnlyList<Portfolio>> GetAllAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Portfolio>>(Portfolios.Where(portfolio => portfolio.UserId == userId).ToList());

    public Task<Portfolio?> GetOwnedAsync(int portfolioId, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Portfolios.FirstOrDefault(portfolio => portfolio.Id == portfolioId && portfolio.UserId == userId));

    public Task<bool> IsOwnedByAsync(int portfolioId, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Portfolios.Any(portfolio => portfolio.Id == portfolioId && portfolio.UserId == userId));

    public Task<Portfolio> CreateAsync(Portfolio entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Portfolios.Count + 100;
        Portfolios.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<int> DeleteAsync(Portfolio entity, CancellationToken cancellationToken = default) =>
        Task.FromResult(Portfolios.Remove(entity) ? 1 : 0);

    public Task<Portfolio?> FindByKeyAsync(int key, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("An unscoped lookup would bypass the ownership check.");

    public Task<int> UpdateAsync(Portfolio entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<int> UpdateRangeAsync(IReadOnlyCollection<Portfolio> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

/// <summary>Positions in memory; a position's owner is the owner of its portfolio in <see cref="FakePortfolioRepository"/>.</summary>
public sealed class FakeStockRepository(FakePortfolioRepository portfolios) : IStockRepository
{
    public List<Stock> Stocks { get; } = new();

    /// <summary>Positions saved one at a time with UpdateAsync.</summary>
    public List<Stock> Updated { get; } = new();

    /// <summary>One entry per UpdateRangeAsync, which is one SaveChanges.</summary>
    public List<IReadOnlyCollection<Stock>> UpdatedInRange { get; } = new();

    public Stock Add(int portfolioId, string symbol, int quantity, decimal entryPrice, decimal currentPrice = 1)
    {
        var stock = new Stock
        {
            Id = Stocks.Count + 1, PortfolioId = portfolioId, Symbol = symbol, Quantity = quantity,
            EntryPrice = entryPrice, CurrentPrice = currentPrice
        };
        Stocks.Add(stock);
        return stock;
    }

    public Task<IReadOnlyList<Stock>> GetAllAsync(int portfolioId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Stock>>(Stocks.Where(stock => stock.PortfolioId == portfolioId).ToList());

    public Task<Stock?> FindOwnedAsync(int stockId, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Stocks.FirstOrDefault(stock => stock.Id == stockId
            && portfolios.Portfolios.Any(portfolio => portfolio.Id == stock.PortfolioId && portfolio.UserId == userId)));

    public Task<Stock?> FindByKeyAsync(int key, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("An unscoped lookup would bypass the ownership check.");

    public Task<Stock> CreateAsync(Stock entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Stocks.Count + 1;
        Stocks.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<int> UpdateAsync(Stock entity, CancellationToken cancellationToken = default)
    {
        Updated.Add(entity);
        return Task.FromResult(1);
    }

    public Task<int> UpdateRangeAsync(IReadOnlyCollection<Stock> entities, CancellationToken cancellationToken = default)
    {
        UpdatedInRange.Add(entities.ToList());
        return Task.FromResult(entities.Count);
    }

    public Task<int> DeleteAsync(Stock entity, CancellationToken cancellationToken = default) =>
        Task.FromResult(Stocks.Remove(entity) ? 1 : 0);
}
