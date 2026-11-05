using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface IAuthService
{
    /// <summary>Creates an investor account and signs it in.</summary>
    Task<AuthResultDTO> SignUp(SignUpDTO model);

    Task<AuthResultDTO> SignIn(SignInDTO model);

    /// <summary>Exchanges a valid refresh token for a new access token and a new refresh token.</summary>
    Task<AuthResultDTO> Refresh(string? refreshToken);

    /// <summary>Revokes the refresh token, found by its value or, failing that, by the signed-in user's id.</summary>
    Task SignOut(string? refreshToken, string? userId);
}
