namespace Finance.Application.BackgroundTasks;

public class BackgroundTaskConfigDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastRunAt { get; set; }
}
