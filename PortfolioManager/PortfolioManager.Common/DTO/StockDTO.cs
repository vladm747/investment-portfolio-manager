using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO;

public class StockDTO
{
    public int Id { get; set; }
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? CurrentPrice { get; set; }    
    public decimal? TotalValue { get; set; } //Quantity * CurrentPrice
    public decimal Gain { get; set; } //Quantity * CurrentPrice - Quantity * EntryPrice
    public decimal GainPercentage { get; set; } //Gain / (EntryPrice * Quantity) * 100
    public DateTime EntryDate { get; set; }
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty; 
    public SectorEnum Sector { get; set; } 
    public int PortfolioId { get; set; }
    
}