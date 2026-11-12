using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO;

/// <summary>A position as the client sees it. Money is <c>decimal</c> (the database column is decimal(18,4)).</summary>
public class StockDTO
{
    public int Id { get; set; }
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }

    /// <summary>What the position is worth now: quantity times current price. Worked out on every read, not stored.</summary>
    public decimal TotalCurrentPrice { get; set; }

    /// <summary>Current value minus what was paid. Worked out on every read, not stored.</summary>
    public decimal Gain { get; set; }

    /// <summary>Gain as a percentage of what was paid, to four decimals. Worked out on every read, not stored.</summary>
    public decimal GainPercentage { get; set; }

    public DateTime EntryDate { get; set; }
    [MaxLength(10)]
    public string Currency { get; set; } = string.Empty;
    public SectorEnum Sector { get; set; }
    public int PortfolioId { get; set; }
}
