using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.DAL.Context;

public class PortfolioManagerDbContext: IdentityDbContext<User>
{
    private readonly string _connectionString = string.Empty;

    public PortfolioManagerDbContext() { }

    public PortfolioManagerDbContext(DbContextOptions<PortfolioManagerDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _connectionString = configuration.GetConnectionString("PortfolioManagerDb") ?? string.Empty;
    }

    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public virtual DbSet<StockSymbol> StockSymbols { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fixed ids: random ones would make every new migration delete and re-insert the roles,
        // wiping all users' role assignments. See RoleSeed.
        modelBuilder.Entity<IdentityRole>().HasData(RoleSeed.Roles());

        modelBuilder.Entity<Portfolio>()
            .HasMany(p => p.Stocks)
            .WithOne(s => s.Portfolio)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only when nothing configured the context already (design-time tools); the API passes options through DI,
        // and the tests use an in-memory provider that must not be overridden here.
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_connectionString);
    }
}
