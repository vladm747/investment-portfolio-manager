namespace PortfolioManager.MathModule.Client.Contract;

// The v1 contract of the Python math service (PortfolioManager.MathModule/app/schemas.py), as C# records.
// JSON is camelCase; the client's serializer options map the names, and dates travel as yyyy-MM-dd.

/// <summary>POST /v1/optimisation. Weights are in the same order as symbols and add up to 1.</summary>
public sealed record OptimisationRequest(IReadOnlyList<string> Symbols, IReadOnlyList<double> Weights);

public sealed record OptimisationResponse(
    Period Period,
    double RiskFreeRate,
    WeightsPair Weights,
    MetricsPair Metrics);

public sealed record Period(DateOnly Start, DateOnly End, int TradingDays);

/// <summary>Weight per symbol: the current portfolio and the max-Sharpe one.</summary>
public sealed record WeightsPair(
    IReadOnlyDictionary<string, double> Base,
    IReadOnlyDictionary<string, double> Optimised);

public sealed record MetricsPair(MetricSet Base, MetricSet Optimised);

/// <summary>
/// All values are fractions (0.12 = 12 %) except the ratios (Sharpe, Sortino, Calmar).
/// null when undefined, for example Sortino of a portfolio with no losing day.
/// </summary>
public sealed record MetricSet(
    double? CumulativeReturn,
    double? Cagr,
    double? Sharpe,
    double? Sortino,
    double? Calmar,
    double? MaxDrawdown,
    double? Volatility,
    double? ExpectedMonthly,
    double? ExpectedYearly);

/// <summary>POST /v1/growth. The quantity must be above zero.</summary>
public sealed record GrowthRequest(IReadOnlyList<PositionRequest> Positions);

public sealed record PositionRequest(string Symbol, double Quantity, DateOnly EntryDate);

public sealed record GrowthResponse(IReadOnlyList<GrowthPoint> Points);

public sealed record GrowthPoint(DateOnly Date, double Value);

/// <summary>POST /v1/prices: the latest price of many symbols in one call.</summary>
public sealed record PricesRequest(IReadOnlyList<string> Symbols);

/// <summary>Symbols that have no price are listed in <see cref="Missing"/>, not in <see cref="Prices"/>.</summary>
public sealed record PricesResponse(IReadOnlyDictionary<string, double> Prices, IReadOnlyList<string> Missing);

/// <summary>
/// GET /v1/stocks/{symbol}. Every field except the symbol can be null, for example an ETF has no sector
/// and a fund may have no current price. Sector is Yahoo Finance's own text, such as "Financial Services".
/// </summary>
public sealed record StockResponse(string Symbol, string? Name, string? Currency, string? Sector, double? Price);

/// <summary>
/// POST /v1/forecast: the risk of the portfolio with these weights (same order as the symbols, adding up to 1) over
/// the next <see cref="ForecastModelInfo.HorizonDays"/> trading days, and, unless <paramref name="IncludeAssets"/> is
/// false, of each of its stocks on its own.
/// </summary>
public sealed record ForecastRequest(IReadOnlyList<string> Symbols, IReadOnlyList<double> Weights, bool IncludeAssets = true);

/// <summary>
/// <paramref name="AsOf"/> is the last trading day of the prices the forecast starts from; <paramref name="TradingDays"/>
/// the daily returns the portfolio has had since its youngest stock was listed. <paramref name="Warnings"/> holds codes:
/// <c>model_stale</c>, <c>portfolio_out_of_domain</c>, <c>assets_out_of_domain</c>, <c>assets_without_forecast</c>.
/// </summary>
public sealed record ForecastResponse(
    ForecastModelInfo Model,
    DateOnly AsOf,
    int TradingDays,
    RiskForecast Portfolio,
    IReadOnlyList<AssetForecast> Assets,
    IReadOnlyList<string> Warnings);

/// <summary>
/// The trained model that made the forecast. <paramref name="AgeDays"/> counts from the last price it was trained on
/// to <c>AsOf</c>; past <paramref name="StaleAfterDays"/> the model is <paramref name="Stale"/> and should be retrained.
/// </summary>
public sealed record ForecastModelInfo(
    string Version,
    DateOnly TrainedThrough,
    int HorizonDays,
    double IntervalLevel,
    int AgeDays,
    int StaleAfterDays,
    bool Stale);

/// <summary>
/// Annualised volatility, one-day VaR 95 % and maximum drawdown over the horizon, as fractions (0.2 = 20 %).
/// <paramref name="OutOfDomain"/>: some feature (named in <paramref name="OutOfDomainFeatures"/>) lies outside the range
/// the model was trained on, or the quotes are <paramref name="StaleQuotes"/> (many days without a price change), so
/// the forecast is less reliable.
/// </summary>
public sealed record RiskForecast(
    MetricForecast Volatility,
    MetricForecast ValueAtRisk95,
    MetricForecast MaxDrawdown,
    bool OutOfDomain,
    IReadOnlyList<string> OutOfDomainFeatures,
    bool StaleQuotes);

/// <summary>
/// The forecast, its interval (<see cref="ForecastModelInfo.IntervalLevel"/>, 80 %) and the same metric over the last
/// horizon of history, the "next months will be like the last ones" estimate. null where undefined.
/// </summary>
public sealed record MetricForecast(double? Value, double? Low, double? High, double? Previous);

/// <summary>A stock of the portfolio. <paramref name="Forecast"/> is null when its own price history is too short or has gaps.</summary>
public sealed record AssetForecast(string Symbol, double Weight, RiskForecast? Forecast);

/// <summary>The body of every error response: {"code": "symbol_not_found", "message": "..."}.</summary>
internal sealed record ErrorResponse(string Code, string Message);
