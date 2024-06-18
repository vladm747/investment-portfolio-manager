namespace PortfolioManager.BLL.Models;

public class PortfolioMetrics
{
    public Metrics Benchmark { get; set; }
    public Metrics Strategy { get; set; }
}
/// <summary>
/// Represents the performance metrics.
/// </summary>
public class Metrics
{
    /// <summary>
    /// The start period of the performance evaluation.
    /// </summary>
    public string StartPeriod { get; set; }

    /// <summary>
    /// The end period of the performance evaluation.
    /// </summary>
    public string EndPeriod { get; set; }

    /// <summary>
    /// The risk-free rate used in the performance evaluation.
    /// </summary>
    public double RiskFreeRate { get; set; }

    /// <summary>
    /// The proportion of time the investment was in the market.
    /// </summary>
    public double TimeInMarket { get; set; }

    /// <summary>
    /// The cumulative return over the period.
    /// </summary>
    public double CumulativeReturn { get; set; }

    /// <summary>
    /// The compound annual growth rate.
    /// </summary>
    public double CAGR { get; set; }

    /// <summary>
    /// The Sharpe ratio.
    /// </summary>
    public double Sharpe { get; set; }

    /// <summary>
    /// The probability of achieving the Sharpe ratio.
    /// </summary>
    public double ProbSharpeRatio { get; set; }

    /// <summary>
    /// The adjusted Sharpe ratio.
    /// </summary>
    public double SmartSharpe { get; set; }

    /// <summary>
    /// The Sortino ratio.
    /// </summary>
    public double Sortino { get; set; }

    /// <summary>
    /// The adjusted Sortino ratio.
    /// </summary>
    public double SmartSortino { get; set; }

    /// <summary>
    /// The Sortino ratio divided by the square root of 2.
    /// </summary>
    public double SortinoSqrt2 { get; set; }

    /// <summary>
    /// The adjusted Sortino ratio divided by the square root of 2.
    /// </summary>
    public double SmartSortinoSqrt2 { get; set; }

    /// <summary>
    /// The Omega ratio.
    /// </summary>
    public double Omega { get; set; }

    /// <summary>
    /// The maximum drawdown over the period.
    /// </summary>
    public double MaxDrawdown { get; set; }

    /// <summary>
    /// The longest drawdown duration in days.
    /// </summary>
    public int LongestDDDays { get; set; }

    /// <summary>
    /// The annualized volatility.
    /// </summary>
    public double VolatilityAnn { get; set; }

    /// <summary>
    /// The R-squared value.
    /// </summary>
    public double R2 { get; set; }

    /// <summary>
    /// The information ratio.
    /// </summary>
    public double InformationRatio { get; set; }

    /// <summary>
    /// The Calmar ratio.
    /// </summary>
    public double Calmar { get; set; }

    /// <summary>
    /// The skewness of the returns distribution.
    /// </summary>
    public double Skew { get; set; }

    /// <summary>
    /// The kurtosis of the returns distribution.
    /// </summary>
    public double Kurtosis { get; set; }

    /// <summary>
    /// The expected daily return.
    /// </summary>
    public double ExpectedDaily { get; set; }

    /// <summary>
    /// The expected monthly return.
    /// </summary>
    public double ExpectedMonthly { get; set; }

    /// <summary>
    /// The expected yearly return.
    /// </summary>
    public double ExpectedYearly { get; set; }

    /// <summary>
    /// The Kelly criterion value.
    /// </summary>
    public double KellyCriterion { get; set; }

    /// <summary>
    /// The risk of ruin.
    /// </summary>
    public double RiskOfRuin { get; set; }

    /// <summary>
    /// The daily value-at-risk.
    /// </summary>
    public double DailyValueAtRisk { get; set; }

    /// <summary>
    /// The expected shortfall or conditional value-at-risk.
    /// </summary>
    public double ExpectedShortfallCVaR { get; set; }

    /// <summary>
    /// The maximum number of consecutive winning days.
    /// </summary>
    public double MaxConsecutiveWins { get; set; }

    /// <summary>
    /// The maximum number of consecutive losing days.
    /// </summary>
    public double MaxConsecutiveLosses { get; set; }

    /// <summary>
    /// The gain/pain ratio.
    /// </summary>
    public double GainPainRatio { get; set; }

    /// <summary>
    /// The one-month gain/pain ratio.
    /// </summary>
    public double GainPain1M { get; set; }

    /// <summary>
    /// The payoff ratio.
    /// </summary>
    public double PayoffRatio { get; set; }

    /// <summary>
    /// The profit factor.
    /// </summary>
    public double ProfitFactor { get; set; }

    /// <summary>
    /// The common sense ratio.
    /// </summary>
    public double CommonSenseRatio { get; set; }

    /// <summary>
    /// The CPC index.
    /// </summary>
    public double CPCIndex { get; set; }

    /// <summary>
    /// The tail ratio.
    /// </summary>
    public double TailRatio { get; set; }

    /// <summary>
    /// The outlier win ratio.
    /// </summary>
    public double OutlierWinRatio { get; set; }

    /// <summary>
    /// The outlier loss ratio.
    /// </summary>
    public double OutlierLossRatio { get; set; }

    /// <summary>
    /// Month-to-date return.
    /// </summary>
    public double MTD { get; set; }

    /// <summary>
    /// Three-month return.
    /// </summary>
    public double M3 { get; set; }

    /// <summary>
    /// Six-month return.
    /// </summary>
    public double M6 { get; set; }

    /// <summary>
    /// Year-to-date return.
    /// </summary>
    public double YTD { get; set; }

    /// <summary>
    /// One-year return.
    /// </summary>
    public double Y1 { get; set; }

    /// <summary>
    /// Three-year annualized return.
    /// </summary>
    public double Y3Ann { get; set; }

    /// <summary>
    /// Five-year annualized return.
    /// </summary>
    public double Y5Ann { get; set; }

    /// <summary>
    /// Ten-year annualized return.
    /// </summary>
    public double Y10Ann { get; set; }

    /// <summary>
    /// All-time annualized return.
    /// </summary>
    public double AllTimeAnn { get; set; }

    /// <summary>
    /// Best daily return.
    /// </summary>
    public double BestDay { get; set; }

    /// <summary>
    /// Worst daily return.
    /// </summary>
    public double WorstDay { get; set; }

    /// <summary>
    /// Best monthly return.
    /// </summary>
    public double BestMonth { get; set; }

    /// <summary>
    /// Worst monthly return.
    /// </summary>
    public double WorstMonth { get; set; }

    /// <summary>
    /// Best yearly return.
    /// </summary>
    public double BestYear { get; set; }

    /// <summary>
    /// Worst yearly return.
    /// </summary>
    public double WorstYear { get; set; }

    /// <summary>
    /// The average drawdown.
    /// </summary>
    public double AvgDrawdown { get; set; }

    /// <summary>
    /// The average number of days in a drawdown.
    /// </summary>
    public double AvgDrawdownDays { get; set; }

    /// <summary>
    /// The recovery factor.
    /// </summary>
    public double RecoveryFactor { get; set; }

    /// <summary>
    /// The ulcer index.
    /// </summary>
    public double UlcerIndex { get; set; }

    /// <summary>
    /// The serenity index.
    /// </summary>
    public double SerenityIndex { get; set; }

    /// <summary>
    /// The average return in up months.
    /// </summary>
    public double AvgUpMonth { get; set; }

    /// <summary>
    /// The average return in down months.
    /// </summary>
    public double AvgDownMonth { get; set; }

    /// <summary>
    /// The proportion of winning days.
    /// </summary>
    public double WinDays { get; set; }

    /// <summary>
    /// The proportion of winning months.
    /// </summary>
    public double WinMonth { get; set; }

    /// <summary>
    /// The proportion of winning quarters.
    /// </summary>
    public double WinQuarter { get; set; }

    /// <summary>
    /// The proportion of winning years.
    /// </summary>
    public double WinYear { get; set; }

    /// <summary>
    /// The beta coefficient.
    /// </summary>
    public string Beta { get; set; }

    /// <summary>
    /// The alpha coefficient.
    /// </summary>
    public string Alpha { get; set; }

    /// <summary>
    /// The correlation coefficient.
    /// </summary>
    public string Correlation { get; set; }

    /// <summary>
    /// The Treynor ratio.
    /// </summary>
    public string TreynorRatio { get; set; }
}
