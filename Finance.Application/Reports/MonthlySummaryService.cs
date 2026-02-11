using Finance.Domain.Repositories;

namespace Finance.Application.Reports;

public sealed class MonthlySummaryService : IMonthlySummaryService
{
    private readonly ITransactionRepository _transactionRepository;

    public MonthlySummaryService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<MonthlySummaryDto>> GetMonthlySummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var filter = new TransactionFilter
        {
            FromDate = from,
            ToDate = to
        };

        var transactions = await _transactionRepository.GetByFilterAsync(filter, cancellationToken);

        var summaries = transactions
            .GroupBy(t => new { t.BookingDate.Year, t.BookingDate.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new MonthlySummaryDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalIncome = g.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalExpense = g.Where(t => t.Amount < 0).Sum(t => -t.Amount)
            })
            .ToList();

        return summaries;
    }
}
