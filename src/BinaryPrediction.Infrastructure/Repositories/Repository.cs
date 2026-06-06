using BinaryPrediction.Core.Entities.Common;
using BinaryPrediction.Core.Repositories;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BinaryPrediction.Infrastructure.Repositories;

public class Repository<T> : IRepository<T>
    where T : BaseEntity
{
    private readonly BinaryPredictionDbContext _dbContext;

    public Repository(BinaryPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<T>().Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<T>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
