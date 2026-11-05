using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken GenerateAccessToken(User user, IEnumerable<string> roles);
    RefreshTokenDTO GenerateRefreshToken();

    /// <summary>Saves a hash of the refresh token on the user, replacing the previous one.</summary>
    Task StoreRefreshTokenAsync(User user, RefreshTokenDTO token);

    /// <summary>Finds the user whose stored hash matches this refresh token, expired or not.</summary>
    Task<User?> FindUserByRefreshTokenAsync(string refreshToken);

    Task RevokeRefreshTokenAsync(User user);
}
