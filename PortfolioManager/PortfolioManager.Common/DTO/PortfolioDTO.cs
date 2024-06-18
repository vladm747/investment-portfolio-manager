namespace PortfolioManager.Common.DTO;

public class PortfolioDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public double TotalEntryPrice {get; set; }
    public double TotalCurrentPrice {get; set; }
    public double TotalGain {get; set; } //TotalCurrentValue - TotalEntryValue
    public double TotalGainPercentage { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    public ICollection<StockDTO>? Stocks {get; set;}
}