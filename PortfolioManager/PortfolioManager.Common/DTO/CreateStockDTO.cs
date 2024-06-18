namespace PortfolioManager.Common.DTO;

public class CreateStockDTO
{
    public int PortfolioId { get; set; }
    public int Quantity { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public double EntryPrice { get; set; }
    public DateTime EntryDate { get; set; }
}