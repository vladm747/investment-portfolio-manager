namespace PortfolioManager.Common.DTO.Auth;

/// <summary>Returned by the Google sign-in and by refresh.</summary>
public class AuthResultDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDTO User { get; set; } = new();
}
