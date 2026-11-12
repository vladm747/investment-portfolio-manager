using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Implementation;

namespace PortfolioManager.Tests.Data;

/// <summary>
/// The real repositories over EF Core's in-memory provider: the LINQ that decides who may see a portfolio or
/// a position, and the one-save batch update. Each test gets its own database.
/// </summary>
public class RepositoryTests
{
    private const string Owner = "user-1";
    private const string Intruder = "user-2";

    private readonly string _database = "repository-tests-" + Guid.NewGuid();

    private PortfolioManagerDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PortfolioManagerDbContext>().UseInMemoryDatabase(_database).Options,
            new ConfigurationBuilder().Build());

    /// <summary>Portfolio 1 and 2 are the owner's, 3 is the intruder's. Positions: 1 and 2 in portfolio 1, 3 in portfolio 3.</summary>
    public RepositoryTests()
    {
        using var context = NewContext();
        context.Portfolios.AddRange(
            new Portfolio { Id = 1, Name = "Mine", UserId = Owner },
            new Portfolio { Id = 2, Name = "Empty", UserId = Owner },
            new Portfolio { Id = 3, Name = "Theirs", UserId = Intruder });
        context.Stocks.AddRange(
            new Stock { Id = 1, PortfolioId = 1, Symbol = "AAPL", Quantity = 2, EntryPrice = 100, CurrentPrice = 100 },
            new Stock { Id = 2, PortfolioId = 1, Symbol = "MSFT", Quantity = 1, EntryPrice = 300, CurrentPrice = 300 },
            new Stock { Id = 3, PortfolioId = 3, Symbol = "TSLA", Quantity = 5, EntryPrice = 200, CurrentPrice = 200 });
        context.SaveChanges();
    }

    // ---- portfolios -------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetOwned_returns_the_portfolio_with_its_positions_to_its_owner()
    {
        await using var context = NewContext();

        var portfolio = await new PortfolioRepository(context).GetOwnedAsync(1, Owner);

        Assert.NotNull(portfolio);
        Assert.Equal(new[] { "AAPL", "MSFT" }, portfolio.Stocks!.Select(stock => stock.Symbol).OrderBy(symbol => symbol));
    }

    [Fact]
    public async Task GetOwned_returns_nothing_to_anyone_else()
    {
        await using var context = NewContext();

        Assert.Null(await new PortfolioRepository(context).GetOwnedAsync(1, Intruder));
        Assert.Null(await new PortfolioRepository(context).GetOwnedAsync(3, Owner));
    }

    [Fact]
    public async Task GetOwned_returns_nothing_for_a_portfolio_that_does_not_exist()
    {
        await using var context = NewContext();

        Assert.Null(await new PortfolioRepository(context).GetOwnedAsync(404, Owner));
    }

    [Fact]
    public async Task IsOwnedBy_is_true_only_for_the_owner()
    {
        await using var context = NewContext();
        var repository = new PortfolioRepository(context);

        Assert.True(await repository.IsOwnedByAsync(1, Owner));
        Assert.False(await repository.IsOwnedByAsync(1, Intruder));
        Assert.False(await repository.IsOwnedByAsync(404, Owner));
    }

    [Fact]
    public async Task GetAll_returns_only_the_portfolios_of_the_user()
    {
        await using var context = NewContext();

        var portfolios = await new PortfolioRepository(context).GetAllAsync(Owner);

        Assert.Equal(new[] { 1, 2 }, portfolios.Select(portfolio => portfolio.Id).OrderBy(id => id));
        Assert.Empty(await new PortfolioRepository(context).GetAllAsync("nobody"));
    }

    // ---- positions --------------------------------------------------------------------------------------------

    [Fact]
    public async Task FindOwned_finds_a_position_through_the_owner_of_its_portfolio()
    {
        await using var context = NewContext();
        var repository = new StockRepository(context);

        Assert.Equal("AAPL", (await repository.FindOwnedAsync(1, Owner))?.Symbol);
        Assert.Null(await repository.FindOwnedAsync(1, Intruder));
        Assert.Null(await repository.FindOwnedAsync(3, Owner));
        Assert.Null(await repository.FindOwnedAsync(404, Owner));
    }

    [Fact]
    public async Task GetAll_of_a_portfolio_returns_only_its_positions()
    {
        await using var context = NewContext();

        var stocks = await new StockRepository(context).GetAllAsync(1);

        Assert.Equal(new[] { "AAPL", "MSFT" }, stocks.Select(stock => stock.Symbol).OrderBy(symbol => symbol));
    }

    // ---- batch update -----------------------------------------------------------------------------------------

    [Fact]
    public async Task UpdateRange_saves_all_the_positions_with_one_SaveChanges()
    {
        await using var context = NewContext();
        var repository = new StockRepository(context);
        var stocks = (await repository.GetAllAsync(1)).ToList();
        var saves = 0;
        context.SavingChanges += (_, _) => saves++;
        foreach (var stock in stocks)
            stock.CurrentPrice = 999;

        var changed = await repository.UpdateRangeAsync(stocks);

        Assert.Equal(1, saves);
        Assert.Equal(2, changed);
        await using var fresh = NewContext();
        Assert.All(await new StockRepository(fresh).GetAllAsync(1), stock => Assert.Equal(999, stock.CurrentPrice));
        Assert.Equal(200, (await new StockRepository(fresh).FindOwnedAsync(3, Intruder))!.EntryPrice); // others untouched
    }

    [Fact]
    public async Task Create_and_delete_work_with_a_cancellation_token()
    {
        await using var context = NewContext();
        var repository = new StockRepository(context);

        var created = await repository.CreateAsync(
            new Stock { PortfolioId = 2, Symbol = "NVDA", Quantity = 1 }, CancellationToken.None);
        Assert.True(created.Id > 0);

        Assert.Equal(1, await repository.DeleteAsync(created, CancellationToken.None));
        Assert.Null(await repository.FindOwnedAsync(created.Id, Owner));
    }

    [Fact]
    public async Task A_cancelled_token_stops_the_query()
    {
        await using var context = NewContext();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new PortfolioRepository(context).GetOwnedAsync(1, Owner, cancelled.Token));
    }
}
