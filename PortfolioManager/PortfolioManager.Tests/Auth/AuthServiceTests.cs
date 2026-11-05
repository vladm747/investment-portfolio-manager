using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PortfolioManager.BLL.Exceptions;
using PortfolioManager.BLL.Services.Auth;
using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private const string Email = "olena@example.com";
    private const string Password = "Secret#1";

    private readonly AuthTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private Task<AuthResultDTO> SignUpOlena() =>
        _host.Auth.SignUp(new SignUpDTO { Email = Email, Password = Password, FullName = "Олена Коваль" });

    [Fact]
    public async Task Sign_up_creates_an_investor_signs_them_in_and_keeps_the_name()
    {
        var result = await SignUpOlena();

        Assert.Equal("Олена Коваль", result.User.FullName);
        Assert.Equal(Email, result.User.Email);
        Assert.Equal(new[] { AuthService.DefaultRole }, result.User.Roles);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task Only_a_hash_of_the_refresh_token_is_stored()
    {
        var result = await SignUpOlena();

        var user = await _host.Users.FindByNameAsync(Email);
        Assert.NotNull(user);
        Assert.NotEqual(result.RefreshToken, user!.RefreshToken);
        Assert.False(string.IsNullOrEmpty(user.RefreshToken));
    }

    [Fact]
    public async Task Sign_up_with_a_taken_email_is_rejected_in_Ukrainian()
    {
        await SignUpOlena();

        var error = await Assert.ThrowsAsync<ValidationFailedException>(SignUpOlena);

        Assert.Contains("вже зареєстрований", error.Message);
    }

    [Fact]
    public async Task Sign_up_with_a_weak_password_lists_every_broken_rule()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _host.Auth.SignUp(new SignUpDTO { Email = Email, Password = "abc", FullName = "Олена Коваль" }));

        // Too short, no digit, no capital letter, no special character.
        Assert.Equal(4, error.Errors.Count);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_answer()
    {
        await SignUpOlena();

        var wrongPassword = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _host.Auth.SignIn(new SignInDTO { Email = Email, Password = "Wrong#1" }));
        var unknownEmail = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _host.Auth.SignIn(new SignInDTO { Email = "nobody@example.com", Password = Password }));

        Assert.Equal(AuthService.InvalidCredentialsMessage, wrongPassword.Message);
        Assert.Equal(AuthService.InvalidCredentialsMessage, unknownEmail.Message);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        await SignUpOlena();
        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
                _host.Auth.SignIn(new SignInDTO { Email = Email, Password = "Wrong#1" }));
        }

        var error = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _host.Auth.SignIn(new SignInDTO { Email = Email, Password = Password }));

        Assert.Equal(AuthService.LockedOutMessage, error.Message);
    }

    [Fact]
    public async Task The_access_token_names_the_user_and_role_and_expires_on_time()
    {
        var result = await SignUpOlena();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(result.User.Id, token.Subject);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == AuthService.DefaultRole);
        Assert.Equal(AuthTestHost.Issuer, token.Issuer);
        Assert.Equal(_host.Clock.Now.UtcDateTime + _host.Settings.AccessTokenLifetime, token.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Refresh_issues_new_tokens_and_the_used_refresh_token_stops_working()
    {
        var signIn = await SignUpOlena();

        var refreshed = await _host.Auth.Refresh(signIn.RefreshToken);

        Assert.NotEqual(signIn.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));
        Assert.NotNull(await _host.Auth.Refresh(refreshed.RefreshToken));
    }

    [Fact]
    public async Task Refresh_works_after_the_access_token_has_expired()
    {
        var signIn = await SignUpOlena();
        _host.Clock.Advance(TimeSpan.FromHours(2));

        var refreshed = await _host.Auth.Refresh(signIn.RefreshToken);

        Assert.Equal(signIn.User.Id, refreshed.User.Id);
    }

    [Fact]
    public async Task An_expired_refresh_token_is_rejected()
    {
        var signIn = await SignUpOlena();
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
        await SignUpOlena();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(refreshToken));
    }

    [Fact]
    public async Task Sign_out_revokes_the_refresh_token()
    {
        var signIn = await SignUpOlena();

        await _host.Auth.SignOut(signIn.RefreshToken, userId: null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));
    }

    [Fact]
    public async Task Sign_out_with_only_the_user_id_also_revokes_the_session()
    {
        var signIn = await SignUpOlena();

        await _host.Auth.SignOut(refreshToken: null, signIn.User.Id);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _host.Auth.Refresh(signIn.RefreshToken));
    }

    [Fact]
    public async Task Sign_out_of_a_session_that_is_already_gone_succeeds()
    {
        await _host.Auth.SignOut("not-a-real-token", userId: null);
    }
}
