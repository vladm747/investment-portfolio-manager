using PortfolioManager.BLL.Valuation;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;

namespace PortfolioManager.BLL.Mapping;

public static class StockMapping
{
    /// <summary>
    /// The position as the client sees it. Its value, gain and gain % are worked out here from the quantity and the
    /// prices (the database stores none of them), so they are right after any change to the position.
    /// </summary>
    public static StockDTO ToDto(this Stock stock)
    {
        var valuation = stock.Valuation();

        return new StockDTO
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            Name = stock.Name,
            Quantity = stock.Quantity,
            EntryPrice = stock.EntryPrice,
            CurrentPrice = stock.CurrentPrice,
            TotalCurrentPrice = valuation.CurrentValue,
            Gain = valuation.Gain,
            GainPercentage = valuation.GainPercentage,
            EntryDate = stock.EntryDate,
            Currency = stock.Currency,
            Sector = stock.Sector,
            PortfolioId = stock.PortfolioId
        };
    }
}
