using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.DAL.Entities;

public class StockDataHistory
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    [MaxLength(15)]
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
}