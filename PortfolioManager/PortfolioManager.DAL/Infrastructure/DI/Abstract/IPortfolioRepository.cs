﻿using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Abstract;

public interface IPortfolioRepository: IRepositoryBase<int, Portfolio>
{
    Task<IEnumerable<Portfolio>> GetAllAsync(string userId);
    Task<Portfolio?> GetAsync(int id);
}