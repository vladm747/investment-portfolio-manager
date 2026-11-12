using PortfolioManager.BLL.Mapping;
using PortfolioManager.BLL.Valuation;
using PortfolioManager.DAL.Entities;

namespace PortfolioManager.Tests.Valuation;

public class PositionValuationTests
{
    [Fact]
    public void A_gain_is_current_value_minus_entry_value_and_a_percentage_of_the_entry_value()
    {
        var valuation = PositionValuation.Of(quantity: 3, entryPrice: 150, currentPrice: 200);

        Assert.Equal(450, valuation.EntryValue);
        Assert.Equal(600, valuation.CurrentValue);
        Assert.Equal(150, valuation.Gain);
        Assert.Equal(33.3333m, valuation.GainPercentage); // 150 / 450, to four decimals
    }

    [Fact]
    public void A_loss_is_negative()
    {
        var valuation = PositionValuation.Of(10, 100, 75);

        Assert.Equal(-250, valuation.Gain);
        Assert.Equal(-25, valuation.GainPercentage);
    }

    [Theory]
    [InlineData(0, 100, 150)] // no shares left
    [InlineData(5, 0, 150)] // nothing was paid
    [InlineData(0, 0, 0)]
    public void Nothing_paid_means_a_percentage_of_zero_never_a_division_by_zero(int quantity, double entryPrice, double currentPrice)
    {
        var valuation = PositionValuation.Of(quantity, (decimal)entryPrice, (decimal)currentPrice);

        Assert.Equal(0, valuation.GainPercentage);
        Assert.Equal(quantity * (decimal)currentPrice, valuation.CurrentValue);
    }

    [Fact]
    public void Decimal_arithmetic_has_no_binary_float_noise()
    {
        // With doubles 3 * 0.1 is 0.30000000000000004, and a gain of exactly nothing would not be exactly nothing.
        var valuation = PositionValuation.Of(3, 0.1m, 0.1m);

        Assert.Equal(0.3m, valuation.EntryValue);
        Assert.Equal(0m, valuation.Gain);
        Assert.Equal(0m, valuation.GainPercentage);
    }

    [Fact]
    public void A_portfolio_is_the_sum_of_its_positions_and_its_percentage_is_worked_out_again()
    {
        var doubled = PositionValuation.Of(1, 100, 200); // +100 %
        var flat = PositionValuation.Of(1, 300, 300); // 0 %

        var total = PositionValuation.Sum(new[] { doubled, flat });

        Assert.Equal(400, total.EntryValue);
        Assert.Equal(500, total.CurrentValue);
        Assert.Equal(100, total.Gain);
        Assert.Equal(25, total.GainPercentage); // not the 50 an average of the two percentages would give
    }

    [Fact]
    public void An_empty_portfolio_is_worth_nothing_and_has_no_percentage()
    {
        var total = PositionValuation.Sum(Array.Empty<PositionValuation>());

        Assert.Equal(new PositionValuation(0, 0, 0, 0), total);
    }

    [Fact]
    public void A_portfolio_of_only_empty_positions_has_no_percentage_either()
    {
        var total = PositionValuation.Sum(new[] { PositionValuation.Of(0, 100, 150), PositionValuation.Of(0, 10, 20) });

        Assert.Equal(0, total.GainPercentage);
    }

    [Fact]
    public void A_stock_is_valued_from_its_quantity_and_prices()
    {
        var stock = new Stock { Quantity = 4, EntryPrice = 100, CurrentPrice = 150 };

        Assert.Equal(new PositionValuation(400, 600, 200, 50), stock.Valuation());
    }

    [Fact]
    public void The_copy_sent_to_the_client_carries_the_worked_out_value_gain_and_percentage()
    {
        var dto = new Stock { Id = 9, Symbol = "AAPL", Quantity = 4, EntryPrice = 100, CurrentPrice = 150, PortfolioId = 2 }.ToDto();

        Assert.Equal(600, dto.TotalCurrentPrice);
        Assert.Equal(200, dto.Gain);
        Assert.Equal(50, dto.GainPercentage);
        Assert.Equal((9, "AAPL", 2), (dto.Id, dto.Symbol, dto.PortfolioId));
    }

    [Fact]
    public void A_position_with_no_shares_is_worth_nothing_and_has_no_percentage()
    {
        var dto = new Stock { Quantity = 0, EntryPrice = 100, CurrentPrice = 150 }.ToDto();

        Assert.Equal(0, dto.TotalCurrentPrice);
        Assert.Equal(0, dto.Gain);
        Assert.Equal(0, dto.GainPercentage);
    }

    // ---- Money ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("1.23455", "1.2346")] // half away from zero, like SQL Server
    [InlineData("1.23454", "1.2345")]
    [InlineData("-1.23455", "-1.2346")]
    [InlineData("150", "150")]
    public void Money_is_rounded_to_four_decimals_the_way_the_database_rounds(string amount, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            Money.Round(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void A_market_price_loses_the_noise_of_the_binary_float_it_came_in()
    {
        Assert.Equal(189.84m, Money.FromMarketPrice(189.84000396728516));
        Assert.Equal(0.1m, Money.FromMarketPrice(0.1));
    }
}
