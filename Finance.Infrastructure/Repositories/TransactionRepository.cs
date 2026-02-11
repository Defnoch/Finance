using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TransactionFilter = Finance.Domain.Repositories.TransactionFilter;

namespace Finance.Infrastructure.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly FinanceDbContext _dbContext;

    public TransactionRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddRangeAsync(transactions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsBySourceReferenceAsync(string sourceSystem, string sourceReference, CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions.AnyAsync(
            t => t.SourceSystem == sourceSystem && t.SourceReference == sourceReference,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByFilterAsync(TransactionFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> query = _dbContext.Transactions;

        if (filter.FromDate is { } from)
            query = query.Where(t => t.BookingDate >= from);
        if (filter.ToDate is { } to)
            query = query.Where(t => t.BookingDate <= to);
        if (filter.CategoryId is { } categoryId)
            query = query.Where(t => t.CategoryId == categoryId);
        if (filter.MinAmount is { } min)
            query = query.Where(t => t.Amount >= min);
        if (filter.MaxAmount is { } max)
            query = query.Where(t => t.Amount <= max);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var text = filter.SearchText.Trim();
            query = query.Where(t => t.Description.Contains(text));
        }
        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = filter.Name.Trim();
            query = query.Where(t => t.Name != null && t.Name.Contains(name));
        }
        if (filter.AccountIds != null && filter.AccountIds.Length > 0)
            query = query.Where(t => t.AccountId != null && filter.AccountIds.Contains((Guid)t.AccountId));

        query = query
            .OrderByDescending(t => t.BookingDate)
            .ThenByDescending(t => t.TransactionId);

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Transactions.FirstOrDefaultAsync(t => t.TransactionId == id, cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Update(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteBySourceAsync(string sourceSystem, string? accountIdentifier = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Transactions.Where(t => t.SourceSystem == sourceSystem);

        if (!string.IsNullOrWhiteSpace(accountIdentifier))
        {
            query = query.Where(t => t.AccountIdentifier == accountIdentifier);
        }

        _dbContext.Transactions.RemoveRange(query);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetBySourceSystemAsync(string sourceSystem, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .Where(t => t.SourceSystem == sourceSystem)
            .ToListAsync(cancellationToken);
    }
}
