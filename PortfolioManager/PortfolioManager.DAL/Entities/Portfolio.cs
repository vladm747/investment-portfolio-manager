using System.ComponentModel.DataAnnotations;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.DAL.Entities;

public class Portfolio
{
    [Key]
    public int Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public double TotalEntryPrice {get; set; }
    public double TotalCurrenPrice {get; set; }
    public double? TotalGain {get; set; } //TotalCurrentValue - TotalEntryValue
    public double? TotalGainPercentage {get; set; } //    
    public string UserId { get; set; } = string.Empty;
    public virtual User? User { get; set; }
    
    public ICollection<Stock>? Stocks {get; set;}
}