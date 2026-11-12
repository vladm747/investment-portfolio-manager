using PortfolioManager.MathModule.Client.Contract;

namespace PortfolioManager.MathModule.Client;

/// <summary>
/// The Python math service: market data from Yahoo Finance, portfolio growth and max-Sharpe optimisation.
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
}
