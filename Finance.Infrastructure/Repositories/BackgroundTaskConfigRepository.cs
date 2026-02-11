using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

public class BackgroundTaskConfigRepository : IBackgroundTaskConfigRepository
{
    private readonly FinanceDbContext _db;
    public BackgroundTaskConfigRepository(FinanceDbContext db) => _db = db;

    public async Task<BackgroundTaskConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.BackgroundTaskConfigs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task UpdateAsync(BackgroundTaskConfig config, CancellationToken cancellationToken = default)
    {
        var dbEntity = await _db.BackgroundTaskConfigs.FirstOrDefaultAsync(x => x.Id == config.Id, cancellationToken);
        if (dbEntity == null)
            throw new KeyNotFoundException($"Config met id {config.Id} niet gevonden voor update!");

        dbEntity.Name = config.Name;
        dbEntity.Description = config.Description;
        dbEntity.IntervalMinutes = config.IntervalMinutes;
        dbEntity.IsEnabled = config.IsEnabled;
        dbEntity.LastRunAt = config.LastRunAt;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<BackgroundTaskConfig>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.BackgroundTaskConfigs.AsNoTracking().ToListAsync(cancellationToken);
}
