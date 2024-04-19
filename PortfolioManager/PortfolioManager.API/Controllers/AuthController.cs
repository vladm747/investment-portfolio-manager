using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PortfolioManager.API.Models;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly IAuthService _authService;
    private readonly IRoleService _roleService;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    
    public AuthController(IAuthService authService, IOptionsSnapshot<JwtSettings> jwtSettings,
        IUserService userService, IRoleService roleService, ITokenService tokenService)
    {
        _roleService = roleService;
        _authService = authService;
        _userService = userService;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp(SignUpDTO model)
    {
        var userId = await _authService.SignUp(model);

        return Ok(userId);
    }

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInDTO model)
    {
        var user = await _authService.SignIn(model);
        var roles = await _roleService.GetRoles(user);
        var accessToken = _tokenService.GenerateJwtToken(user, roles, _jwtSettings);
        var refreshToken = _tokenService.GenerateRefreshToken(_jwtSettings);
        
        SetRefreshToken(refreshToken.Token);
        SetAccessToken(accessToken);
        
        var result = await _tokenService.UpdateUserRefreshToken(user, refreshToken);
        
        return Ok(new Tokens(){AccessToken = accessToken, RefreshToken = refreshToken.Token});
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        var user = _userService.GetUserById(HttpContext.User);

        if (!user.RefreshToken.Equals(refreshToken))
            return Unauthorized("Invalid Refresh Token");
        
        if(user.TokenExpires < DateTime.Now)
            return Unauthorized("Token expired");
        
        var roles = await _roleService.GetRoles(user);

        string jwtToken = _tokenService.GenerateJwtToken(user, roles, _jwtSettings);
        var newRefreshToken = _tokenService.GenerateRefreshToken(_jwtSettings);
        
        SetRefreshToken(newRefreshToken.Token);
        SetAccessToken(jwtToken);
        
        var result = await _tokenService.UpdateUserRefreshToken(user, newRefreshToken);
        
        return Ok(jwtToken);
    }
    
    [HttpPost("test-refresh-token")]
    public async Task<IActionResult> TestRefreshToken([FromBody] string refreshToken)
    {
        var user = _userService.GetUserById(HttpContext.User);

        if (!user.RefreshToken.Equals(refreshToken))
            return Unauthorized("Invalid Refresh Token");
        
        if(user.TokenExpires < DateTime.Now)
            return Unauthorized("Token expired");
        
        var roles = await _roleService.GetRoles(user);

        string jwtToken = _tokenService.GenerateJwtToken(user, roles, _jwtSettings);
        var newRefreshToken = _tokenService.GenerateRefreshToken(_jwtSettings);
        
        SetRefreshToken(newRefreshToken.Token);
        SetAccessToken(jwtToken);
        
        var result = await _tokenService.UpdateUserRefreshToken(user, newRefreshToken);
        
        return Ok(jwtToken);
    }
    
    [HttpPost("sign-out")]
    [Authorize]
    public new async Task<IActionResult> SignOut()
    {
        await _authService.SignOut();
        
        SetAccessTokenNull();
        SetRefreshTokenNull();

        return Ok();
    }
    
    private void SetRefreshToken(string token)
    {
        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTimeOffset.Now.AddDays(7)
        };
        
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
    
    private void SetAccessToken(string accessToken)
    {
        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTimeOffset.Now.AddHours(_jwtSettings.ExpirationInHours)
        };
       
        Response.Cookies.Append("accessToken", accessToken, cookieOptions);
    }
    
    private void SetAccessTokenNull()
    {
        var cookieOptions = new CookieOptions
        {
            MaxAge = TimeSpan.FromMilliseconds(1),
            SameSite = SameSiteMode.None,
            Secure = true
        };
       
        Response.Cookies.Append("accessToken", "", cookieOptions);
    }
    
    private void SetRefreshTokenNull()
    {
        var cookieOptions = new CookieOptions
        {
            MaxAge = TimeSpan.FromMilliseconds(1),
            SameSite = SameSiteMode.None,
            Secure = true
        };
        
        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }
}