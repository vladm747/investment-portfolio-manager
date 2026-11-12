using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PortfolioManager.Common;

namespace PortfolioManager.DAL.Entities;

/// <summary>
/// One holding of a portfolio. Only what was typed in or fetched is stored: the value, gain and gain % are worked
/// out from the quantity and the two prices every time they are read, so they can never disagree with them.
/// </summary>
public class Stock
{
    [Key]
    public int Id { get; set; }
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>What was paid for one share. Money is exact: decimal(18,4), never a binary float.</summary>
    [Precision(18, 4)]
    public decimal EntryPrice { get; set; }

    /// <summary>The last price fetched from the market data service.</summary>
    [Precision(18, 4)]
    public decimal CurrentPrice { get; set; }

    public DateTime EntryDate { get; set; }
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;
    public SectorEnum Sector { get; set; }
    public int PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
}
