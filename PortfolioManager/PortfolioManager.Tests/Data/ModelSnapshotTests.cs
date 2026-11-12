using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using PortfolioManager.DAL.Context;

namespace PortfolioManager.Tests.Data;

/// <summary>
/// Compares the EF Core model with the committed migrations snapshot, offline: the SQL Server provider
/// is configured but no connection is ever opened.
/// </summary>
public class ModelSnapshotTests
{
    private static PortfolioManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioManagerDbContext>()
            .UseSqlServer("Server=(localdb)\\never-opened;Database=never-opened")
            .Options;
        return new PortfolioManagerDbContext(options, new ConfigurationBuilder().Build());
    }

    [Fact]
    public void Model_matches_the_latest_migration()
    {
        using var context = CreateContext();

        // Same check as `dotnet ef migrations has-pending-model-changes`.
        Assert.False(context.Database.HasPendingModelChanges(),
            "The model has changes that no migration covers. Add a migration, or fix the model.");
    }

    [Fact]
    public void Roles_are_seeded_with_fixed_ids()
    {
        using var first = CreateContext();
        using var second = CreateContext();

        var firstSeed = SeededRoles(first);
        var secondSeed = SeededRoles(second);

        Assert.Equal(
            new[] { (RoleSeed.AdminRoleId, "admin", "ADMIN"), (RoleSeed.InvestorRoleId, "investor", "INVESTOR") },
            firstSeed);
        Assert.Equal(firstSeed, secondSeed);
    }

    private static (string Id, string Name, string NormalizedName)[] SeededRoles(DbContext context) =>
        // Seed data lives only in the design-time model, not in the read-optimised runtime model.
        context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(IdentityRole))!
            .GetSeedData()
            .Select(row => ((string)row["Id"]!, (string)row["Name"]!, (string)row["NormalizedName"]!))
            .OrderBy(row => row.Item1, StringComparer.Ordinal)
            .ToArray();
}
