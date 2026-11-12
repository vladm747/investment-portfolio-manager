using Microsoft.Extensions.Logging.Abstractions;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Services;
using PortfolioManager.BLL.Valuation;
using PortfolioManager.Common;
using PortfolioManager.Common.DTO;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.Tests.Services;

/// <summary>StockService over fake repositories and a fake math module: no database, no network.</summary>
public class StockServiceTests
{
    private const string Owner = "user-1";
    private const string Intruder = "user-2";

    private readonly FakeMathModule _mathModule = new();
    private readonly FakePortfolioRepository _portfolios = new();
    private readonly FakeStockRepository _stocks;

    public StockServiceTests()
    {
        _stocks = new FakeStockRepository(_portfolios);
    }

    private StockService Service() =>
        new(_stocks, _portfolios, _mathModule, NullLogger<StockService>.Instance);

    private static readonly DateTime EntryDate = new(2024, 5, 6);

    private static CreateStockDTO Request(int quantity, string symbol, decimal entryPrice) =>
        new() { Quantity = quantity, Symbol = symbol, EntryPrice = entryPrice, EntryDate = EntryDate };

    // ---- CreateAsync ------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_takes_name_currency_sector_and_price_from_the_math_module()
    {
        _portfolios.Add(7, Owner);
        _mathModule.Stock = _ => new StockResponse("AAPL", "Apple Inc.", "USD", "Technology", 200);

        var created = await Service().CreateAsync(Owner, 7, Request(3, "aapl", 150));

        Assert.Equal(new[] { "aapl" }, _mathModule.StockCalls); // the math module normalises the ticker
        Assert.Equal("AAPL", created.Symbol);
        Assert.Equal("Apple Inc.", created.Name);
        Assert.Equal("USD", created.Currency);
        Assert.Equal(SectorEnum.Technology, created.Sector);
        Assert.Equal(200, created.CurrentPrice);
        Assert.Equal(600, created.TotalCurrentPrice);
        Assert.Equal(150, created.Gain); // 3 * 200 - 3 * 150
        Assert.Equal(33.3333m, created.GainPercentage);
        Assert.Equal(7, Assert.Single(_stocks.Stocks).PortfolioId);
    }

    [Fact]
    public async Task Create_stores_only_what_was_typed_and_fetched_rounded_to_what_the_column_keeps()
    {
        _portfolios.Add(7, Owner);
        // A price as the Python service sends it (a binary float), and one typed with more decimals than the column has.
        _mathModule.Stock = _ => new StockResponse("AAPL", "Apple Inc.", "USD", "Technology", 189.84000396728516);

        var created = await Service().CreateAsync(Owner, 7, Request(3, "AAPL", 150.12345m));

        var stored = Assert.Single(_stocks.Stocks);
        Assert.Equal(189.84m, stored.CurrentPrice);
        Assert.Equal(150.1235m, stored.EntryPrice); // what SQL Server would store, so a later read gives the same number
        Assert.Equal(stored.EntryPrice, created.EntryPrice);
        Assert.Equal(stored.CurrentPrice, created.CurrentPrice);
    }

    [Fact]
    public async Task Create_maps_a_sector_with_a_space_in_its_name()
    {
        _portfolios.Add(1, Owner);
        _mathModule.Stock = _ => new StockResponse("JPM", "JPMorgan Chase", "USD", "Financial Services", 100);

        var created = await Service().CreateAsync(Owner, 1, Request(1, "JPM", 90));

        Assert.Equal(SectorEnum.FinancialServices, created.Sector);
    }

    [Fact]
    public async Task Create_stores_an_ETF_without_a_sector_as_Unknown()
    {
        _portfolios.Add(1, Owner);
        _mathModule.Stock = _ => new StockResponse("SPY", "SPDR S&P 500 ETF", "USD", null, 500);

        var created = await Service().CreateAsync(Owner, 1, Request(2, "SPY", 480));

        Assert.Equal(SectorEnum.Unknown, created.Sector);
        Assert.Single(_stocks.Stocks);
    }

    [Fact]
    public async Task Create_falls_back_to_the_ticker_when_the_name_and_currency_are_missing()
    {
        _portfolios.Add(1, Owner);
        _mathModule.Stock = _ => new StockResponse("XYZ", null, null, null, 10);

        var created = await Service().CreateAsync(Owner, 1, Request(1, "XYZ", 9));

        Assert.Equal("XYZ", created.Name);
        Assert.Equal(string.Empty, created.Currency);
    }

    [Fact]
    public async Task Create_for_an_unknown_ticker_stores_nothing_and_lets_the_typed_exception_through()
    {
        _portfolios.Add(1, Owner);
        _mathModule.Stock = _ => throw new SymbolNotFoundException();

        await Assert.ThrowsAsync<SymbolNotFoundException>(() => Service().CreateAsync(Owner, 1, Request(1, "XXXX", 9)));

        Assert.Empty(_stocks.Stocks);
    }

    [Fact]
    public async Task Create_when_Yahoo_is_unreachable_stores_nothing()
    {
        _portfolios.Add(1, Owner);
        _mathModule.Stock = _ => throw new MarketDataUnavailableException();

        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => Service().CreateAsync(Owner, 1, Request(1, "AAPL", 9)));

        Assert.Empty(_stocks.Stocks);
    }

    [Fact]
    public async Task Create_without_a_current_price_is_refused_instead_of_storing_a_zero_price()
    {
        _portfolios.Add(1, Owner);
        _mathModule.Stock = _ => new StockResponse("FUND", "Some fund", "USD", null, null);

        var error = await Assert.ThrowsAsync<MarketDataUnavailableException>(() => Service().CreateAsync(Owner, 1, Request(1, "FUND", 9)));

        Assert.Contains("FUND", error.Message);
        Assert.Empty(_stocks.Stocks);
    }

    [Fact]
    public async Task Create_in_the_portfolio_of_another_user_is_a_404_and_does_not_even_ask_the_market()
    {
        _portfolios.Add(7, Owner);
        _mathModule.Stock = _ => new StockResponse("AAPL", "Apple Inc.", "USD", "Technology", 200);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().CreateAsync(Intruder, 7, Request(1, "AAPL", 150)));

        Assert.Empty(_stocks.Stocks);
        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task Create_in_a_portfolio_that_does_not_exist_is_a_404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service().CreateAsync(Owner, 99, Request(1, "AAPL", 150)));

        Assert.Empty(_stocks.Stocks);
    }

    // ---- GetAllAsync ------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_returns_the_positions_of_a_portfolio_of_the_user()
    {
        _portfolios.Add(5, Owner);
        _stocks.Add(5, "AAPL", 2, 100);
        _stocks.Add(6, "MSFT", 1, 300); // another portfolio

        var stocks = await Service().GetAllAsync(Owner, 5);

        Assert.Equal(new[] { "AAPL" }, stocks.Select(stock => stock.Symbol));
    }

    [Fact]
    public async Task GetAll_of_the_portfolio_of_another_user_is_a_404()
    {
        _portfolios.Add(5, Owner);
        _stocks.Add(5, "AAPL", 2, 100);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAllAsync(Intruder, 5));
    }

    [Fact]
    public async Task GetAll_shows_value_gain_and_percentage_worked_out_from_quantity_and_prices()
    {
        _portfolios.Add(5, Owner);
        _stocks.Add(5, "AAPL", 2, 100, currentPrice: 150);

        var shown = Assert.Single(await Service().GetAllAsync(Owner, 5));

        Assert.Equal(300, shown.TotalCurrentPrice);
        Assert.Equal(100, shown.Gain);
        Assert.Equal(50, shown.GainPercentage);
    }

    // ---- UpdateAsync ------------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_recalculates_value_gain_and_percentage_for_the_new_quantity()
    {
        _portfolios.Add(5, Owner);
        var stock = _stocks.Add(5, "AAPL", quantity: 2, entryPrice: 100, currentPrice: 150);

        var updated = await Service().UpdateAsync(Owner, stock.Id, 4);

        Assert.Equal(4, updated.Quantity);
        Assert.Equal(600, updated.TotalCurrentPrice);
        Assert.Equal(200, updated.Gain);
        Assert.Equal(50, updated.GainPercentage);
        Assert.Same(stock, Assert.Single(_stocks.Updated));
        Assert.Equal(4, stock.Quantity); // the stored position changed; its value and gain are not stored at all
    }

    [Fact]
    public async Task Update_of_the_position_of_another_user_is_a_404_and_changes_nothing()
    {
        _portfolios.Add(5, Owner);
        var stock = _stocks.Add(5, "AAPL", 2, 100);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().UpdateAsync(Intruder, stock.Id, 50));

        Assert.Equal(2, stock.Quantity);
        Assert.Empty(_stocks.Updated);
    }

    [Fact]
    public async Task Update_of_a_position_that_does_not_exist_is_a_404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service().UpdateAsync(Owner, 404, 5));
    }

    // ---- UpdatePricesAsync ------------------------------------------------------------------------------------

    private void HoldStocks(params (string Symbol, int Quantity, decimal EntryPrice)[] holdings)
    {
        _portfolios.Add(5, Owner);
        foreach (var (symbol, quantity, entryPrice) in holdings)
            _stocks.Add(5, symbol, quantity, entryPrice);
    }

    [Fact]
    public async Task UpdatePrices_asks_for_all_prices_in_one_call_and_saves_every_stock_in_one_go()
    {
        HoldStocks(("AAPL", 2, 100), ("msft", 1, 300), ("AAPL", 1, 120)); // lower case: older rows were stored as typed
        _mathModule.Prices = _ => new PricesResponse(new Dictionary<string, double> { ["AAPL"] = 200, ["MSFT"] = 330 }, Array.Empty<string>());

        await Service().UpdatePricesAsync(Owner, 5);

        Assert.Equal(new[] { "AAPL", "msft" }, Assert.Single(_mathModule.PriceCalls));
        var saved = Assert.Single(_stocks.UpdatedInRange); // one SaveChanges for the whole portfolio, not one per stock
        Assert.Equal(3, saved.Count);
        Assert.Empty(_stocks.Updated);
        var microsoft = saved.Single(stock => stock.Symbol == "msft");
        Assert.Equal(330, microsoft.CurrentPrice);
        Assert.Equal(new PositionValuation(300, 330, 30, 10), microsoft.Valuation()); // worked out, never stored
        Assert.Equal(new[] { 200m, 330m, 200m }, saved.Select(stock => stock.CurrentPrice));
    }

    [Fact]
    public async Task UpdatePrices_writes_nothing_when_one_price_is_missing()
    {
        HoldStocks(("AAPL", 2, 100), ("DEAD", 1, 300));
        _mathModule.Prices = _ => new PricesResponse(new Dictionary<string, double> { ["AAPL"] = 200 }, new[] { "DEAD" });

        var error = await Assert.ThrowsAsync<MarketDataUnavailableException>(() => Service().UpdatePricesAsync(Owner, 5));

        Assert.Contains("DEAD", error.Message);
        Assert.Empty(_stocks.UpdatedInRange);
        Assert.Empty(_stocks.Updated);
        Assert.All(_stocks.Stocks, stock => Assert.Equal(1, stock.CurrentPrice));
    }

    [Fact]
    public async Task UpdatePrices_of_an_empty_portfolio_does_not_call_the_math_module()
    {
        _portfolios.Add(5, Owner);

        await Service().UpdatePricesAsync(Owner, 5);

        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task UpdatePrices_of_the_portfolio_of_another_user_is_a_404_and_does_not_ask_the_market()
    {
        HoldStocks(("AAPL", 2, 100));

        await Assert.ThrowsAsync<NotFoundException>(() => Service().UpdatePricesAsync(Intruder, 5));

        Assert.Equal(0, _mathModule.Calls);
        Assert.Empty(_stocks.UpdatedInRange);
    }

    // ---- DeleteAsync ------------------------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_the_position_of_the_user()
    {
        _portfolios.Add(5, Owner);
        var stock = _stocks.Add(5, "AAPL", 2, 100);

        var deleted = await Service().DeleteAsync(Owner, stock.Id);

        Assert.True(deleted);
        Assert.Empty(_stocks.Stocks);
    }

    [Fact]
    public async Task Delete_of_the_position_of_another_user_is_a_404_and_removes_nothing()
    {
        _portfolios.Add(5, Owner);
        var stock = _stocks.Add(5, "AAPL", 2, 100);

        await Assert.ThrowsAsync<NotFoundException>(() => Service().DeleteAsync(Intruder, stock.Id));

        Assert.Single(_stocks.Stocks);
    }
}
