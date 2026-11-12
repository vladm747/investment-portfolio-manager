using Microsoft.EntityFrameworkCore;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;

namespace PortfolioManager.DAL.Infrastructure.DI.Implementation.Base;

public abstract class RepositoryBase<TKey, TEntity> : IRepositoryBase<TKey, TEntity> 
    where TEntity: class
    where TKey : IEquatable<TKey>
{
    private readonly bool _disposeContext;
    private bool _isDisposed;
    private DbContext Context { get; set; }
    protected DbSet<TEntity> Table { get; }

    protected RepositoryBase(DbContext dbContext)
    {
        Context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Table = Context.Set<TEntity>();
        _disposeContext = false;
    }
    
    public virtual async Task<TEntity?> FindByKeyAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return await Table.FindAsync(new object?[] { key }, cancellationToken);
    }

    public async Task<TEntity> CreateAsync(TEntity item, CancellationToken cancellationToken = default)
    {
        var entityEntry = await Table.AddAsync(item, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entityEntry.Entity;
    }

    public async Task<int> UpdateAsync(TEntity item, CancellationToken cancellationToken = default)
    {
        Context.Entry(item).State = EntityState.Modified;
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> UpdateRangeAsync(IReadOnlyCollection<TEntity> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
            Context.Entry(item).State = EntityState.Modified;

        // One SaveChanges is one transaction: either every item is saved or none is.
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(TEntity item, CancellationToken cancellationToken = default)
    {
        Table.Remove(item);
        return await Context.SaveChangesAsync(cancellationToken);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing && _disposeContext)
            Context.Dispose();

        _isDisposed = true;
    }

    ~RepositoryBase()
    {
        Dispose(true);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}