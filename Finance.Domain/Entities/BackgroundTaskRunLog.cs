namespace Finance.Domain.Entities;

public class BackgroundTaskRunLog
{
    public Guid Id { get; set; }
    public Guid TaskConfigId { get; set; }
    public BackgroundTaskConfig TaskConfig { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
