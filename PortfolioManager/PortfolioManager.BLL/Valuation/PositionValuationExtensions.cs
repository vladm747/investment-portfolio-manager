using PortfolioManager.DAL.Entities;

namespace PortfolioManager.BLL.Valuation;

public static class PositionValuationExtensions
{
    /// <summary>The value, gain and gain % of a position, from its quantity and its two prices.</summary>
    public static PositionValuation Valuation(this Stock stock) =>
        PositionValuation.Of(stock.Quantity, stock.EntryPrice, stock.CurrentPrice);
}
