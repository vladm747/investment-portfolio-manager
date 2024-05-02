using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class TokenService: ITokenService
{
    private readonly UserManager<User> _userManager;

    public TokenService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }
    
    public async Task<IdentityResult> UpdateUserRefreshToken(User user, RefreshTokenDTO token)
    {
        if (user == null)
            throw new UserNotFoundException("User Not Found");

        user.RefreshToken = token.Token;
        user.TokenCreated = token.Created;
        user.TokenExpires = token.Expires;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new UserTokenUpdateException("Can't update user. TokenService.UpdateUserRefreshToken");
        
        return result;
    }

    private List<Claim> SetUserClaims(User user)
    {
        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.Id),
            new (ClaimTypes.Name, user.FullName),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (ClaimTypes.NameIdentifier, user.Id),
            new (ClaimTypes.Role, "investor")
        };

        return claims;
    }

    private IEnumerable<Claim> SetRoleClaims(IEnumerable<string> roles) =>
        roles.Select(r => new Claim(ClaimTypes.Role, r));
    
    public string GenerateJwtToken(User user, IEnumerable<string> roles, JwtSettings jwtSettings)
    {
        if (user == null) throw new Exception($"Jwt generation not proceeded - {nameof(user)} is null");

        var claims = SetUserClaims(user);
        
        var roleClaims = SetRoleClaims(roles);
        
        claims.AddRange(roleClaims);
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddHours(Convert.ToDouble(jwtSettings.ExpirationInHours));

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Issuer,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshTokenDTO GenerateRefreshToken(JwtSettings jwtSettings)
    {
        var refreshToken = new RefreshTokenDTO()
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.Now.AddDays(7),
            Created = DateTime.Now
        };
        
        return refreshToken;
    }
}