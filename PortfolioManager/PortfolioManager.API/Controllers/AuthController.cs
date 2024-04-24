using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PortfolioManager.API.Models;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly IAuthService _authService;
    private readonly IRoleService _roleService;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly SignInManager<User> _signInManager;
    
    
    public AuthController(IAuthService authService, IOptionsSnapshot<JwtSettings> jwtSettings, 
        SignInManager<User> signInManager, IUserService userService, IRoleService roleService,
        ITokenService tokenService)
    {
        _roleService = roleService;
        _authService = authService;
        _userService = userService;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _signInManager = signInManager;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp(SignUpDTO model)
    {
        var userId = await _authService.SignUp(model);

        return Ok(userId);
    }

    [HttpPost("signin")]
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
    
    [AllowAnonymous]
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