using Microsoft.AspNetCore.Identity;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.BLL.Services.Auth;

public class AuthService : IAuthService
{
    public const string DefaultRole = "investor";
    public const string GoogleProvider = "Google";
    public const string InvalidGoogleTokenMessage = "Не вдалося підтвердити вхід через Google. Спробуйте ще раз.";
    public const string UnverifiedEmailMessage = "Google не підтвердив пошту цього акаунта. Увійдіть з іншим акаунтом Google.";
    public const string EmailBelongsToAnotherGoogleAccountMessage =
        "Ця пошта вже належить іншому акаунту Google. Увійдіть з тим акаунтом, з яким ви входили раніше.";
    public const string LockedOutMessage = "Обліковий запис заблоковано. Зверніться до адміністратора.";
    public const string SessionExpiredMessage = "Сесія завершилась. Увійдіть знову.";

    /// <summary>Same limit as <see cref="User.FullName"/> in the database.</summary>
    private const int MaxFullNameLength = 50;

    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly GoogleAuthSettings _googleSettings;
    private readonly TimeProvider _time;

    public AuthService(UserManager<User> userManager, ITokenService tokenService,
        IGoogleTokenValidator googleTokenValidator, GoogleAuthSettings googleSettings, TimeProvider time)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _googleTokenValidator = googleTokenValidator;
        _googleSettings = googleSettings;
        _time = time;
    }

    public async Task<AuthResultDTO> SignInWithGoogle(GoogleSignInDTO model, CancellationToken cancellationToken = default)
    {
        var identity = await _googleTokenValidator.ValidateAsync(model.Credential, cancellationToken);

        // The e-mail is what links an account to a person, so it must be one Google vouches for.
        if (string.IsNullOrWhiteSpace(identity.Email) || !identity.EmailVerified)
            throw new AuthenticationFailedException(UnverifiedEmailMessage);

        var email = identity.Email.Trim();
        var user = await FindOrCreateUserAsync(identity, email);

        // No password means no failed attempts to count, so a lockout is now an account-disable switch:
        // set LockoutEnd of a user far in the future to keep them out.
        if (await _userManager.IsLockedOutAsync(user))
            throw new AuthenticationFailedException(LockedOutMessage);

        // Grant only: taking an e-mail off the list does not strip a role it already gave.
        if (_googleSettings.IsAdmin(email) && !await _userManager.IsInRoleAsync(user, RoleSeed.AdminRoleName))
            await AddToRoleAsync(user, RoleSeed.AdminRoleName);

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

    /// <summary>
    /// Google's account id (<c>sub</c>) is the key: the e-mail of a Google account can change, the id cannot.
    /// An account is found by it, else adopted by e-mail (an old test account), else created.
    /// </summary>
    private async Task<User> FindOrCreateUserAsync(GoogleIdentity identity, string email)
    {
        var user = await _userManager.FindByLoginAsync(GoogleProvider, identity.Subject);
        if (user != null)
            return user;

        user = await _userManager.FindByEmailAsync(email);
        if (user != null)
            return await LinkAsync(user, identity);

        user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = DisplayName(identity.Name, email)
        };

        var created = await _userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            // Two first sign-ins at the same moment (two tabs) race on the unique user name: the loser uses the winner's account.
            var winner = await _userManager.FindByLoginAsync(GoogleProvider, identity.Subject);
            if (winner != null)
                return winner;

            throw new InvalidOperationException("Can't create the account: " + Describe(created));
        }

        try
        {
            Ensure(await _userManager.AddLoginAsync(user, GoogleLogin(identity)));
            await AddToRoleAsync(user, DefaultRole);
        }
        catch
        {
            // Don't leave behind an account without a role or a login: it could never be used, and its e-mail would stay taken.
            await _userManager.DeleteAsync(user);
            throw;
        }

        return user;
    }

    /// <summary>
    /// A leftover account with this e-mail (made when sign-up used a password) now belongs to the Google account:
    /// the password is removed, so Google is the only way in. If another Google account is already linked to it, the
    /// e-mail address has changed hands at Google, and giving this person the old owner's data would be a takeover.
    /// </summary>
    private async Task<User> LinkAsync(User user, GoogleIdentity identity)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(login => login.LoginProvider == GoogleProvider))
            throw new AuthenticationFailedException(EmailBelongsToAnotherGoogleAccountMessage);

        Ensure(await _userManager.AddLoginAsync(user, GoogleLogin(identity)));

        if (await _userManager.HasPasswordAsync(user))
            Ensure(await _userManager.RemovePasswordAsync(user));

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            Ensure(await _userManager.UpdateAsync(user));
        }

        return user;
    }

    private async Task AddToRoleAsync(User user, string role) =>
        Ensure(await _userManager.AddToRoleAsync(user, role));

    private static UserLoginInfo GoogleLogin(GoogleIdentity identity) =>
        new(GoogleProvider, identity.Subject, GoogleProvider);

    /// <summary>The name Google shows, or the part of the e-mail before the @ when it has none.</summary>
    private static string DisplayName(string? googleName, string email)
    {
        var name = string.IsNullOrWhiteSpace(googleName) ? email.Split('@')[0] : googleName.Trim();
        return name.Length > MaxFullNameLength ? name[..MaxFullNameLength] : name;
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(Describe(result));
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));

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
