using EmailFixer.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailFixer.Infrastructure.Data.Repositories;

/// <summary>
/// Repository для EmailCheck entity
/// </summary>
public interface IEmailCheckRepository : IRepository<EmailCheck>
{
    Task<List<EmailCheck>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<EmailCheck>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<EmailCheck>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<int> GetCheckCountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Реализация EmailCheck repository
/// </summary>
public class EmailCheckRepository : Repository<EmailCheck>, IEmailCheckRepository
{
    public EmailCheckRepository(EmailFixerDbContext context) : base(context)
    {
    }

    public async Task<List<EmailCheck>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CheckedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailCheck>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CheckedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmailCheck>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.BatchId == batchId)
            .OrderByDescending(e => e.CheckedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCheckCountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(e => e.UserId == userId, cancellationToken);
    }
}
