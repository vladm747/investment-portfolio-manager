using PortfolioManager.BLL.Valuation;

namespace PortfolioManager.Tests.Valuation;

public class PortfolioWeightsTests
{
    [Fact]
    public void Each_weight_is_the_share_of_the_total_value()
    {
        var (symbols, weights) = PortfolioWeights.FromValues(new[] { ("AAPL", 600.0), ("MSFT", 400.0) });

        Assert.Equal(new[] { "AAPL", "MSFT" }, symbols);
        Assert.Equal(new[] { 0.6, 0.4 }, weights);
    }

    [Fact]
    public void The_weights_add_up_to_one()
    {
        var (_, weights) = PortfolioWeights.FromValues(new[] { ("A", 1.0), ("B", 1.0), ("C", 1.0) });

        Assert.Equal(1.0, weights.Sum(), precision: 12);
    }

    [Fact]
    public void A_ticker_in_several_positions_is_added_up_whatever_its_case()
    {
        var (symbols, weights) = PortfolioWeights.FromValues(new[] { ("AAPL", 100.0), ("msft", 100.0), (" aapl ", 200.0) });

        Assert.Equal(new[] { "AAPL", "MSFT" }, symbols);
        Assert.Equal(new[] { 0.75, 0.25 }, weights);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_position_worth_nothing_or_nonsense_is_left_out(double value)
    {
        var (symbols, weights) = PortfolioWeights.FromValues(new[] { ("AAPL", 100.0), ("BAD", value) });

        Assert.Equal(new[] { "AAPL" }, symbols);
        Assert.Equal(new[] { 1.0 }, weights);
    }

    [Fact]
    public void Nothing_to_weigh_gives_an_empty_result()
    {
        Assert.Empty(PortfolioWeights.FromValues(Array.Empty<(string, double)>()).Symbols);

        var (symbols, weights) = PortfolioWeights.FromValues(new[] { ("OLD", 0.0) });

        Assert.Empty(symbols);
        Assert.Empty(weights);
    }
}
