using PortfolioManager.BLL.Models;
using PortfolioManager.Common.DTO;

namespace PortfolioManager.BLL.Services;

public static class PortfolioComparer
    {
        public static ComparisonResult Compare(MetricsDTO strategyBase, MetricsDTO strategyOptimized)
        {
            var result = new ComparisonResult();

            AddComparison(result, "Загальний дохід", strategyBase.CumulativeReturn, strategyOptimized.CumulativeReturn);
            AddComparison(result, "Середньорічний темп приросту (CAGR﹪)", strategyBase.CAGR, strategyOptimized.CAGR);
            AddComparison(result, "Коефіцієнт Шарпа", strategyBase.Sharpe, strategyOptimized.Sharpe);
            AddComparison(result, "Коефіцієнт Сортіно", strategyBase.Sortino, strategyOptimized.Sortino);
            AddComparison(result, "Коефіцієнт Кальмара", strategyBase.Calmar, strategyOptimized.Calmar);
            AddComparison(result, "Максимальна просадка", strategyBase.MaxDrawdown, strategyOptimized.MaxDrawdown, isLowerBetter: true);
            AddComparison(result, "Показник волатильності (річний)", strategyBase.VolatilityAnn, strategyOptimized.VolatilityAnn, isLowerBetter: true);
            AddComparison(result, "Прогнозований місячний дохід", strategyBase.ExpectedMonthly, strategyOptimized.ExpectedMonthly);
            AddComparison(result, "Прогнозований річний дохід", strategyBase.ExpectedYearly, strategyOptimized.ExpectedYearly);

            return result;
        }

        private static void AddComparison(ComparisonResult result, string metricName, double strategyBaseValue, double strategyOptimizedValue, bool isLowerBetter = false)
        {
            var comparison = new ComparisonViewModel
            {
                MetricName = metricName,
                StrategyBaseValue = strategyBaseValue,
                StrategyOptimizedValue = strategyOptimizedValue,
            };

            result.Comparisons.Add(comparison);
        }
    }