using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface ITransactionRepository
{
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySourceReferenceAsync(string sourceSystem, string sourceReference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByFilterAsync(TransactionFilter filter, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verwijder alle transacties voor een gegeven bron (bijv. ING) en optioneel voor een specifieke rekening.
    /// </summary>
    Task DeleteBySourceAsync(string sourceSystem, string? accountIdentifier = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetBySourceSystemAsync(string sourceSystem, CancellationToken cancellationToken = default);
}