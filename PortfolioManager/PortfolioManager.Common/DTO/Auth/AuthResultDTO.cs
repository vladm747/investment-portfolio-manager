namespace PortfolioManager.Common.DTO.Auth;

/// <summary>Returned by sign-up, sign-in and refresh.</summary>
public class AuthResultDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDTO User { get; set; } = new();
}
