using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities.Auth;
using PortfolioManager.MathModule.Client;
using PortfolioManager.Tests.Auth;
using PortfolioManager.Tests.Services;

namespace PortfolioManager.Tests.Api;

/// <summary>A user who signed in through the real <c>POST api/auth/google</c>, with the access token the API gave them.</summary>
public sealed record TestUser(string Id, string Email, string Token);

/// <summary>
/// The real API, started from its own Program: routing, [ApiController] validation, JWT authentication, authorization,
/// the exception filter, the services and the repositories. Only the three things that leave the process are replaced:
/// SQL Server by EF Core's in-memory database, the Python math service by <see cref="FakeMathModule"/>, and Google's
/// token check by <see cref="FakeGoogleTokenValidator"/>. Nothing touches a network or a real database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The one e-mail on the host's <c>Auth:AdminEmails</c> list: whoever signs in with it becomes an admin.</summary>
    public const string AdminEmail = "admin@example.com";

    /// <summary>Production, so the tests see the responses a user sees: no stack traces, no internal messages, no Swagger.</summary>
    protected virtual string EnvironmentName => "Production";

    private readonly string _database = "api-tests-" + Guid.NewGuid();

    public FakeMathModule MathModule { get; } = new();

    public FakeGoogleTokenValidator Google { get; } = new();

    static ApiFactory()
    {
        // Program reads these while it builds. The first two values are never used: the database and the math module
        // are replaced in ConfigureWebHost.
        Environment.SetEnvironmentVariable("ConnectionStrings__PortfolioManagerDb", "Server=(localdb)\\never-opened;Database=never-opened");
        Environment.SetEnvironmentVariable("MathModule__BaseUrl", "http://math.invalid");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "portfolio-manager-api-tests");
        Environment.SetEnvironmentVariable("JWT_SECRET", "api-test-signing-key-that-is-at-least-32-bytes");
        // Required at startup. The client ID only matters to the real validator, which the fake replaces.
        Environment.SetEnvironmentVariable("Authentication__Google__ClientId", "api-tests.apps.googleusercontent.com");
        Environment.SetEnvironmentVariable("Auth__AdminEmails__0", AdminEmail);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PortfolioManagerDbContext>>();
            services.AddDbContext<PortfolioManagerDbContext>(options => options.UseInMemoryDatabase(_database));

            services.RemoveAll<IMathModuleClient>();
            services.AddSingleton<IMathModuleClient>(MathModule);

            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator>(Google);
        });
    }

    private readonly object _schemaLock = new();
    private bool _schemaCreated;

    /// <summary>Creates the schema and seeds the investor and admin roles, as the migrations do on SQL Server.</summary>
    private void EnsureSchema()
    {
        lock (_schemaLock)
        {
            if (_schemaCreated)
                return;

            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PortfolioManagerDbContext>().Database.EnsureCreated();
            _schemaCreated = true;
        }
    }

    /// <summary>A person with a Google account nobody has used before, as Google's token would describe them.</summary>
    public static GoogleIdentity NewGoogleIdentity(string fullName = "Олена Коваль") =>
        new("google-sub-" + Guid.NewGuid().ToString("N"), $"{Guid.NewGuid():N}@example.com", true, fullName);

    /// <summary>Gives the person a credential Google "issued" for them and posts it to <c>POST api/auth/google</c>.</summary>
    public async Task<HttpResponseMessage> PostGoogleSignInAsync(GoogleIdentity identity)
    {
        EnsureSchema();
        using var client = CreateClient();
        return await client.PostAsJsonAsync("api/auth/google", new { credential = Google.Credential(identity) });
    }

    /// <summary>Signs the person in through the real endpoint. The first sign-in creates their investor account.</summary>
    public async Task<AuthResultDTO> SignInAsync(GoogleIdentity identity)
    {
        using var response = await PostGoogleSignInAsync(identity);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthResultDTO>())!;
    }

    /// <summary>A new investor, created the way a visitor becomes one: by signing in with Google for the first time.</summary>
    public async Task<TestUser> SignUpAsync(string fullName = "Олена Коваль")
    {
        var identity = NewGoogleIdentity(fullName);
        var result = await SignInAsync(identity);

        return new TestUser(result.User.Id, identity.Email!, result.AccessToken);
    }

    /// <summary>A new user who has been given the admin role, signed in again so the token carries it.</summary>
    public async Task<TestUser> SignUpAdminAsync()
    {
        var identity = NewGoogleIdentity("Адміністратор");
        var first = await SignInAsync(identity);

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var stored = (await users.FindByIdAsync(first.User.Id))!;
            var added = await users.AddToRoleAsync(stored, "admin");
            Assert.True(added.Succeeded);
        }

        var again = await SignInAsync(identity); // Google issues a new credential for the same account
        Assert.Contains("admin", again.User.Roles);
        return new TestUser(again.User.Id, identity.Email!, again.AccessToken);
    }

    /// <summary>A client that sends the user's access token, or none for an anonymous visitor.</summary>
    public HttpClient ClientFor(TestUser? user)
    {
        var client = CreateClient();
        if (user is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }
}

/// <summary>The same host started as Development, the only environment in which Swagger exists.</summary>
public sealed class DevelopmentApiFactory : ApiFactory
{
    protected override string EnvironmentName => "Development";
}
