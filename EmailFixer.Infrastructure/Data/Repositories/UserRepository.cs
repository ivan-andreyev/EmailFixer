using EmailFixer.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailFixer.Infrastructure.Data.Repositories;

/// <summary>
/// Repository для User entity
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByStripeIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAndProviderAsync(string email, string authProvider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Реализация User repository
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(EmailFixerDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            u => u.Email == email,
            cancellationToken);
    }

    public async Task<User?> GetByStripeIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            u => u.StripeCustomerId == stripeCustomerId,
            cancellationToken);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            u => u.GoogleId == googleId,
            cancellationToken);
    }

    public async Task<User?> GetByEmailAndProviderAsync(string email, string authProvider, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            u => u.Email == email && u.AuthProvider == authProvider,
            cancellationToken);
    }
}
