using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.DAL.Entities;

public class StockSymbol
{ 
    [Key]
    public string Symbol { get; set; } = null!;

    public string? Name { get; set; }

    public string? Sector { get; set; }
}