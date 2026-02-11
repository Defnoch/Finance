using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface IAccountFiscalYearRepository
{
    Task<IReadOnlyList<int>> GetYearsForAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddAsync(AccountFiscalYear fiscalYear, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<AccountFiscalYear> fiscalYears, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountFiscalYear>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
}
