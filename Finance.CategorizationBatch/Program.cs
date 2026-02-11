using System;
using System.Threading.Tasks;
using Finance.Domain;
using Finance.Domain.Entities;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.CategorizationBatch;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"[CategorizationBatch] Start - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite("Data Source=../Finance.Api/finance.db")
            .Options;
        using var db = new FinanceDbContext(options);

        var rules = await db.CategorizationRules
            .Where(r => r.IsEnabled && !r.IsIgnored)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
        var engine = new RuleBasedCategorizationEngine();

        var transactions = await db.Transactions
            .Where(t => t.CategoryId == null)
            .ToListAsync();
        int updated = 0;
        foreach (var tx in transactions)
        {
            var input = new TransactionCategorizationInput
            {
                Direction = tx.Amount < 0 ? Direction.Debit : Direction.Credit,
                Amount = tx.Amount,
                Currency = tx.Currency,
                BookingDate = tx.BookingDate.ToDateTime(TimeOnly.MinValue),
                CounterpartyIban = tx.AccountIdentifier,
                CounterpartyName = tx.Name,
                Description = tx.Description,
                PaymentMethod = null // niet beschikbaar
            };
            var result = engine.Categorize(input, rules);
            if (result.IsMatched && Guid.TryParse(result.CategoryId, out var catId))
            {
                tx.CategoryId = catId;
                updated++;
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"[CategorizationBatch] Klaar. {updated} transacties gecategoriseerd.");
    }
}
