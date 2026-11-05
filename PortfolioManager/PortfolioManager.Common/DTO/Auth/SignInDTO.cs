using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO.Auth;

public class SignInDTO
{
    [Required(ErrorMessage = "Вкажіть пошту.")]
    public string Email { get; set; } = String.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    public string Password { get; set; } = String.Empty;
}
