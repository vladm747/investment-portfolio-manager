namespace PortfolioManager.BLL.Models;

public class ComparisonViewModel
{
    public string MetricName { get; set; }
    public double StrategyBaseValue { get; set; }
    public double StrategyOptimizedValue { get; set; }
}

public class ComparisonResult
{
    public List<ComparisonViewModel> Comparisons { get; set; } = new List<ComparisonViewModel>(); 
    public Weights Weights { get; set; }
}

public class Weights
{
    public double[] BaseWeights { get; set; }

    public Dictionary<string, double> OptimizedWeights { get; set; }
}