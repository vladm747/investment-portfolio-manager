using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace PortfolioManager.Common.DTO.Auth;

public class SignInGoogleDTO
{
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public bool RememberMe { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }
}