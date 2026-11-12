using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Services.Auth;
using PortfolioManager.Common.DTO.Auth;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.Tests.Auth;

/// <summary>A clock the tests can move forward, to expire tokens without waiting.</summary>
public class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>
/// The real AuthService, TokenService and ASP.NET Identity, configured as in Program.cs, over an in-memory database
/// seeded with the investor and admin roles. Only Google is replaced: <see cref="Google"/> hands out the credentials
/// that <see cref="SignInAsync"/> then signs in with.
/// </summary>
public sealed class AuthTestHost : IDisposable
{
    public const string Issuer = "portfolio-manager-tests";
    public const string Secret = "test-signing-key-that-is-at-least-32-bytes-long";
    public const string ClientId = "test-client.apps.googleusercontent.com";
    public const string AdminEmail = "admin@example.com";

    /// <summary>A person with a Google account, as the validator reports them.</summary>
    public static GoogleIdentity Olena { get; } = new("google-sub-olena", "olena@example.com", true, "Олена Коваль");

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public TestClock Clock { get; } = new();
    public JwtSettings Settings { get; } = new() { Issuer = Issuer, Secret = Secret };
    public FakeGoogleTokenValidator Google { get; } = new();

    public AuthTestHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbContext<PortfolioManagerDbContext>(options =>
            options.UseInMemoryDatabase("auth-tests-" + Guid.NewGuid()));

        services.AddIdentity<User, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty;
            })
            .AddEntityFrameworkStores<PortfolioManagerDbContext>();

        services.AddSingleton(Options.Create(Settings));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(new GoogleAuthSettings
        {
            ClientId = ClientId,
            AdminEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { AdminEmail }
        });
        services.AddSingleton<IGoogleTokenValidator>(Google);
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        Db.Database.EnsureCreated();
    }

    public IAuthService Auth => _scope.ServiceProvider.GetRequiredService<IAuthService>();
    public UserManager<User> Users => _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    public PortfolioManagerDbContext Db => _scope.ServiceProvider.GetRequiredService<PortfolioManagerDbContext>();

    /// <summary>Signs this person in with a credential Google "just issued" for them.</summary>
    public Task<AuthResultDTO> SignInAsync(GoogleIdentity identity) =>
        Auth.SignInWithGoogle(new GoogleSignInDTO { Credential = Google.Credential(identity) });

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
