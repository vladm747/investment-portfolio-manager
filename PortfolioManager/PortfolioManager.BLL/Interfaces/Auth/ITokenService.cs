using Microsoft.AspNetCore.Identity;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Interfaces.Auth;

public interface ITokenService
{
    Task<IdentityResult> UpdateUserRefreshToken(User user, RefreshTokenDTO token);
    string GenerateJwtToken(User user, IEnumerable<string> roles, JwtSettings jwtSettings);
    RefreshTokenDTO GenerateRefreshToken(JwtSettings jwtSettings);
}