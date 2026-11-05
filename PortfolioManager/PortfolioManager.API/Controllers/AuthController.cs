using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
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
    public new async Task<IActionResult> SignOut(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDTO? request)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        await _authService.SignOut(GetRefreshToken(request), userId);
        Response.Cookies.Delete(RefreshTokenCookie, RefreshTokenCookieOptions());

        return NoContent();
    }

    [HttpPost("signin-google")]
    public async Task<IActionResult> SignInGoogle()
    {
        return new ChallengeResult(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", "Auth") // Where google responds back
            });
    }

    /// <summary>
    /// Google Login Response After Login Operation From Google Page
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GoogleResponse()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
            return BadRequest("troubles with auth result in GoogleResponce"); // TODO: Handle this better.

        //Check if the redirection has been done via google or any other links
        if (authenticateResult.Principal.Identities.ToList()[0].AuthenticationType.ToLower() == "google")
        {
            //check if principal value exists or not
            if (authenticateResult.Principal != null)
            {
                //get google account id for any operation to be carried out on the basis of the id
                var googleAccountId = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //claim value initialization as mentioned on the startup file with o.DefaultScheme = "Application"
                var claimsIdentity = new ClaimsIdentity("Application");
                if (authenticateResult.Principal != null)
                {
                    //Now add the values on claim and redirect to the page to be accessed after successful login
                    var details = authenticateResult.Principal.Claims.ToList();
                    claimsIdentity.AddClaim(authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier));// Full Name Of The User
                    claimsIdentity.AddClaim(authenticateResult.Principal.FindFirst(ClaimTypes.Email)); // Email Address of The User
                    await HttpContext.SignInAsync(GoogleDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Index", "Portfolio");
                }
            }
        }
        return RedirectToAction("Index", "Portfolio");
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
