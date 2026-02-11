using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface IImportBatchRepository
{
    Task AddAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task<ImportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}