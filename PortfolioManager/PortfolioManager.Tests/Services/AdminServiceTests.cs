using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortfolioManager.BLL.Services;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Entities.Auth;
using PortfolioManager.DAL.Infrastructure.DI.Implementation;
using PortfolioManager.Tests.Auth;

namespace PortfolioManager.Tests.Services;

/// <summary>The dashboard's numbers, from the real repository over EF Core's in-memory database and a clock the tests move.</summary>
public class AdminServiceTests : IDisposable
{
    private readonly TestClock _clock = new();
    private readonly PortfolioManagerDbContext _db;

    public AdminServiceTests()
    {
        _db = new PortfolioManagerDbContext(
            new DbContextOptionsBuilder<PortfolioManagerDbContext>().UseInMemoryDatabase("admin-tests-" + Guid.NewGuid()).Options,
            new ConfigurationBuilder().Build());
        _db.Database.EnsureCreated(); // seeds the investor and admin roles, as the migrations do
    }

    public void Dispose() => _db.Dispose();

    private AdminService Service() => new(new UserRepository(_db), _clock);

    private User AddUser(string name, DateTime? lastActive = null, bool admin = false, int portfolios = 0, bool investor = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = name + "@example.com",
            Email = name + "@example.com",
            FullName = name,
            // Never signed in: the column keeps the default, as it does for an account whose tokens were never issued.
            TokenCreated = lastActive ?? default,
            // These must never leave the API.
            PasswordHash = "secret-hash",
            RefreshToken = "secret-refresh-token"
        };
        _db.Users.Add(user);
        if (investor)
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = RoleSeed.InvestorRoleId });
        if (admin)
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = RoleSeed.AdminRoleId });
        for (var i = 0; i < portfolios; i++)
            _db.Portfolios.Add(new Portfolio { Name = $"{name} {i}", UserId = user.Id });
        _db.SaveChanges();
        return user;
    }

    private DateTime Ago(TimeSpan span) => _clock.Now.UtcDateTime - span;

    [Fact]
    public async Task With_nobody_signed_up_everything_is_zero()
    {
        var result = await Service().GetUsersAsync();

        Assert.Empty(result.Users);
        Assert.Equal(0, result.Stats.TotalUsers);
        Assert.Equal(0, result.Stats.Admins);
        Assert.Equal(0, result.Stats.ActiveLast7Days);
        Assert.Equal(0, result.Stats.TotalPortfolios);
    }

    [Fact]
    public async Task The_cards_count_users_admins_and_portfolios()
    {
        AddUser("olena", Ago(TimeSpan.FromHours(1)), portfolios: 2);
        AddUser("ivan", Ago(TimeSpan.FromHours(1)), portfolios: 1);
        AddUser("admin", Ago(TimeSpan.FromHours(1)), admin: true);

        var stats = (await Service().GetUsersAsync()).Stats;

        Assert.Equal(3, stats.TotalUsers);
        Assert.Equal(1, stats.Admins);
        Assert.Equal(3, stats.TotalPortfolios);
    }

    [Fact]
    public async Task An_admin_with_both_roles_is_counted_once()
    {
        AddUser("admin", Ago(TimeSpan.FromHours(1)), admin: true, investor: true);

        var stats = (await Service().GetUsersAsync()).Stats;

        Assert.Equal(1, stats.Admins);
        Assert.Equal(1, stats.TotalUsers);
    }

    [Fact]
    public async Task Active_means_tokens_issued_within_the_last_seven_days()
    {
        AddUser("today", Ago(TimeSpan.FromHours(3)));
        AddUser("six-days", Ago(TimeSpan.FromDays(6)));
        AddUser("exactly-seven-days", Ago(TimeSpan.FromDays(7)));
        AddUser("just-over-seven-days", Ago(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1)));
        AddUser("last-month", Ago(TimeSpan.FromDays(30)));

        var stats = (await Service().GetUsersAsync()).Stats;

        Assert.Equal(5, stats.TotalUsers);
        Assert.Equal(3, stats.ActiveLast7Days);
    }

    [Fact]
    public async Task The_window_moves_with_the_clock()
    {
        AddUser("olena", Ago(TimeSpan.FromDays(6)));
        Assert.Equal(1, (await Service().GetUsersAsync()).Stats.ActiveLast7Days);

        _clock.Advance(TimeSpan.FromDays(2));

        Assert.Equal(0, (await Service().GetUsersAsync()).Stats.ActiveLast7Days);
    }

    [Fact]
    public async Task A_user_who_never_signed_in_has_no_last_active_date_and_is_not_active()
    {
        AddUser("never");

        var result = await Service().GetUsersAsync();

        Assert.Null(Assert.Single(result.Users).LastActiveAt);
        Assert.Equal(0, result.Stats.ActiveLast7Days);
        Assert.Equal(1, result.Stats.TotalUsers);
    }

    [Fact]
    public async Task A_user_with_no_portfolios_has_a_count_of_zero()
    {
        AddUser("olena", Ago(TimeSpan.FromHours(1)), portfolios: 3);
        AddUser("ivan", Ago(TimeSpan.FromHours(1)));

        var users = (await Service().GetUsersAsync()).Users;

        Assert.Equal(3, users.Single(user => user.FullName == "olena").PortfolioCount);
        Assert.Equal(0, users.Single(user => user.FullName == "ivan").PortfolioCount);
    }

    [Fact]
    public async Task Each_row_carries_the_profile_roles_and_a_utc_last_active_time()
    {
        var signedIn = Ago(TimeSpan.FromHours(5));
        var user = AddUser("admin", signedIn, admin: true, portfolios: 1);

        var row = Assert.Single((await Service().GetUsersAsync()).Users);

        Assert.Equal(user.Id, row.Id);
        Assert.Equal("admin", row.FullName);
        Assert.Equal("admin@example.com", row.Email);
        Assert.Equal(new[] { "admin", "investor" }, row.Roles);
        Assert.Equal(1, row.PortfolioCount);
        Assert.Equal(signedIn, row.LastActiveAt);
        Assert.Equal(DateTimeKind.Utc, row.LastActiveAt!.Value.Kind);
    }

    [Fact]
    public async Task A_user_without_any_role_is_listed_with_no_roles()
    {
        AddUser("nobody", Ago(TimeSpan.FromHours(1)), investor: false);

        var row = Assert.Single((await Service().GetUsersAsync()).Users);

        Assert.Empty(row.Roles);
    }

    [Fact]
    public async Task The_most_recently_active_user_comes_first_and_those_who_never_signed_in_come_last()
    {
        AddUser("never");
        AddUser("old", Ago(TimeSpan.FromDays(20)));
        AddUser("new", Ago(TimeSpan.FromMinutes(5)));

        var names = (await Service().GetUsersAsync()).Users.Select(user => user.FullName);

        Assert.Equal(new[] { "new", "old", "never" }, names);
    }

    [Fact]
    public async Task The_answer_has_nothing_but_profile_and_counts()
    {
        AddUser("olena", Ago(TimeSpan.FromHours(1)), portfolios: 1);

        var json = System.Text.Json.JsonSerializer.Serialize(await Service().GetUsersAsync());

        Assert.DoesNotContain("secret-hash", json);
        Assert.DoesNotContain("secret-refresh-token", json);
        Assert.Equal(
            new[] { "Email", "FullName", "Id", "LastActiveAt", "PortfolioCount", "Roles" },
            System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("Users")[0]
                .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
    }
}
