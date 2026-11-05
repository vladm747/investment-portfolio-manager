using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PortfolioManager.BLL.Helpers;
using PortfolioManager.BLL.Interfaces.Auth;
using PortfolioManager.BLL.Services.Auth;
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
/// The real AuthService, TokenService and ASP.NET Identity, configured as in Program.cs,
/// over an in-memory database seeded with the investor and admin roles.
/// </summary>
public sealed class AuthTestHost : IDisposable
{
    public const string Issuer = "portfolio-manager-tests";
    public const string Secret = "test-signing-key-that-is-at-least-32-bytes-long";

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public TestClock Clock { get; } = new();
    public JwtSettings Settings { get; } = new() { Issuer = Issuer, Secret = Secret };

    public AuthTestHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbContext<PortfolioManagerDbContext>(options =>
            options.UseInMemoryDatabase("auth-tests-" + Guid.NewGuid()));

        services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<PortfolioManagerDbContext>()
            .AddErrorDescriber<UkrainianIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        services.AddSingleton(Options.Create(Settings));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        Db.Database.EnsureCreated();
    }

    public IAuthService Auth => _scope.ServiceProvider.GetRequiredService<IAuthService>();
    public UserManager<User> Users => _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    public PortfolioManagerDbContext Db => _scope.ServiceProvider.GetRequiredService<PortfolioManagerDbContext>();

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
