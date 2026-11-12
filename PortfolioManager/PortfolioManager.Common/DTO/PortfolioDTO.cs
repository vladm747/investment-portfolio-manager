namespace PortfolioManager.Common.DTO;

/// <summary>A portfolio with its positions. Every total is the sum over the positions, worked out on every read.</summary>
public class PortfolioDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal TotalEntryPrice { get; set; }
    public decimal TotalCurrentPrice { get; set; }
    public decimal TotalGain { get; set; } // TotalCurrentPrice - TotalEntryPrice
    public decimal TotalGainPercentage { get; set; }
    public string UserId { get; set; } = string.Empty;

    public ICollection<StockDTO>? Stocks {get; set;}
}
