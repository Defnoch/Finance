using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly FinanceDbContext _dbContext;
    public AccountRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> GetByIdentifierAsync(string provider, string accountIdentifier, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Provider == provider && a.AccountIdentifier == accountIdentifier, cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _dbContext.Accounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts.ToListAsync(cancellationToken);
    }
}
