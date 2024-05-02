namespace PortfolioManager.DAL.Entities;

public class StockDataHistory
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
}