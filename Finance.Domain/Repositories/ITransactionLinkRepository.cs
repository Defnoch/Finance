using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface ITransactionLinkRepository
{
    Task AddAsync(TransactionLink link, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionLink>> GetLinksForTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid transactionId1, Guid transactionId2, CancellationToken cancellationToken = default);
}
