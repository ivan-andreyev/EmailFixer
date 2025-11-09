using EmailFixer.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailFixer.Infrastructure.Data;

/// <summary>
/// Database context для Email Fixer
/// </summary>
public class EmailFixerDbContext : DbContext
{
    public EmailFixerDbContext(DbContextOptions<EmailFixerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<EmailCheck> EmailChecks { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.Property(e => e.DisplayName)
                .HasMaxLength(255);

            // OAuth configuration
            entity.Property(e => e.GoogleId)
                .HasMaxLength(255);

            entity.Property(e => e.AuthProvider)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("google");

            // Composite unique index for email + provider
            entity.HasIndex(e => new { e.Email, e.AuthProvider })
                .IsUnique()
                .HasName("IX_User_Email_AuthProvider");

            // Unique index for GoogleId when not null
            entity.HasIndex(e => e.GoogleId)
                .IsUnique()
                .HasName("IX_User_GoogleId");

            entity.HasIndex(e => e.StripeCustomerId)
                .IsUnique();

            // Account status
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastLoginAt);
        });

        // EmailCheck configuration
        modelBuilder.Entity<EmailCheck>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(254);

            entity.Property(e => e.Message)
                .HasMaxLength(500);

            entity.Property(e => e.SuggestedEmail)
                .HasMaxLength(254);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.CheckedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.EmailChecks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CreditTransaction configuration
        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.StripePaymentIntentId)
                .HasMaxLength(255);

            entity.Property(e => e.Type)
                .HasConversion<int>();

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.StripePaymentIntentId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
