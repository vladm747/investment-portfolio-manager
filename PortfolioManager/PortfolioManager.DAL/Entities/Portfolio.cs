using System.ComponentModel.DataAnnotations;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.DAL.Entities;

/// <summary>
/// A user's portfolio. Its totals are not stored: they are the sums over <see cref="Stocks"/>, worked out when
/// they are read (see PortfolioManager.BLL Valuation).
/// </summary>
public class Portfolio
{
    [Key]
    public int Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public virtual User? User { get; set; }

    public ICollection<Stock>? Stocks {get; set;}
}
