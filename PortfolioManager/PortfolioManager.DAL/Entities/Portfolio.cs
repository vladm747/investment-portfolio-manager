using System.ComponentModel.DataAnnotations;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.DAL.Entities;

public class Portfolio
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal TotalEntryValue {get; set; }
    public decimal TotalCurrentValue {get; set; }
    public decimal TotalGain {get; set; } //TotalCurrentValue - TotalEntryValue
    
    public string UserId { get; set; } = string.Empty;
    public virtual User? User { get; set; }
    
    public ICollection<Stock>? Stocks {get; set;}
}