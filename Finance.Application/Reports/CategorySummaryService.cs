using Finance.Domain.Repositories;

namespace Finance.Application.Reports;

public sealed class CategorySummaryService : ICategorySummaryService
{
    private readonly ITransactionRepository _transactionRepository;

    public CategorySummaryService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetCategorySummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var filter = new TransactionFilter
        {
            FromDate = from,
            ToDate = to
        };

        var transactions = await _transactionRepository.GetByFilterAsync(filter, cancellationToken);

        var summaries = transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new CategorySummaryDto
            {
                CategoryId = g.Key,
                // CategoryName is (nog) niet direct bekend uit Transaction; wordt desnoods later ingevuld via join.
                CategoryName = string.Empty,
                TotalAmount = g.Sum(t => t.Amount)
            })
            .ToList();

        return summaries;
    }
}
