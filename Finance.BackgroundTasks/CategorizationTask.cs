using Finance.GeneratedApiClient;

namespace Finance.BackgroundTasks;

public class CategorizationTask : IBackgroundTask
{
    private readonly string _baseUrl;
    public string Name => "categorization";

    public CategorizationTask(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"[CategorizationTask] Start - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        var apiClient = new FinanceApiClient(_baseUrl);
        var startedAt = DateTime.UtcNow;
        const int batchSize = 100;
        int updated = 0;
        string status = "Success";
        string resultSummary = "";
        try
        {
            // Haal alle rules op via de API
            var rules = (await apiClient.RulesAllAsync()).Where(r => r.IsEnabled && !r.IsIgnored).OrderByDescending(r => r.Priority).ToList();
            // Haal alle accounts op via de juiste methode
            var accounts = await apiClient.AccountsAsync(System.Threading.CancellationToken.None);
            var assignCommands = new List<AssignCategoryCommand>();
            foreach (var account in accounts)
            {
                // Haal alle transacties op zonder categorie voor dit account
                var accountIds = new List<Guid> { account.AccountId };
                var transactions = (await apiClient.TransactionsAsync(accountIds, null, null, null, null, null, null, null, null)).Where(t => t.CategoryId == null).ToList();
                foreach (var rule in rules)
                {
                    // Alleen rules met een naam-conditie (equals)
                    var nameCond = rule.Conditions?.FirstOrDefault(c => c.Field == "Name" && c.Operator == "Equals" && !string.IsNullOrWhiteSpace(c.Value));
                    if (nameCond == null) continue;
                    var matchingTxs = transactions.Where(t => string.Equals(t.Name, nameCond.Value, StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var tx in matchingTxs)
                    {
                        assignCommands.Add(new AssignCategoryCommand {
                            TransactionId = tx.TransactionId,
                            CategoryId = rule.CategoryId
                        });
                        if (assignCommands.Count >= batchSize)
                        {
                            await apiClient.BatchAssignAsync(assignCommands);
                            updated += assignCommands.Count;
                            assignCommands.Clear();
                        }
                    }
                }
            }
            if (assignCommands.Count > 0)
            {
                await apiClient.BatchAssignAsync(assignCommands);
                updated += assignCommands.Count;
            }
            resultSummary = $"{updated} transacties direct gecategoriseerd op basis van rules.";
        }
        catch (Exception ex)
        {
            status = "Failed";
            resultSummary = ex.Message;
        }
        var finishedAt = DateTime.UtcNow;
        Console.WriteLine($"[CategorizationTask] Klaar. {resultSummary}");
    }
}
