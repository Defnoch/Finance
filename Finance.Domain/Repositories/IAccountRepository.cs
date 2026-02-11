using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdentifierAsync(string provider, string accountIdentifier, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default); // Toegevoegd voor TransactionLinkerService
}
