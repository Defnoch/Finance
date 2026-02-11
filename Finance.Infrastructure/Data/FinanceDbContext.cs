using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Data;

public sealed class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();
    public DbSet<RuleCondition> RuleConditions => Set<RuleCondition>();
    public DbSet<BackgroundTaskConfig> BackgroundTaskConfigs => Set<BackgroundTaskConfig>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<TransactionLink> TransactionLinks => Set<TransactionLink>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AccountFiscalYear> AccountFiscalYears => Set<AccountFiscalYear>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(b =>
        {
            b.HasKey(t => t.TransactionId);

            b.Property(t => t.SourceSystem)
                .HasMaxLength(50)
                .IsRequired();

            b.Property(t => t.SourceReference)
                .HasMaxLength(200)
                .IsRequired();

            b.Property(t => t.Currency)
                .HasMaxLength(10)
                .IsRequired();

            b.Property(t => t.AccountIdentifier)
                .HasMaxLength(100)
                .IsRequired();

            b.Property(t => t.CounterpartyIdentifier)
                .HasMaxLength(200);

            b.Property(t => t.Description)
                .HasMaxLength(1024);

            b.Property(t => t.ResultingBalance);

            b.Property(t => t.TransactionType)
                .HasMaxLength(100);

            b.Property(t => t.Notifications)
                .HasMaxLength(1024);

            b.Property(t => t.Name)
                .HasMaxLength(200);

            b.Property(t => t.AccountId);
            b.Property(t => t.CounterpartyAccountId);
            b.HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Account>()
                .WithMany()
                .HasForeignKey(t => t.CounterpartyAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(t => t.BookingDate);
            b.HasIndex(t => t.CategoryId);

            b.HasIndex(t => new { t.SourceSystem, t.SourceReference })
                .IsUnique();
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.HasKey(c => c.CategoryId);

            b.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();

            b.Property(c => c.Kind)
                .IsRequired();

            b.Property(c => c.ColorHex)
                .HasMaxLength(7);

            b.HasIndex(c => c.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ImportBatch>(b =>
        {
            b.HasKey(i => i.ImportBatchId);

            b.Property(i => i.SourceSystem)
                .HasMaxLength(50)
                .IsRequired();

            b.Property(i => i.FileName)
                .HasMaxLength(260)
                .IsRequired();

            b.Property(i => i.ErrorMessage)
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<CategorizationRule>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).HasMaxLength(200).IsRequired();
            b.Property(r => r.Priority).IsRequired();
            b.Property(r => r.IsEnabled).IsRequired();
            b.Property(r => r.CategoryId).IsRequired();
            b.HasMany(r => r.Conditions).WithOne().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleCondition>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Field).HasMaxLength(100).IsRequired();
            b.Property(c => c.Operator).HasMaxLength(50).IsRequired();
            b.Property(c => c.Value).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<BackgroundTaskConfig>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Id)
                .HasConversion(
                    v => v.ToString(), // Guid -> string
                    v => Guid.Parse(v)) // string -> Guid
                .HasColumnType("TEXT");
            b.Property(t => t.Name).HasMaxLength(100).IsRequired();
            b.Property(t => t.Description).HasMaxLength(500);
            b.Property(t => t.IntervalMinutes).IsRequired();
            b.Property(t => t.IsEnabled).IsRequired();
            b.Property(t => t.LastRunAt);
        });

        modelBuilder.Entity<TransactionLink>(b =>
        {
            b.HasKey(tl => tl.TransactionLinkId);
            b.Property(tl => tl.TransactionId1).IsRequired();
            b.Property(tl => tl.TransactionId2).IsRequired();
            b.Property(tl => tl.LinkedAt).IsRequired();
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.UserId);
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.UserId).IsUnique();
            b.HasIndex(u => u.CreatedAt);
            b.Property(u => u.Username).HasMaxLength(50).IsRequired();
            b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            b.Property(u => u.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<AccountFiscalYear>(b =>
        {
            b.HasKey(x => new { x.AccountId, x.Year });
            b.HasIndex(x => new { x.AccountId, x.Year }).IsUnique();
            b.Property(x => x.AccountId).IsRequired();
            b.Property(x => x.Year).IsRequired();
            b.HasOne<Account>()
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
