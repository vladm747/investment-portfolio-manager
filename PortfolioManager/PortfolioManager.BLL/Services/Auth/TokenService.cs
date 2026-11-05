using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class TokenService : ITokenService
{
    private readonly UserManager<User> _userManager;
    private readonly JwtSettings _settings;
    private readonly TimeProvider _time;

    public TokenService(UserManager<User> userManager, IOptions<JwtSettings> settings, TimeProvider time)
    {
        _userManager = userManager;
        _settings = settings.Value;
        _time = time;
    }

    public AccessToken GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _time.GetUtcNow().UtcDateTime;
        var expires = now.Add(_settings.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Issuer,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public RefreshTokenDTO GenerateRefreshToken()
    {
        var now = _time.GetUtcNow().UtcDateTime;

        return new RefreshTokenDTO
        {
            Token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64)),
            Created = now,
            Expires = now.Add(_settings.RefreshTokenLifetime)
        };
    }

    public async Task StoreRefreshTokenAsync(User user, RefreshTokenDTO token)
    {
        user.RefreshToken = Hash(token.Token);
        user.TokenCreated = token.Created;
        user.TokenExpires = token.Expires;

        await UpdateAsync(user);
    }

    public async Task<User?> FindUserByRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var hash = Hash(refreshToken);
        return await _userManager.Users.FirstOrDefaultAsync(user => user.RefreshToken == hash);
    }

    public async Task RevokeRefreshTokenAsync(User user)
    {
        user.RefreshToken = string.Empty;
        user.TokenExpires = _time.GetUtcNow().UtcDateTime;

        await UpdateAsync(user);
    }

    /// <summary>
    /// Only a hash is stored, so a leaked database copy cannot be used to take over sessions.
    /// The token is 512 random bits, so a fast unsalted hash is enough.
    /// </summary>
    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task UpdateAsync(User user)
    {
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new UserTokenUpdateException(
                "Can't update the user's refresh token: " + string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
