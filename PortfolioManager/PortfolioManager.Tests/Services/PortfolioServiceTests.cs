using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Services;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;

namespace PortfolioManager.Tests.Services;

/// <summary>PortfolioService over a fake repository: who sees which portfolio, and where the totals come from.</summary>
public class PortfolioServiceTests
{
    private const string Owner = "user-1";
    private const string Intruder = "user-2";

    private readonly FakePortfolioRepository _portfolios = new();

    private PortfolioService Service() => new(_portfolios);

    private static Stock Position(string symbol, int quantity, decimal entryPrice, decimal currentPrice) =>
        new() { Symbol = symbol, Quantity = quantity, EntryPrice = entryPrice, CurrentPrice = currentPrice };

    // ---- totals -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_totals_are_worked_out_from_the_positions()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 2, 100, 150), Position("MSFT", 1, 300, 330));

        var portfolio = await Service().GetAsync(Owner, 1);

        Assert.Equal(500, portfolio.TotalEntryPrice); // 2 * 100 + 300
        Assert.Equal(630, portfolio.TotalCurrentPrice); // 2 * 150 + 330
        Assert.Equal(130, portfolio.TotalGain);
        Assert.Equal(26, portfolio.TotalGainPercentage);
    }

    [Fact]
    public async Task The_totals_follow_a_change_to_a_position_the_next_time_they_are_read()
    {
        var aapl = Position("AAPL", 2, 100, 150);
        _portfolios.Add(1, Owner, aapl);
        var before = await Service().GetAsync(Owner, 1);

        aapl.Quantity = 4;
        var after = await Service().GetAsync(Owner, 1);

        Assert.Equal(300, before.TotalCurrentPrice);
        Assert.Equal(600, after.TotalCurrentPrice);
        Assert.Equal(400, after.TotalEntryPrice);
    }

    [Fact]
    public async Task A_portfolio_without_positions_has_zero_totals_not_NaN()
    {
        _portfolios.Add(1, Owner);

        var portfolio = await Service().GetAsync(Owner, 1);

        Assert.Equal(0, portfolio.TotalEntryPrice);
        Assert.Equal(0, portfolio.TotalCurrentPrice);
        Assert.Equal(0, portfolio.TotalGain);
        Assert.Equal(0, portfolio.TotalGainPercentage);
        Assert.Empty(portfolio.Stocks!);
    }

    [Fact]
    public async Task A_portfolio_with_a_position_of_zero_shares_has_finite_totals()
    {
        _portfolios.Add(1, Owner, Position("OLD", 0, 100, 150));

        var portfolio = await Service().GetAsync(Owner, 1);

        Assert.Equal(0, portfolio.TotalGainPercentage);
        Assert.Equal(0, Assert.Single(portfolio.Stocks!).GainPercentage);
    }

    // ---- ownership --------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_only_the_portfolios_of_the_user()
    {
        _portfolios.Add(1, Owner);
        _portfolios.Add(2, Intruder);
        _portfolios.Add(3, Owner);

        var mine = await Service().GetAllAsync(Owner);

        Assert.Equal(new[] { 1, 3 }, mine.Select(portfolio => portfolio.Id));
    }

    [Fact]
    public async Task Get_of_the_portfolio_of_another_user_is_a_404()
    {
        _portfolios.Add(1, Owner);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(Intruder, 1));
    }

    [Fact]
    public async Task Get_of_a_portfolio_that_does_not_exist_is_the_same_404()
    {
        var other = await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(Owner, 404));

        _portfolios.Add(1, Owner);
        var foreign = await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(Intruder, 1));

        Assert.Equal(other.Message, foreign.Message); // nothing tells the two cases apart
    }

    [Fact]
    public async Task Delete_removes_the_portfolio_of_the_user()
    {
        _portfolios.Add(1, Owner);

        await Service().DeleteAsync(Owner, 1);

        Assert.Empty(_portfolios.Portfolios);
    }

    [Fact]
    public async Task Delete_of_the_portfolio_of_another_user_is_a_404_and_removes_nothing()
    {
        _portfolios.Add(1, Owner);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().DeleteAsync(Intruder, 1));

        Assert.Single(_portfolios.Portfolios);
    }

    [Fact]
    public async Task Create_makes_the_signed_in_user_the_owner_and_trims_the_name()
    {
        var created = await Service().CreatePortfolioAsync(Owner, new CreatePortfolioDTO { Name = "  Дивідендний  " });

        var stored = Assert.Single(_portfolios.Portfolios);
        Assert.Equal(Owner, stored.UserId);
        Assert.Equal("Дивідендний", stored.Name);
        Assert.Equal("Дивідендний", created.Name);
        Assert.Equal(0, created.TotalCurrentPrice);
    }
}
