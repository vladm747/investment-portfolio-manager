namespace PortfolioManager.BLL.Models;

public class ComparisonViewModel
{
    /// <summary>Stable identifier of the metric, such as "sharpe". Unlike the name it does not depend on the language.</summary>
    public string MetricKey { get; set; } = string.Empty;

    /// <summary>The name shown to the user.</summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// A smaller figure is better (volatility, maximum drawdown). A drawdown is stored as a negative number,
    /// so there "smaller" means smaller in size: compare the absolute values.
    /// </summary>
    public bool LowerIsBetter { get; set; }

    public double StrategyBaseValue { get; set; }
    public double StrategyOptimizedValue { get; set; }
}

public class ComparisonResult
{
    public List<ComparisonViewModel> Comparisons { get; set; } = new List<ComparisonViewModel>();
    public Weights Weights { get; set; } = new();

    /// <summary>
    /// The risk the trained model expects for the current and the optimised portfolio over the next three months.
    /// null when the forecast could not be made (a stock listed less than a year ago, the service unavailable):
    /// the optimisation is shown without it.
    /// </summary>
    public OptimisationRiskForecast? RiskForecast { get; set; }
}

public class Weights
{
    public double[] BaseWeights { get; set; } = Array.Empty<double>();

    public Dictionary<string, double> OptimizedWeights { get; set; } = new();
}
