﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Entities.Auth;

namespace PortfolioManager.DAL.Context;

public class PortfolioManagerDbContext: IdentityDbContext<User>
{
    private string _connectionString = string.Empty;
    
    public PortfolioManagerDbContext() { }

    public PortfolioManagerDbContext(DbContextOptions<PortfolioManagerDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _connectionString = configuration.GetConnectionString("PortfolioManagerDb");
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockDataHistory> StockDataHistory { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //This may not work one day
        //optionsBuilder.UseSqlServer(_connectionString);
        optionsBuilder.UseSqlServer("Server=.;Database=PortfolioManagerDb;Trusted_Connection=True;TrustServerCertificate=true");
    }
}