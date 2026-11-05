using System.ComponentModel.DataAnnotations;

namespace PortfolioManager.Common.DTO.Auth;

/// <summary>
/// There is deliberately no Role here: every new account gets the "investor" role on the server,
/// so nobody can register themselves as an admin.
/// </summary>
public class SignUpDTO
{
    [Required(ErrorMessage = "Вкажіть пошту.")]
    [EmailAddress(ErrorMessage = "Некоректна адреса пошти.")]
    public string Email { get; set; } = String.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    public string Password { get; set; } = String.Empty;

    [Required(ErrorMessage = "Вкажіть ім'я та прізвище.")]
    [MaxLength(50, ErrorMessage = "Ім'я та прізвище разом мають бути не довші за 50 символів.")]
    public string FullName { get; set; } = String.Empty;
}
