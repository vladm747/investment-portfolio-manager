using Microsoft.Extensions.Logging.Abstractions;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Models;
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

    [Fact]
    public async Task GetMetrics_adds_the_forecast_risk_of_the_current_and_the_optimised_portfolio()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 3, 200), Position("MSFT", 1, 400));
        _mathModule.Optimisation = _ => Optimisation(Metrics(1), Metrics(2),
            new() { ["AAPL"] = 0.6, ["MSFT"] = 0.4 },
            new() { ["MSFT"] = 1.0, ["AAPL"] = 0.0 }); // the optimiser dropped Apple
        _mathModule.Forecast = request => SampleForecast.For(request, portfolioVolatility: request.Symbols.Count / 10.0);

        var result = await Service().GetMetricsAsync(Owner, 1);

        Assert.Collection(_mathModule.ForecastCalls,
            current =>
            {
                Assert.Equal(new[] { "AAPL", "MSFT" }, current.Symbols);
                Assert.Equal(new[] { 0.6, 0.4 }, current.Weights);
                Assert.False(current.IncludeAssets); // the page compares the portfolios only
            },
            optimised =>
            {
                Assert.Equal(new[] { "MSFT" }, optimised.Symbols); // a stock at 0 is not part of the portfolio
                Assert.Equal(new[] { 1.0 }, optimised.Weights);
                Assert.False(optimised.IncludeAssets);
            });
        var risk = result.RiskForecast!;
        Assert.Equal("risk-v1", risk.Model.Version);
        Assert.Equal(SampleForecast.AsOf, risk.AsOf);
        Assert.Equal(0.2, risk.Base.Volatility.Value);
        Assert.Equal(0.1, risk.Optimised.Volatility.Value);
        Assert.Equal(new RiskMetric(0.025, 0.025 / 2, 0.025 * 2, 0.025 * 0.75), risk.Base.ValueAtRisk95);
    }

    [Theory]
    [InlineData("unavailable")]
    [InlineData("not_enough_history")]
    [InlineData("symbol_not_found")]
    public async Task GetMetrics_without_a_forecast_is_still_the_optimisation(string failure)
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        AnswerWithAnyOptimisation();
        Exception error = failure switch
        {
            "unavailable" => new MarketDataUnavailableException(),
            "not_enough_history" => new MathModuleRejectedException(failure, "too short"),
            _ => new SymbolNotFoundException()
        };
        _mathModule.Forecast = _ => throw error;

        var result = await Service().GetMetricsAsync(Owner, 1);

        Assert.Null(result.RiskForecast);
        Assert.Equal(9, result.Comparisons.Count);
    }

    [Fact]
    public async Task GetMetrics_does_not_hide_a_cancelled_request_behind_a_missing_forecast()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        AnswerWithAnyOptimisation();
        _mathModule.Forecast = _ => throw new OperationCanceledException();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service().GetMetricsAsync(Owner, 1));
    }

    // ---- GetForecastAsync -------------------------------------------------------------------------------------

    [Fact]
    public async Task GetForecast_weights_the_portfolio_like_the_optimisation_and_asks_for_every_stock()
    {
        _portfolios.Add(1, Owner,
            Position("AAPL", 1, 200), Position("msft", 1, 400), Position("aapl", 2, 200), Position("OLD", 0, 100));
        _mathModule.Forecast = request => SampleForecast.For(request);

        await Service().GetForecastAsync(Owner, 1);

        var request = Assert.Single(_mathModule.ForecastCalls);
        Assert.Equal(new[] { "AAPL", "MSFT" }, request.Symbols);
        Assert.Equal(new[] { 0.6, 0.4 }, request.Weights);
        Assert.True(request.IncludeAssets);
    }

    [Fact]
    public async Task GetForecast_returns_the_models_figures_and_the_losses_in_money()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 3, 200), Position("MSFT", 1, 400)); // worth 1000
        _mathModule.Forecast = request => SampleForecast.For(request);

        var forecast = await Service().GetForecastAsync(Owner, 1);

        Assert.Equal(SampleForecast.AsOf, forecast.AsOf);
        Assert.Equal(899, forecast.TradingDays);
        Assert.Equal(new RiskModelInfo("risk-v1", new DateOnly(2026, 11, 13), 63, 0.8, 2, 60, false), forecast.Model);
        Assert.Equal(1000m, forecast.PortfolioValue);
        Assert.Equal(new RiskMetric(0.2, 0.2 / 2, 0.2 * 2, 0.2 * 0.75), forecast.Portfolio.Volatility);
        Assert.Equal(new MoneyRange(25m, 12.5m, 50m), forecast.Losses.ValueAtRisk95);
        Assert.Equal(new MoneyRange(125m, 62.5m, 250m), forecast.Losses.MaxDrawdown);
        Assert.Equal(new[] { ("AAPL", 0.6), ("MSFT", 0.4) }, forecast.Assets.Select(asset => (asset.Symbol, asset.Weight)));
        Assert.Equal(0.3, forecast.Assets[0].Forecast!.Volatility.Value);
    }

    [Fact]
    public async Task GetForecast_rounds_the_losses_like_money_and_leaves_an_undefined_bound_empty()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 3, 333.33m)); // worth 999.99
        _mathModule.Forecast = request => SampleForecast.For(request) with
        {
            Portfolio = SampleForecast.Risk() with { ValueAtRisk95 = new MetricForecast(0.0123456789, null, 0.02, null) }
        };

        var forecast = await Service().GetForecastAsync(Owner, 1);

        Assert.Equal(new MoneyRange(12.3456m, null, 19.9998m), forecast.Losses.ValueAtRisk95); // 12.345555...
    }

    [Fact]
    public async Task GetForecast_passes_on_stocks_without_a_forecast_unusual_stocks_and_the_warnings()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100), Position("GME", 1, 100));
        _mathModule.Forecast = request => SampleForecast.For(request) with
        {
            Model = SampleForecast.Model with { AgeDays = 75, Stale = true },
            Assets = new[]
            {
                new AssetForecast("AAPL", 0.5, null),
                new AssetForecast("GME", 0.5, SampleForecast.Risk() with { OutOfDomain = true, OutOfDomainFeatures = new[] { "vol_252" } })
            },
            Warnings = new[] { "model_stale", "assets_out_of_domain", "assets_without_forecast" }
        };

        var forecast = await Service().GetForecastAsync(Owner, 1);

        Assert.True(forecast.Model.Stale);
        Assert.Null(forecast.Assets[0].Forecast);
        Assert.True(forecast.Assets[1].Forecast!.OutOfDomain);
        Assert.Equal(new[] { "vol_252" }, forecast.Assets[1].Forecast!.OutOfDomainFeatures);
        Assert.Equal(new[] { "model_stale", "assets_out_of_domain", "assets_without_forecast" }, forecast.Warnings);
    }

    [Fact]
    public async Task GetForecast_of_a_portfolio_with_nothing_in_it_is_a_400_and_calls_nothing()
    {
        _portfolios.Add(1, Owner, Position("OLD", 0, 100));

        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => Service().GetForecastAsync(Owner, 1));

        Assert.Equal("У портфелі немає позицій для прогнозу ризику.", Assert.Single(error.Errors));
        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task GetForecast_of_the_portfolio_of_another_user_is_a_404_and_calls_nothing()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetForecastAsync(Intruder, 1));

        Assert.Equal(0, _mathModule.Calls);
    }

    [Fact]
    public async Task GetForecast_names_a_recently_listed_stock_in_Ukrainian()
    {
        // The exact text of the math service (app/forecast/portfolio.py), from its own test data.
        _portfolios.Add(1, Owner, Position("AAA", 1, 100), Position("CCC", 1, 100));
        _mathModule.Forecast = _ => throw new MathModuleRejectedException("not_enough_history",
            "Not enough price history: CCC has prices only from 2026-06-29, which gives 99 trading days; the forecast needs at least 252.");

        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => Service().GetForecastAsync(Owner, 1));

        Assert.Equal(
            "Для прогнозу ризику потрібна щонайменше річна історія цін кожної акції портфеля. " +
            "CCC має ціни лише з 29.06.2026: 99 із 252 потрібних торгових днів.",
            Assert.Single(error.Errors));
    }

    [Theory]
    [InlineData("not_enough_history", "Some other text.",
        "Для прогнозу ризику потрібна щонайменше річна історія цін кожної акції портфеля.")]
    [InlineData("forecast_failed", "The model's features for the portfolio on 2026-11-13 are incomplete.",
        "Не вдалося обчислити прогноз ризику: в історії цін акцій портфеля є пропуски. Спробуйте пізніше.")]
    public async Task GetForecast_explains_what_the_user_can_act_on_in_Ukrainian(string code, string text, string expected)
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        _mathModule.Forecast = _ => throw new MathModuleRejectedException(code, text);

        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => Service().GetForecastAsync(Owner, 1));

        Assert.Equal(expected, Assert.Single(error.Errors));
    }

    [Fact]
    public async Task GetForecast_lets_the_other_failures_through_unwrapped()
    {
        _portfolios.Add(1, Owner, Position("AAPL", 1, 100));
        _mathModule.Forecast = _ => throw new MathModuleRejectedException("invalid_weights", "a bug of ours");

        var rejected = await Assert.ThrowsAsync<MathModuleRejectedException>(() => Service().GetForecastAsync(Owner, 1));
        Assert.Equal("a bug of ours", rejected.Message);

        _mathModule.Forecast = _ => throw new MarketDataUnavailableException();
        await Assert.ThrowsAsync<MarketDataUnavailableException>(() => Service().GetForecastAsync(Owner, 1));

        _mathModule.Forecast = _ => throw new SymbolNotFoundException();
        await Assert.ThrowsAsync<SymbolNotFoundException>(() => Service().GetForecastAsync(Owner, 1));
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
