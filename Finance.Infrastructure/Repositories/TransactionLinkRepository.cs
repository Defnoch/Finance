using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

public sealed class TransactionLinkRepository : ITransactionLinkRepository
{
    private readonly FinanceDbContext _dbContext;
    public TransactionLinkRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TransactionLink link, CancellationToken cancellationToken = default)
    {
        await _dbContext.TransactionLinks.AddAsync(link, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionLink>> GetLinksForTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TransactionLinks
            .Where(l => l.TransactionId1 == transactionId || l.TransactionId2 == transactionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid transactionId1, Guid transactionId2, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TransactionLinks.AnyAsync(l =>
            (l.TransactionId1 == transactionId1 && l.TransactionId2 == transactionId2) ||
            (l.TransactionId1 == transactionId2 && l.TransactionId2 == transactionId1),
            cancellationToken);
    }
}
