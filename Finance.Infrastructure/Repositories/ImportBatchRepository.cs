using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

public sealed class ImportBatchRepository : IImportBatchRepository
{
    private readonly FinanceDbContext _dbContext;

    public ImportBatchRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ImportBatch batch, CancellationToken cancellationToken = default)
    {
        await _dbContext.ImportBatches.AddAsync(batch, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ImportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ImportBatches.FirstOrDefaultAsync(b => b.ImportBatchId == id, cancellationToken);
    }
}

