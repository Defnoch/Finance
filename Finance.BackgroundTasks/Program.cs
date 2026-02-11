using Microsoft.Extensions.Configuration;
using Finance.GeneratedApiClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finance.BackgroundTasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<BackgroundTaskWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();
        await host.RunAsync();
    }
}

public class BackgroundTaskWorker : BackgroundService
{
    private readonly IConfiguration _config;

    public BackgroundTaskWorker(IConfiguration config)
    {
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseUrl = _config["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.WriteLine("FOUT: Geen API base URL gevonden in de configuratie (Api:BaseUrl). Voeg deze toe aan appsettings.json, appsettings.Development.json of een environment variable.");
            return;
        }
        var apiClient = new FinanceApiClient(baseUrl);
        while (!stoppingToken.IsCancellationRequested)
        {
            var configs = await apiClient.ConfigAsync();
            foreach (var cfg in configs)
            {
                if (!cfg.IsEnabled) continue;
                var now = DateTime.UtcNow;
                var lastRun = cfg.LastRunAt ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var interval = TimeSpan.FromMinutes(Convert.ToDouble(cfg.IntervalMinutes));
                if (now - lastRun < interval) continue;
                Console.WriteLine($"[BackgroundTasks] Start taak: {cfg.Name}");
                var startedAt = DateTime.UtcNow;
                string status = "Success";
                string resultSummary = "";
                try
                {
                    IBackgroundTask? task = cfg.Name switch
                    {
                        "categorization" => new CategorizationTask(baseUrl),
                        // Voeg hier andere taken toe
                        _ => null
                    };
                    if (task == null)
                    {
                        status = "Skipped";
                        resultSummary = $"Geen implementatie voor taak: {cfg.Name}";
                    }
                    else
                    {
                        await task.RunAsync();
                        resultSummary = $"Taak {cfg.Name} succesvol uitgevoerd.";
                    }
                }
                catch (Exception ex)
                {
                    status = "Failed";
                    resultSummary = ex.Message;
                }
                var finishedAt = DateTime.UtcNow;
                // Controleer of TaskConfigId bestaat voordat je logt
                var configExists = configs.Any(c => c.Id == cfg.Id);
                if (!configExists)
                {
                    Console.WriteLine($"[BackgroundTasks] FOUT: TaskConfigId {cfg.Id} bestaat niet in de config. LastRun wordt niet opgeslagen.");
                }
                else
                {
                    await apiClient.LastrunAsync(cfg.Id, finishedAt);
                }
                Console.WriteLine($"[BackgroundTasks] Klaar taak: {cfg.Name} - {status} - {resultSummary}");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Poll elke minuut
        }
    }
}
