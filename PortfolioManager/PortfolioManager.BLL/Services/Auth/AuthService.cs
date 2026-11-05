using Microsoft.AspNetCore.Identity;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class AuthService : IAuthService
{
    public const string DefaultRole = "investor";
    public const string InvalidCredentialsMessage = "Невірна пошта або пароль.";
    public const string LockedOutMessage = "Забагато невдалих спроб входу. Спробуйте ще раз за кілька хвилин.";
    public const string SessionExpiredMessage = "Сесія завершилась. Увійдіть знову.";

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _time;

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager,
        ITokenService tokenService, TimeProvider time)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _time = time;
    }

    public async Task<AuthResultDTO> SignUp(SignUpDTO model)
    {
        var email = model.Email.Trim();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            FullName = model.FullName.Trim(),
            UserName = email,
            Email = email
        };

        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
            throw new ValidationFailedException(created.Errors.Select(e => e.Description));

        try
        {
            var roleAdded = await _userManager.AddToRoleAsync(user, DefaultRole);
            if (!roleAdded.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleAdded.Errors.Select(e => e.Description)));
        }
        catch
        {
            // Don't leave behind an account without a role: the user could not use it and could not sign up again.
            await _userManager.DeleteAsync(user);
            throw;
        }

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResultDTO> SignIn(SignInDTO model)
    {
        // Accounts are created with UserName = Email, and user names are unique.
        var user = await _userManager.FindByNameAsync(model.Email.Trim());

        // Same message for an unknown email and a wrong password, so the form can't be used to find accounts.
        if (user == null)
            throw new AuthenticationFailedException(InvalidCredentialsMessage);

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            throw new AuthenticationFailedException(LockedOutMessage);

        if (!result.Succeeded)
            throw new AuthenticationFailedException(InvalidCredentialsMessage);

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResultDTO> Refresh(string? refreshToken)
    {
        var user = string.IsNullOrWhiteSpace(refreshToken)
            ? null
            : await _tokenService.FindUserByRefreshTokenAsync(refreshToken);

        if (user == null || user.TokenExpires <= _time.GetUtcNow().UtcDateTime)
            throw new AuthenticationFailedException(SessionExpiredMessage);

        if (await _userManager.IsLockedOutAsync(user))
            throw new AuthenticationFailedException(LockedOutMessage);

        // Rotation: the token just used stops working, because IssueTokensAsync stores a new one.
        return await IssueTokensAsync(user);
    }

    public async Task SignOut(string? refreshToken, string? userId)
    {
        User? user = null;

        if (!string.IsNullOrWhiteSpace(refreshToken))
            user = await _tokenService.FindUserByRefreshTokenAsync(refreshToken);

        if (user == null && !string.IsNullOrWhiteSpace(userId))
            user = await _userManager.FindByIdAsync(userId);

        // Signing out must always succeed for the caller, even when the session is already gone.
        if (user != null)
            await _tokenService.RevokeRefreshTokenAsync(user);
    }

    private async Task<AuthResultDTO> IssueTokensAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _tokenService.StoreRefreshTokenAsync(user, refreshToken);

        return new AuthResultDTO
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.Expires,
            User = new UserDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            }
        };
    }
}
