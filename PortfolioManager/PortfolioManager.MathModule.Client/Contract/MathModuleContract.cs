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

/// <summary>The body of every error response: {"code": "symbol_not_found", "message": "..."}.</summary>
internal sealed record ErrorResponse(string Code, string Message);
