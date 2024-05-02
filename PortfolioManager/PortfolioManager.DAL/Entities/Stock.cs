using System.ComponentModel.DataAnnotations;
using PortfolioManager.Common;

namespace PortfolioManager.DAL.Entities;

public class Stock
{
    [Key]
    public int Id { get; set; }
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal EntryValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Gain { get; set; } //Quantity * CurrentPrice - Quantity * EntryPrice
    public decimal GainPercentage { get; set; } //Gain / (EntryPrice * Quantity) * 100
    public DateTime EntryDate { get; set; }
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty; 
    public SectorEnum Sector { get; set; } 
    public int PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
}