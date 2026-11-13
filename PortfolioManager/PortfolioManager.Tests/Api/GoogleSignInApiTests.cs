using System.Net;
using System.Net.Http.Json;
using System.Text;
using PortfolioManager.BLL.Services.Auth;
using PortfolioManager.Common.DTO.Auth;

namespace PortfolioManager.Tests.Api;

/// <summary><c>POST api/auth/google</c> as the browser calls it: the one way to get a token.</summary>
public class GoogleSignInApiTests : ApiTestBase
{
    public GoogleSignInApiTests(ApiFactory api) : base(api) { }

    [Fact]
    public async Task A_valid_Google_token_signs_in_and_the_first_sign_in_creates_an_investor()
    {
        var identity = ApiFactory.NewGoogleIdentity("Олена Коваль");

        using var response = await Api.PostGoogleSignInAsync(identity);

        var result = await ReadAsync<AuthResultDTO>(response);
        Assert.Equal(identity.Email, result.User.Email);
        Assert.Equal("Олена Коваль", result.User.FullName);
        Assert.Equal(new[] { "investor" }, result.User.Roles);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task The_access_token_it_gives_works_on_the_api()
    {
        var user = await Api.SignUpAsync("Олена Коваль");
        using var client = Api.ClientFor(user);

        var me = await ReadAsync<UserDTO>(await client.GetAsync("api/user/me"));

        Assert.Equal(user.Email, me.Email);
        Assert.Equal(user.Id, me.Id);
    }

    [Fact]
    public async Task The_refresh_token_is_also_set_as_an_http_only_cookie()
    {
        using var response = await Api.PostGoogleSignInAsync(ApiFactory.NewGoogleIdentity());

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("pm_refresh_token="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_same_person_signing_in_again_is_the_same_user_and_the_session_can_be_refreshed()
    {
        var identity = ApiFactory.NewGoogleIdentity();
        var first = await Api.SignInAsync(identity);
        var second = await Api.SignInAsync(identity);
        using var client = Api.ClientFor(null);

        var refreshed = await ReadAsync<AuthResultDTO>(
            await client.PostAsJsonAsync("api/auth/refresh-token", new { refreshToken = second.RefreshToken }));

        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Equal(first.User.Id, refreshed.User.Id);
    }

    [Fact]
    public async Task A_credential_Google_did_not_issue_is_401_in_Ukrainian()
    {
        using var client = Api.ClientFor(null);

        using var response = await client.PostAsJsonAsync("api/auth/google", new { credential = "tampered.id.token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(AuthService.InvalidGoogleTokenMessage, (await ReadErrorAsync(response)).Message);
    }

    [Fact]
    public async Task A_token_with_an_unverified_email_is_401()
    {
        using var response = await Api.PostGoogleSignInAsync(ApiFactory.NewGoogleIdentity() with { EmailVerified = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(AuthService.UnverifiedEmailMessage, (await ReadErrorAsync(response)).Message);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"credential\":\"\"}")]
    [InlineData("{\"credential\":null}")]
    public async Task A_request_without_a_credential_is_400(string body)
    {
        using var client = Api.ClientFor(null);

        using var response = await client.PostAsync("api/auth/google", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_absurdly_long_credential_is_400_before_it_reaches_the_validator()
    {
        using var client = Api.ClientFor(null);

        using var response = await client.PostAsJsonAsync("api/auth/google",
            new { credential = new string('x', GoogleSignInDTO.MaxCredentialLength + 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_request_cannot_say_who_the_user_is_or_what_role_they_get()
    {
        var identity = ApiFactory.NewGoogleIdentity();
        using var client = Api.ClientFor(null);

        // Extra fields are ignored: the name, e-mail and role come only from Google's validated token.
        var result = await ReadAsync<AuthResultDTO>(await client.PostAsJsonAsync("api/auth/google",
            new { credential = Api.Google.Credential(identity), email = ApiFactory.AdminEmail, roles = new[] { "admin" }, fullName = "Хакер" }));

        Assert.Equal(identity.Email, result.User.Email);
        Assert.Equal(new[] { "investor" }, result.User.Roles);
        Assert.NotEqual("Хакер", result.User.FullName);
    }

    [Fact]
    public async Task An_email_on_the_admin_list_gets_the_admin_role_through_the_real_configuration()
    {
        var admin = ApiFactory.NewGoogleIdentity("Адміністратор") with { Email = ApiFactory.AdminEmail };

        var result = await Api.SignInAsync(admin);

        Assert.Contains("admin", result.User.Roles);
        Assert.Contains("investor", result.User.Roles);
    }
}
