using PortfolioManager.BLL.Models;
using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.BLL.Services;

public static class PortfolioComparer
{
    private sealed record Metric(string Key, string Name, Func<MetricSet, double?> Value, bool LowerIsBetter = false);

    /// <summary>The nine metrics in the order the comparison table shows them.</summary>
    private static readonly Metric[] Metrics =
    [
        new("cumulativeReturn", "Загальний дохід", set => set.CumulativeReturn),
        new("cagr", "Середньорічний темп приросту (CAGR﹪)", set => set.Cagr),
        new("sharpe", "Коефіцієнт Шарпа", set => set.Sharpe),
        new("sortino", "Коефіцієнт Сортіно", set => set.Sortino),
        new("calmar", "Коефіцієнт Кальмара", set => set.Calmar),
        new("maxDrawdown", "Максимальна просадка", set => set.MaxDrawdown, LowerIsBetter: true),
        new("volatility", "Показник волатильності (річний)", set => set.Volatility, LowerIsBetter: true),
        new("expectedMonthly", "Прогнозований місячний дохід", set => set.ExpectedMonthly),
        new("expectedYearly", "Прогнозований річний дохід", set => set.ExpectedYearly),
    ];

    public static ComparisonResult Compare(MetricSet strategyBase, MetricSet strategyOptimized) =>
        new()
        {
            Comparisons = Metrics.Select(metric => new ComparisonViewModel
            {
                MetricKey = metric.Key,
                MetricName = metric.Name,
                LowerIsBetter = metric.LowerIsBetter,
                // An undefined metric is shown as 0, as before.
                StrategyBaseValue = metric.Value(strategyBase) ?? 0,
                StrategyOptimizedValue = metric.Value(strategyOptimized) ?? 0,
            }).ToList()
        };
}
