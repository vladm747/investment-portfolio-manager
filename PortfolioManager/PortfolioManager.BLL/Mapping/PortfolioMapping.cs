using PortfolioManager.BLL.Valuation;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;

namespace PortfolioManager.BLL.Mapping;

public static class PortfolioMapping
{
    /// <summary>
    /// The portfolio with its positions. The totals are the sums over the positions, worked out here every time:
    /// nothing derived is stored, so nothing can go stale.
    /// </summary>
    public static PortfolioDTO ToDto(this Portfolio portfolio)
    {
        var stocks = portfolio.Stocks ?? new List<Stock>();
        var total = PositionValuation.Sum(stocks.Select(stock => stock.Valuation()));

        return new PortfolioDTO
        {
            Id = portfolio.Id,
            Name = portfolio.Name,
            UserId = portfolio.UserId,
            TotalEntryPrice = total.EntryValue,
            TotalCurrentPrice = total.CurrentValue,
            TotalGain = total.Gain,
            TotalGainPercentage = total.GainPercentage,
            Stocks = stocks.Select(stock => stock.ToDto()).ToList()
        };
    }
}
