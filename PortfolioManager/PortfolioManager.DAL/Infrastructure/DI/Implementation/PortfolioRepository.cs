﻿using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Context;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation;

public class PortfolioRepository(PortfolioManagerDbContext context)
    : RepositoryBase<int, Portfolio>(context), IPortfolioRepository
{
    public async Task<IEnumerable<Portfolio>> GetAllAsync()
    {
        return await Table.Select(item => item).ToListAsync();
    }

    public async Task<Portfolio?> GetAsync(int id)
    {
        return await Table.Where(item => item.Id == id).Include(item => item.Stocks)
            .SingleOrDefaultAsync();
    }
}