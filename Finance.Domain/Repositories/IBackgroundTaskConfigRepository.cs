using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface IBackgroundTaskConfigRepository
{
    Task<BackgroundTaskConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(BackgroundTaskConfig config, CancellationToken cancellationToken = default);
    Task<List<BackgroundTaskConfig>> GetAllAsync(CancellationToken cancellationToken = default);
}
