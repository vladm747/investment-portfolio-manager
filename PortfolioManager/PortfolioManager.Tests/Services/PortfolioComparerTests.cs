using PortfolioManager.BLL.Services;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.Tests.Services;

public class PortfolioComparerTests
{
    private static MetricSet Metrics(double factor, double? sortino = 1) =>
        new(CumulativeReturn: 1 * factor, Cagr: 2 * factor, Sharpe: 3 * factor, Sortino: sortino, Calmar: 5 * factor,
            MaxDrawdown: -6 * factor, Volatility: 7 * factor, ExpectedMonthly: 8 * factor, ExpectedYearly: 9 * factor);

    [Fact]
    public void Every_metric_has_a_stable_key_in_the_order_of_the_table()
    {
        var keys = PortfolioComparer.Compare(Metrics(1), Metrics(2)).Comparisons.Select(c => c.MetricKey);

        Assert.Equal(
            new[]
            {
                "cumulativeReturn", "cagr", "sharpe", "sortino", "calmar",
                "maxDrawdown", "volatility", "expectedMonthly", "expectedYearly"
            },
            keys);
    }

    [Fact]
    public void Only_the_drawdown_and_the_volatility_are_better_when_lower()
    {
        var lowerIsBetter = PortfolioComparer.Compare(Metrics(1), Metrics(2)).Comparisons
            .Where(c => c.LowerIsBetter)
            .Select(c => c.MetricKey);

        Assert.Equal(new[] { "maxDrawdown", "volatility" }, lowerIsBetter);
    }

    [Fact]
    public void Each_metric_pairs_the_current_value_with_the_optimised_one()
    {
        var comparisons = PortfolioComparer.Compare(Metrics(1), Metrics(2)).Comparisons;

        var drawdown = comparisons.Single(c => c.MetricKey == "maxDrawdown");
        Assert.Equal("Максимальна просадка", drawdown.MetricName);
        Assert.Equal(-6, drawdown.StrategyBaseValue);
        Assert.Equal(-12, drawdown.StrategyOptimizedValue);
    }

    [Fact]
    public void An_undefined_metric_is_shown_as_zero()
    {
        var sortino = PortfolioComparer.Compare(Metrics(1, sortino: null), Metrics(2, sortino: null)).Comparisons
            .Single(c => c.MetricKey == "sortino");

        Assert.Equal(0, sortino.StrategyBaseValue);
        Assert.Equal(0, sortino.StrategyOptimizedValue);
    }
}
