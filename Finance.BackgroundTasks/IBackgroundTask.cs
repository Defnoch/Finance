namespace Finance.BackgroundTasks;

public interface IBackgroundTask
{
    string Name { get; }
    Task RunAsync();
}
