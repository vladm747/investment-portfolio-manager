using System.ComponentModel.DataAnnotations;
using PortfolioManager.Common.Validation;

namespace PortfolioManager.Common.DTO;

/// <summary>
/// What the client sends to add a position: <c>POST api/portfolios/{portfolioId}/positions</c>. The portfolio is in
/// the address, not in the body, so the two can never disagree.
/// </summary>
public class CreateStockDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Кількість акцій має бути більшою за нуль.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = SymbolFormat.ErrorMessage)]
    [RegularExpression(SymbolFormat.Pattern, ErrorMessage = SymbolFormat.ErrorMessage)]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Above zero. The lower bound is the smallest price the database column keeps (four decimals); a price that
    /// small would be stored as zero otherwise.
    /// </summary>
    [Range(typeof(decimal), "0.0001", "1000000000000", ErrorMessage = "Ціна купівлі має бути більшою за нуль.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal EntryPrice { get; set; }

    [NotInFuture(ErrorMessage = "Дата купівлі не може бути в майбутньому.", TooEarlyMessage = "Вкажіть дату купівлі.")]
    public DateTime EntryDate { get; set; }
}
