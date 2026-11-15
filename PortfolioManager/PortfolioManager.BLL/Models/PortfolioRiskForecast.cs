namespace PortfolioManager.BLL.Models;

// The risk forecast of the trained model (risk-v1), as the API returns it: GET api/portfolios/{id}/forecast, and the
// riskForecast part of GET api/portfolios/{id}/optimisation. The fractions are the math service's own figures
// (MathModuleContract.cs, ForecastResponse); only the amounts in money are worked out here.

/// <summary>
/// The risk of a portfolio over the next <see cref="RiskModelInfo.HorizonDays"/> trading days (about three months),
/// starting from the prices of <see cref="AsOf"/>.
/// </summary>
/// <param name="TradingDays">Daily returns the portfolio has had since its youngest stock was listed.</param>
/// <param name="PortfolioValue">What the positions are worth now: the base of <see cref="Losses"/>.</param>
/// <param name="Assets">Each stock on its own, in the order of the portfolio, with its share of the value.</param>
/// <param name="Warnings">
/// Codes the client explains: <c>model_stale</c>, <c>portfolio_out_of_domain</c>, <c>assets_out_of_domain</c>,
/// <c>assets_without_forecast</c>.
/// </param>
public sealed record PortfolioRiskForecast(
    RiskModelInfo Model,
    DateOnly AsOf,
    int TradingDays,
    decimal PortfolioValue,
    RiskFigures Portfolio,
    PotentialLosses Losses,
    IReadOnlyList<AssetRisk> Assets,
    IReadOnlyList<string> Warnings);

/// <summary>
/// The model that made the forecast. <see cref="AgeDays"/> counts from the last price it was trained on to the forecast
/// date; past <see cref="StaleAfterDays"/> it is <see cref="Stale"/> and should be retrained.
/// </summary>
/// <param name="IntervalLevel">How often the actual value is expected inside [Low, High]: 0.8.</param>
public sealed record RiskModelInfo(
    string Version,
    DateOnly TrainedThrough,
    int HorizonDays,
    double IntervalLevel,
    int AgeDays,
    int StaleAfterDays,
    bool Stale);

/// <summary>
/// Annualised volatility, one-day value at risk at 95 % and maximum drawdown over the horizon, as fractions
/// (0.2 = 20 %). <see cref="OutOfDomain"/> marks a stock or portfolio unlike anything the model was trained on (the
/// features in <see cref="OutOfDomainFeatures"/>, or <see cref="StaleQuotes"/>): the forecast is less reliable.
/// </summary>
public sealed record RiskFigures(
    RiskMetric Volatility,
    RiskMetric ValueAtRisk95,
    RiskMetric MaxDrawdown,
    bool OutOfDomain,
    IReadOnlyList<string> OutOfDomainFeatures,
    bool StaleQuotes);

/// <summary>
/// The forecast <see cref="Value"/> with its interval [<see cref="Low"/>, <see cref="High"/>], and
/// <see cref="Previous"/>, the same metric over the last horizon of history, which the forecast is meant to beat.
/// null where undefined.
/// </summary>
public sealed record RiskMetric(double? Value, double? Low, double? High, double? Previous);

/// <param name="Forecast">null when the stock's own history is too short or has gaps; the portfolio is still forecast.</param>
public sealed record AssetRisk(string Symbol, double Weight, RiskFigures? Forecast);

/// <summary>
/// The value at risk and the maximum drawdown in money: the portfolio's current value times the fractions.
/// A one-day loss of at least <see cref="ValueAtRisk95"/> is expected on about one trading day in twenty.
/// </summary>
public sealed record PotentialLosses(MoneyRange ValueAtRisk95, MoneyRange MaxDrawdown);

public sealed record MoneyRange(decimal? Value, decimal? Low, decimal? High);

/// <summary>On the optimisation page: the risk of the current portfolio against the optimised one, from the same model.</summary>
public sealed record OptimisationRiskForecast(RiskModelInfo Model, DateOnly AsOf, RiskFigures Base, RiskFigures Optimised);
