using Microsoft.Extensions.Logging.Abstractions;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Services;
using PortfolioManager.DAL.Entities;
using PortfolioManager.MathModule.Client;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.Tests.Services;

/// <summary>PortfolioStatisticService with a fake math module: what it asks for and how it shapes the answer.</summary>
public class PortfolioStatisticServiceTests
{
    private const string Owner = "user-1";
    private const string Intruder = "user-2";

    private readonly FakeMathModule _mathModule = new();
    private readonly FakePortfolioRepository _portfolios = new();

    private PortfolioStatisticService Service() =>
        new(_portfolios, _mathModule, NullLogger<PortfolioStatisticService>.Instance);

    private static MetricSet Metrics(double factor, double? sortino = 1) =>
        new(CumulativeReturn: 1 * factor, Cagr: 2 * factor, Sharpe: 3 * factor, Sortino: sortino, Calmar: 5 * factor,
            MaxDrawdown: -6 * factor, Volatility: 7 * factor, ExpectedMonthly: 8 * factor, ExpectedYearly: 9 * factor);

    private static OptimisationResponse Optimisation(
        MetricSet baseMetrics, MetricSet optimisedMetrics,
        Dictionary<string, double> baseWeights, Dictionary<string, double> optimisedWeights) =>
        new(new Period(new DateOnly(2020, 1, 2), new DateOnly(2024, 5, 6), 1090), 0,
            new WeightsPair(baseWeights, optimisedWeights), new MetricsPair(baseMetrics, optimisedMetrics));

    private static Stock Position(string symbol, int quantity, decimal currentPrice, DateTime? entryDate = null) =>
        new() { Symbol = symbol, Quantity = quantity, EntryPrice = 1, CurrentPrice = currentPrice, EntryDate = entryDate ?? new DateTime(2024, 5, 6) };

    private void AnswerWithAnyOptimisation() =>
        _mathModule.Optimisation = request => Optimisation(Metrics(1), Metrics(2),
            request.Symbols.Zip(request.Weights).ToDictionary(pair => pair.First, pair => pair.Second),
            request.Symbols.ToDictionary(symbol => symbol, _ => 1.0 / request.Symbols.Count));

    // ---- GetMetricsAsync --------------------------------------------------------------------------------------

    [Fact]
    public async Task GetMetrics_builds_the_weights_from_what_the_positions_are_worth_now()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 3, 200), Position("MSFT", 1, 400)); // 600 and 400
        AnswerWithAnyOptimisation();

        await Service().GetMetricsAsync(Owner, 1);

        var request = Assert.Single(_mathModule.OptimisationCalls);
        Assert.Equal(new[] { "AAPL", "MSFT" }, request.Symbols);
        Assert.Equal(new[] { 0.6, 0.4 }, request.Weights);
    }

    [Fact]
    public async Task GetMetrics_follows_a_change_to_a_position_because_nothing_is_stored()
    {
        // Before, the weights came from a stored copy that nothing refreshed when the quantity changed.
        var aapl = Position("AAPL", 1, 100);
        _portfolios.Add(1, Owner, aapl, Position("MSFT", 1, 100));
        AnswerWithAnyOptimisation();

        await Service().GetMetricsAsync(Owner, 1);
        aapl.Quantity = 3; // the user buys more
        await Service().GetMetricsAsync(Owner, 1);
        aapl.CurrentPrice = 50; // and the price falls

        await Service().GetMetricsAsync(Owner, 1);

        Assert.Equal(new[] { 0.5, 0.5 }, _mathModule.OptimisationCalls[0].Weights);
        Assert.Equal(new[] { 0.75, 0.25 }, _mathModule.OptimisationCalls[1].Weights);
        Assert.Equal(new[] { 0.6, 0.4 }, _mathModule.OptimisationCalls[2].Weights);
    }

    [Fact]
    public async Task GetMetrics_adds_up_a_ticker_held_in_several_positions_and_leaves_out_empty_ones()
    {
        _portfolios.Add(1, Owner,
            Position("AAPL", 1, 100), Position("msft", 1, 100), Position("aapl", 2, 100), Position("OLD", 0, 100));
        AnswerWithAnyOptimisation();

        await Service().GetMetricsAsync(Owner, 1);

        var request = Assert.Single(_mathModule.OptimisationCalls);
        Assert.Equal(new[] { "AAPL", "MSFT" }, request.Symbols);
        Assert.Equal(new[] { 0.75, 0.25 }, request.Weights);
    }

    [Fact]
    public async Task GetMetrics_of_a_portfolio_with_nothing_to_optimise_is_a_400_and_calls_nothing()
    {
        _portfolios.Add(1, Owner, Position("OLD", 0, 100));

        await Assert.ThrowsAsync<ValidationFailedException>(() => Service().GetMetricsAsync(Owner, 1));

        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task GetMetrics_of_the_portfolio_of_another_user_is_a_404_and_calls_nothing()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetMetricsAsync(Intruder, 1));

        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task GetMetrics_compares_the_nine_metrics_current_against_optimised()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        _mathModule.Optimisation = _ => Optimisation(Metrics(1), Metrics(2),
            new() { ["AAPL"] = 1 }, new() { ["AAPL"] = 1 });

        var result = await Service().GetMetricsAsync(Owner, 1);

        Assert.Equal(
            new[]
            {
                "Загальний дохід", "Середньорічний темп приросту (CAGR﹪)", "Коефіцієнт Шарпа", "Коефіцієнт Сортіно",
                "Коефіцієнт Кальмара", "Максимальна просадка", "Показник волатильності (річний)",
                "Прогнозований місячний дохід", "Прогнозований річний дохід"
            },
            result.Comparisons.Select(c => c.MetricName));
        var cagr = result.Comparisons[1];
        Assert.Equal(2, cagr.StrategyBaseValue);
        Assert.Equal(4, cagr.StrategyOptimizedValue);
        var volatility = result.Comparisons[6];
        Assert.Equal(7, volatility.StrategyBaseValue);
        Assert.Equal(14, volatility.StrategyOptimizedValue);
    }

    [Fact]
    public async Task GetMetrics_shows_an_undefined_metric_as_zero()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        _mathModule.Optimisation = _ => Optimisation(Metrics(1, sortino: null), Metrics(2),
            new() { ["AAPL"] = 1 }, new() { ["AAPL"] = 1 });

        var result = await Service().GetMetricsAsync(Owner, 1);

        var sortino = result.Comparisons.Single(c => c.MetricKey == "sortino");
        Assert.Equal(0, sortino.StrategyBaseValue);
        Assert.Equal(1, sortino.StrategyOptimizedValue);
    }

    [Fact]
    public async Task GetMetrics_lists_the_current_weights_in_the_order_of_the_optimised_ones()
    {
        // The client pairs the two lists by position, so the order of the optimised weights decides.
        _portfolios.Add(1, Owner, Position("AAPL", 5, 100), Position("MSFT", 3, 100), Position("TSLA", 2, 100));
        _mathModule.Optimisation = _ => Optimisation(Metrics(1), Metrics(2),
            new() { ["AAPL"] = 0.5, ["MSFT"] = 0.3, ["TSLA"] = 0.2 },
            new() { ["TSLA"] = 0.6, ["AAPL"] = 0.4, ["MSFT"] = 0.0 });

        var weights = (await Service().GetMetricsAsync(Owner, 1)).Weights;

        Assert.Equal(new[] { "TSLA", "AAPL", "MSFT" }, weights.OptimizedWeights.Keys);
        Assert.Equal(new[] { 0.2, 0.5, 0.3 }, weights.BaseWeights);
        Assert.Equal(0.6, weights.OptimizedWeights["TSLA"]);
    }

    [Fact]
    public async Task GetMetrics_lets_the_typed_exceptions_through_unwrapped()
    {
        // The old code caught everything and rethrew a plain Exception, which the filter could only turn into a 500.
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        _mathModule.Optimisation = _ => throw new MarketDataUnavailableException();

        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => Service().GetMetricsAsync(Owner, 1));

        _mathModule.Optimisation = _ => throw new MathModuleRejectedException("not_enough_history", "too short");

        await Assert.ThrowsAsync<MathModuleRejectedException>(() => Service().GetMetricsAsync(Owner, 1));
    }

    // ---- GetGrowthAsync ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Growth_sends_every_position_with_its_date_and_maps_the_points_to_midnight_dates()
    {
        _portfolios.Add(1, Owner,
            Position("AAPL", 3, 100, new DateTime(2024, 5, 6, 14, 30, 0)), // time of day is dropped
            Position("MSFT", 1, 100, new DateTime(2024, 6, 1)));
        _mathModule.Growth = _ => new GrowthResponse(new[]
        {
            new GrowthPoint(new DateOnly(2024, 5, 6), 1000.5), new GrowthPoint(new DateOnly(2024, 5, 7), 1010)
        });

        var result = await Service().GetGrowthAsync(Owner, 1);

        var request = Assert.Single(_mathModule.GrowthCalls);
        Assert.Equal(
            new[] { new PositionRequest("AAPL", 3, new DateOnly(2024, 5, 6)), new PositionRequest("MSFT", 1, new DateOnly(2024, 6, 1)) },
            request.Positions);
        Assert.Equal(1000.5m, result.GrowthData[new DateTime(2024, 5, 6)]);
        Assert.Equal(1010m, result.GrowthData[new DateTime(2024, 5, 7)]);
    }

    [Fact]
    public async Task Growth_leaves_out_positions_of_zero_shares_because_the_math_module_refuses_them()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 3, 100), Position("OLD", 0, 100, new DateTime(2023, 1, 1)));
        _mathModule.Growth = _ => new GrowthResponse(Array.Empty<GrowthPoint>());

        await Service().GetGrowthAsync(Owner, 1);

        Assert.Equal(new[] { "AAPL" }, Assert.Single(_mathModule.GrowthCalls).Positions.Select(p => p.Symbol));
    }

    [Fact]
    public async Task Growth_of_a_portfolio_without_positions_is_empty_and_calls_nothing()
    {
        _portfolios.Add(1, Owner);

        var result = await Service().GetGrowthAsync(Owner, 1);

        Assert.Empty(result.GrowthData);
        Assert.Empty(_mathModule.GrowthCalls);
    }

    [Fact]
    public async Task Growth_of_the_portfolio_of_another_user_is_a_404_and_calls_nothing()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetGrowthAsync(Intruder, 1));

        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task Growth_lets_the_typed_exceptions_through_unwrapped()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        _mathModule.Growth = _ => throw new MarketDataUnavailableException();

        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => Service().GetGrowthAsync(Owner, 1));
    }
}
