namespace PortfolioManager.Common.DTO;

public class PortfolioDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal TotalEntryPrice {get; set; }
    public decimal TotalCurrentPrice {get; set; }
    public decimal TotalGain {get; set; } //TotalCurrentValue - TotalEntryValue
    
    public string UserId { get; set; } = string.Empty;
    
    public ICollection<StockDTO>? Stocks {get; set;}
}