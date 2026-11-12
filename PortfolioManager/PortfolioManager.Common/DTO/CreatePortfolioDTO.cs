using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO;

/// <summary>What the client sends to create a portfolio. The owner is never part of it: it is the signed-in user.</summary>
public class CreatePortfolioDTO
{
    [Required(ErrorMessage = "Вкажіть назву портфеля.")]
    [StringLength(100, ErrorMessage = "Назва портфеля має бути не довшою за 100 символів.")]
    public string Name { get; set; } = string.Empty;
}
