namespace PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

public interface IRepositoryBase<TKey, TEntity>
{
    Task<TEntity?> FindByKeyAsync(TKey key);
    Task<TEntity?> CreateAsync(TEntity entity);
    Task<int> UpdateAsync(TEntity entity);
    Task<int> DeleteAsync(TEntity entity);
}