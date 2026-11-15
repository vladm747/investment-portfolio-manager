using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.MathModule.Client;

/// <summary>
/// The Python math service: market data from Yahoo Finance, portfolio growth, max-Sharpe optimisation and the risk
/// forecast of the trained model.
/// Every method throws <see cref="MarketDataUnavailableException"/> when the service cannot answer,
/// <see cref="SymbolNotFoundException"/> for an unknown ticker and <see cref="MathModuleRejectedException"/>
/// when the service refuses the request; a cancelled token throws <see cref="OperationCanceledException"/>.
/// </summary>
public interface IMathModuleClient
{
    /// <summary>Name, currency, sector and price of one ticker.</summary>
    Task<StockResponse> GetStockAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Latest prices of many tickers in one call; tickers without a price come back in <c>Missing</c>.</summary>
    Task<PricesResponse> GetPricesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken = default);

    /// <summary>Daily value of the positions from the first purchase to today.</summary>
    Task<GrowthResponse> GetGrowthAsync(GrowthRequest request, CancellationToken cancellationToken = default);

    /// <summary>Max-Sharpe weights and the metrics of the current and the optimised portfolio.</summary>
    Task<OptimisationResponse> OptimiseAsync(OptimisationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Volatility, VaR 95 % and maximum drawdown the trained model expects over the next three months, for the
    /// portfolio and its stocks. A stock listed less than a year ago is a <see cref="MathModuleRejectedException"/>
    /// with the code <c>not_enough_history</c>.
    /// </summary>
    Task<ForecastResponse> ForecastAsync(ForecastRequest request, CancellationToken cancellationToken = default);
}
