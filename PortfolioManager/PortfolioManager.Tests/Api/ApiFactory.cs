using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities.Auth;
using PortfolioManager.MathModule.Client;
using PortfolioManager.Tests.Services;

namespace PortfolioManager.Tests.Api;

/// <summary>A user created through the real sign-up endpoint, with the access token the API gave them.</summary>
public sealed record TestUser(string Id, string Email, string Token);

/// <summary>
/// The real API, started from its own Program: routing, [ApiController] validation, JWT authentication, authorization,
/// the exception filter, the services and the repositories. Only the two things that leave the process are replaced:
/// SQL Server by EF Core's in-memory database, and the Python math service by <see cref="FakeMathModule"/>.
/// Nothing touches a network or a real database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Secret#1";

    /// <summary>Production, so the tests see the responses a user sees: no stack traces, no internal messages, no Swagger.</summary>
    protected virtual string EnvironmentName => "Production";

    private readonly string _database = "api-tests-" + Guid.NewGuid();

    public FakeMathModule MathModule { get; } = new();

    static ApiFactory()
    {
        // Program reads these while it builds. Neither value is ever used: the database and the math module
        // are replaced in ConfigureWebHost.
        Environment.SetEnvironmentVariable("ConnectionStrings__PortfolioManagerDb", "Server=(localdb)\\never-opened;Database=never-opened");
        Environment.SetEnvironmentVariable("MathModule__BaseUrl", "http://math.invalid");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "portfolio-manager-api-tests");
        Environment.SetEnvironmentVariable("JWT_SECRET", "api-test-signing-key-that-is-at-least-32-bytes");
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

    /// <summary>A new investor, created the way a visitor creates one: through <c>POST api/auth/signup</c>.</summary>
    public async Task<TestUser> SignUpAsync(string fullName = "Олена Коваль")
    {
        EnsureSchema();
        var email = $"{Guid.NewGuid():N}@example.com";
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/signup", new { email, password = Password, fullName });
        response.EnsureSuccessStatusCode();

        var result = (await response.Content.ReadFromJsonAsync<AuthResultDTO>())!;
        return new TestUser(result.User.Id, email, result.AccessToken);
    }

    /// <summary>A new user who has been given the admin role, signed in again so the token carries it.</summary>
    public async Task<TestUser> SignUpAdminAsync()
    {
        var user = await SignUpAsync("Адміністратор");

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var stored = (await users.FindByIdAsync(user.Id))!;
            var added = await users.AddToRoleAsync(stored, "admin");
            Assert.True(added.Succeeded);
        }

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/signin", new { email = user.Email, password = Password });
        response.EnsureSuccessStatusCode();

        var result = (await response.Content.ReadFromJsonAsync<AuthResultDTO>())!;
        Assert.Contains("admin", result.User.Roles);
        return user with { Token = result.AccessToken };
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
