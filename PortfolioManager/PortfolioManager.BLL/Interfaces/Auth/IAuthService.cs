using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface IAuthService
{
    /// <summary>
    /// The only way in. Checks Google's ID token, finds the account it belongs to (or creates an investor account,
    /// or links an old test account with the same e-mail) and signs it in.
    /// </summary>
    Task<AuthResultDTO> SignInWithGoogle(GoogleSignInDTO model, CancellationToken cancellationToken = default);

    /// <summary>Exchanges a valid refresh token for a new access token and a new refresh token.</summary>
    Task<AuthResultDTO> Refresh(string? refreshToken);

    /// <summary>Revokes the refresh token, found by its value or, failing that, by the signed-in user's id.</summary>
    Task SignOut(string? refreshToken, string? userId);
}
