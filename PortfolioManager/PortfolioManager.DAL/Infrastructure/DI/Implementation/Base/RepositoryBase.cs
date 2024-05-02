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
    
    public virtual async Task<TEntity> FindByKeyAsync(TKey key)
    {
        return await Table.FindAsync(key);
    }

    public async Task<TEntity?> CreateAsync(TEntity item)
    {
        var entityEntry = await Table.AddAsync(item);
        await Context.SaveChangesAsync();
        return entityEntry.Entity;
    }

    public async Task<int> UpdateAsync(TEntity item)
    {
        Context.Entry(item).State = EntityState.Modified;
        var result = await Context.SaveChangesAsync();
        return result;
    }

    public async Task<int> DeleteAsync(TEntity item)
    {
        Table.Remove(item);
        var result = await Context.SaveChangesAsync();
        return result;
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