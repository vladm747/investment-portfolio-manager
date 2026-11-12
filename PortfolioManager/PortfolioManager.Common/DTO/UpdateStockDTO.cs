using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO;

/// <summary>What the client sends to change a position: <c>PUT api/positions/{id}</c>.</summary>
public class UpdateStockDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Кількість акцій має бути більшою за нуль.")]
    public int Quantity { get; set; }
}
