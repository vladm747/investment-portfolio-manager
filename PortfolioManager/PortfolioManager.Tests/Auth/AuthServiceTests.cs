using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Services.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private static readonly GoogleIdentity Olena = AuthTestHost.Olena;
    private static readonly string Email = Olena.Email!;

    private readonly AuthTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private Task<AuthResultDTO> SignInOlena() => _host.SignInAsync(Olena);

    private Task<int> CountUsers() => _host.Db.Users.CountAsync();

    // ---- signing in with Google -------------------------------------------------------------------------------

    [Fact]
    public async Task The_first_sign_in_creates_an_investor_signs_them_in_and_takes_the_name_from_Google()
    {
        var result = await SignInOlena();

        Assert.Equal("Олена Коваль", result.User.FullName);
        Assert.Equal(Email, result.User.Email);
        Assert.Equal(new[] { AuthService.DefaultRole }, result.User.Roles);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task The_new_account_is_linked_to_the_Google_account_and_has_no_password()
    {
        await SignInOlena();

        var user = (await _host.Users.FindByLoginAsync("Google", Olena.Subject))!;
        Assert.Equal(Email, user.Email);
        Assert.True(user.EmailConfirmed);
        Assert.False(await _host.Users.HasPasswordAsync(user));
    }

    [Fact]
    public async Task Only_a_hash_of_the_refresh_token_is_stored()
    {
        var result = await SignInOlena();

        var user = await _host.Users.FindByNameAsync(Email);
        Assert.NotNull(user);
        Assert.NotEqual(result.RefreshToken, user!.RefreshToken);
        Assert.False(string.IsNullOrEmpty(user.RefreshToken));
    }

    [Fact]
    public async Task A_second_sign_in_is_the_same_user_and_creates_no_other_account()
    {
        var first = await SignInOlena();
        var second = await SignInOlena();

        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Equal(1, await CountUsers());
    }

    [Fact]
    public async Task The_same_Google_account_with_a_changed_email_is_still_the_same_user()
    {
        var first = await SignInOlena();

        var second = await _host.SignInAsync(Olena with { Email = "olena.new@example.com" });

        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Equal(1, await CountUsers());
    }

    [Fact]
    public async Task A_different_Google_account_gets_a_different_user()
    {
        var olena = await SignInOlena();

        var ivan = await _host.SignInAsync(new GoogleIdentity("google-sub-ivan", "ivan@example.com", true, "Іван Мельник"));

        Assert.NotEqual(olena.User.Id, ivan.User.Id);
        Assert.Equal(2, await CountUsers());
    }

    [Fact]
    public async Task An_old_account_with_the_same_email_is_taken_over_by_the_Google_account_and_loses_its_password()
    {
        var old = new User { Id = Guid.NewGuid().ToString(), UserName = Email, Email = Email, FullName = "Стара назва" };
        Assert.True((await _host.Users.CreateAsync(old, "Secret#1")).Succeeded);
        Assert.True((await _host.Users.AddToRoleAsync(old, AuthService.DefaultRole)).Succeeded);

        var result = await SignInOlena();

        Assert.Equal(old.Id, result.User.Id);
        Assert.Equal(1, await CountUsers());
        var linked = (await _host.Users.FindByLoginAsync("Google", Olena.Subject))!;
        Assert.Equal(old.Id, linked.Id);
        Assert.False(await _host.Users.HasPasswordAsync(linked));
        Assert.True(linked.EmailConfirmed);
        Assert.Equal(new[] { AuthService.DefaultRole }, result.User.Roles);
    }

    [Fact]
    public async Task An_email_that_now_belongs_to_another_Google_account_does_not_open_the_old_owners_data()
    {
        var owner = await SignInOlena();

        // The same address, but Google's account id differs: the address has changed hands at Google.
        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _host.SignInAsync(Olena with { Subject = "google-sub-someone-else" }));

        Assert.Equal(AuthService.EmailBelongsToAnotherGoogleAccountMessage, error.Message);
        Assert.Equal(1, await CountUsers());
        Assert.Equal(owner.User.Id, (await _host.Users.FindByLoginAsync("Google", Olena.Subject))!.Id);
        Assert.Null(await _host.Users.FindByLoginAsync("Google", "google-sub-someone-else"));
    }

    [Theory]
    [InlineData("O'Brien@example.com")]
    [InlineData("o.brien+portfolio@example.co.uk")]
    public async Task Any_address_Google_vouches_for_can_sign_in(string email)
    {
        var result = await _host.SignInAsync(new GoogleIdentity("google-sub-1", email, true, "Тест"));

        Assert.Equal(email, result.User.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Without_a_name_the_part_of_the_email_before_the_at_sign_is_used(string? googleName)
    {
        var result = await _host.SignInAsync(Olena with { Name = googleName });

        Assert.Equal("olena", result.User.FullName);
    }

    [Fact]
    public async Task A_name_longer_than_the_database_allows_is_cut()
    {
        var result = await _host.SignInAsync(Olena with { Name = new string('Я', 80) });

        Assert.Equal(50, result.User.FullName.Length);
    }

    [Fact]
    public async Task An_email_Google_has_not_verified_is_rejected_and_creates_no_account()
    {
        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _host.SignInAsync(Olena with { EmailVerified = false }));

        Assert.Equal(AuthService.UnverifiedEmailMessage, error.Message);
        Assert.Equal(0, await CountUsers());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task A_token_without_an_email_is_rejected(string? email)
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.SignInAsync(Olena with { Email = email }));

        Assert.Equal(0, await CountUsers());
    }

    [Theory]
    [InlineData("not-a-token-google-issued")]
    [InlineData("")]
    public async Task A_credential_Google_did_not_issue_is_rejected(string credential)
    {
        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _host.Auth.SignInWithGoogle(new GoogleSignInDTO { Credential = credential }));

        Assert.Equal(AuthService.InvalidGoogleTokenMessage, error.Message);
        Assert.Equal(0, await CountUsers());
    }

    [Fact]
    public async Task A_locked_out_user_is_rejected_even_with_a_valid_Google_token()
    {
        var signIn = await SignInOlena();
        var user = (await _host.Users.FindByIdAsync(signIn.User.Id))!;
        await _host.Users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(SignInOlena);

        Assert.Equal(AuthService.LockedOutMessage, error.Message);
    }

    [Fact]
    public async Task A_locked_out_user_cannot_refresh_their_session_either()
    {
        var signIn = await SignInOlena();
        var user = (await _host.Users.FindByIdAsync(signIn.User.Id))!;
        await _host.Users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));

        Assert.Equal(AuthService.LockedOutMessage, error.Message);
    }

    [Fact]
    public async Task A_new_sign_in_replaces_the_previous_session()
    {
        var first = await SignInOlena();
        await SignInOlena();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(first.RefreshToken));
    }

    // ---- the admin role ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("admin@example.com")]
    [InlineData("Admin@Example.COM")]
    public async Task An_email_on_the_admin_list_gets_the_admin_role_in_the_first_access_token(string email)
    {
        var result = await _host.SignInAsync(new GoogleIdentity("google-sub-admin", email, true, "Адміністратор"));

        Assert.Contains("admin", result.User.Roles);
        Assert.Contains(AuthService.DefaultRole, result.User.Roles);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
    }

    [Fact]
    public async Task An_email_not_on_the_list_does_not_get_the_admin_role()
    {
        var result = await SignInOlena();

        Assert.DoesNotContain("admin", result.User.Roles);
    }

    [Fact]
    public async Task An_existing_investor_who_is_put_on_the_list_becomes_an_admin_at_the_next_sign_in()
    {
        var old = new User { Id = Guid.NewGuid().ToString(), UserName = AuthTestHost.AdminEmail, Email = AuthTestHost.AdminEmail, FullName = "Адмін" };
        Assert.True((await _host.Users.CreateAsync(old)).Succeeded);
        Assert.True((await _host.Users.AddToRoleAsync(old, AuthService.DefaultRole)).Succeeded);

        var result = await _host.SignInAsync(new GoogleIdentity("google-sub-admin", AuthTestHost.AdminEmail, true, "Адмін"));

        Assert.Equal(old.Id, result.User.Id);
        Assert.Contains("admin", result.User.Roles);
    }

    [Fact]
    public async Task The_admin_role_is_only_ever_granted_never_taken_back()
    {
        var signIn = await SignInOlena();
        var user = (await _host.Users.FindByIdAsync(signIn.User.Id))!;
        await _host.Users.AddToRoleAsync(user, "admin");

        // Olena's email is not on the list (say it was taken off after it granted the role): the role stays.
        var again = await SignInOlena();

        Assert.Contains("admin", again.User.Roles);
    }

    [Fact]
    public async Task Signing_in_twice_as_an_admin_does_not_add_the_role_twice()
    {
        var identity = new GoogleIdentity("google-sub-admin", AuthTestHost.AdminEmail, true, "Адмін");
        await _host.SignInAsync(identity);

        var again = await _host.SignInAsync(identity);

        Assert.Equal(new[] { "admin", AuthService.DefaultRole }, again.User.Roles.OrderBy(role => role));
    }

    // ---- the tokens this API issues ---------------------------------------------------------------------------

    [Fact]
    public async Task The_access_token_names_the_user_and_role_and_expires_on_time()
    {
        var result = await SignInOlena();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(result.User.Id, token.Subject);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == AuthService.DefaultRole);
        Assert.Equal(AuthTestHost.Issuer, token.Issuer);
        Assert.Equal(_host.Clock.Now.UtcDateTime + _host.Settings.AccessTokenLifetime, token.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Refresh_issues_new_tokens_and_the_used_refresh_token_stops_working()
    {
        var signIn = await SignInOlena();

        var refreshed = await _host.Auth.Refresh(signIn.RefreshToken);

        Assert.NotEqual(signIn.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));
        Assert.NotNull(await _host.Auth.Refresh(refreshed.RefreshToken));
    }

    [Fact]
    public async Task Refresh_works_after_the_access_token_has_expired()
    {
        var signIn = await SignInOlena();
        _host.Clock.Advance(TimeSpan.FromHours(2));

        var refreshed = await _host.Auth.Refresh(signIn.RefreshToken);

        Assert.Equal(signIn.User.Id, refreshed.User.Id);
    }

    [Fact]
    public async Task An_expired_refresh_token_is_rejected()
    {
        var signIn = await SignInOlena();
        _host.Clock.Advance(_host.Settings.RefreshTokenLifetime + TimeSpan.FromMinutes(1));

        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));

        Assert.Equal(AuthService.SessionExpiredMessage, error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    public async Task A_missing_or_unknown_refresh_token_is_rejected(string? refreshToken)
    {
        await SignInOlena();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(refreshToken));
    }

    [Fact]
    public async Task Sign_out_revokes_the_refresh_token()
    {
        var signIn = await SignInOlena();

        await _host.Auth.SignOut(signIn.RefreshToken, userId: null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));
    }

    [Fact]
    public async Task Sign_out_with_only_the_user_id_also_revokes_the_session()
    {
        var signIn = await SignInOlena();

        await _host.Auth.SignOut(refreshToken: null, signIn.User.Id);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));
    }

    [Fact]
    public async Task Sign_out_of_a_session_that_is_already_gone_succeeds()
    {
        await _host.Auth.SignOut("not-a-real-token", userId: null);
    }

    [Fact]
    public async Task Signing_in_again_after_signing_out_works()
    {
        var first = await SignInOlena();
        await _host.Auth.SignOut(first.RefreshToken, userId: null);

        var second = await SignInOlena();

        Assert.Equal(first.User.Id, second.User.Id);
        Assert.NotNull(await _host.Auth.Refresh(second.RefreshToken));
    }
}
