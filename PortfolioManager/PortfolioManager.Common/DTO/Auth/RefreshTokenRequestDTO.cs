namespace PortfolioManager.Common.DTO.Auth;

/// <summary>Body of refresh-token and sign-out. When the token is missing, the API falls back to its HttpOnly cookie.</summary>
public class RefreshTokenRequestDTO
{
    public string? RefreshToken { get; set; }
}
