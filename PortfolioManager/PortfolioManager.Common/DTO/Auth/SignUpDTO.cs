namespace PortfolioManager.Common.DTO.Auth;

public class SignUpDTO
{
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string FullName { get; set; } = String.Empty;
}