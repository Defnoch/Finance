namespace Finance.Application.BackgroundTasks;

public interface IBackgroundTaskService
{
    Task<IEnumerable<BackgroundTaskConfigDto>> GetConfigsAsync();
    Task UpdateLastRunAsync(Guid id, DateTime lastRunAt);
}
