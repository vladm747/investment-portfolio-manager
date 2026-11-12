using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.API.Controllers;

/// <summary>
/// The client keeps the access token in memory and the refresh token in its own storage, and sends the
/// refresh token in the body of refresh-token and sign-out. The API also sets the refresh token as an
/// HttpOnly cookie and reads it when the body has none, so the client can switch to cookie-only
/// sessions if it is ever served from the same site as the API.
/// </summary>
[Route("api/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookie = "pm_refresh_token";
    private const string RefreshTokenCookiePath = "/api/auth";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResultDTO>> SignUp(SignUpDTO model)
    {
        var result = await _authService.SignUp(model);
        SetRefreshTokenCookie(result);

        return Ok(result);
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResultDTO>> SignIn(SignInDTO model)
    {
        var result = await _authService.SignIn(model);
        SetRefreshTokenCookie(result);

        return Ok(result);
    }

    /// <summary>Works without an access token: that is the whole point when the access token has expired.</summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResultDTO>> RefreshToken(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDTO? request)
    {
        var result = await _authService.Refresh(GetRefreshToken(request));
        SetRefreshTokenCookie(result);

        return Ok(result);
    }

    /// <summary>Always returns 204, even when the session had already expired.</summary>
    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDTO? request)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        await _authService.SignOut(GetRefreshToken(request), userId);
        Response.Cookies.Delete(RefreshTokenCookie, RefreshTokenCookieOptions());

        return NoContent();
    }

    private string? GetRefreshToken(RefreshTokenRequestDTO? request) =>
        string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? Request.Cookies[RefreshTokenCookie]
            : request.RefreshToken;

    private void SetRefreshTokenCookie(AuthResultDTO result)
    {
        var options = RefreshTokenCookieOptions();
        options.Expires = result.RefreshTokenExpiresAt;

        Response.Cookies.Append(RefreshTokenCookie, result.RefreshToken, options);
    }

    /// <summary>The client and the API are different sites on Azure, so the cookie needs SameSite=None, which needs Secure.</summary>
    private static CookieOptions RefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = RefreshTokenCookiePath
    };
}
