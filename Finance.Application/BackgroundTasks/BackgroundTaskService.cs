using Finance.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.BackgroundTasks;

public class BackgroundTaskService : IBackgroundTaskService
{
    private readonly IBackgroundTaskConfigRepository _repo;
    public BackgroundTaskService(IBackgroundTaskConfigRepository repo) => _repo = repo;

    public async Task<IEnumerable<BackgroundTaskConfigDto>> GetConfigsAsync()
    {
        var configs = await _repo.GetAllAsync();
        return configs.Select(c => new BackgroundTaskConfigDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IntervalMinutes = c.IntervalMinutes,
            IsEnabled = c.IsEnabled,
            LastRunAt = c.LastRunAt
        }).ToList();
    }

    public async Task UpdateLastRunAsync(Guid id, DateTime lastRunAt)
    {
        var config = await _repo.GetByIdAsync(id);
        if (config == null) throw new KeyNotFoundException();
        config.LastRunAt = lastRunAt;
        await _repo.UpdateAsync(config);
    }
}
