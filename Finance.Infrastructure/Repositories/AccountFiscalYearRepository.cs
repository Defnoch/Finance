using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

public sealed class AccountFiscalYearRepository : IAccountFiscalYearRepository
{
    private readonly FinanceDbContext _dbContext;
    public AccountFiscalYearRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<int>> GetYearsForAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountFiscalYears
            .Where(x => x.AccountId == accountId)
            .Select(x => x.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AccountFiscalYear fiscalYear, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountFiscalYears.AddAsync(fiscalYear, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<AccountFiscalYear> fiscalYears, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountFiscalYears.AddRangeAsync(fiscalYears, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountFiscalYear>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountFiscalYears
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }
}
