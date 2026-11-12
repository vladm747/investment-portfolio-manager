namespace PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

public interface IRepositoryBase<TKey, TEntity>
{
    Task<TEntity?> FindByKeyAsync(TKey key, CancellationToken cancellationToken = default);
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Saves all the entities in one round trip to the database, so they change together or not at all.</summary>
    Task<int> UpdateRangeAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
