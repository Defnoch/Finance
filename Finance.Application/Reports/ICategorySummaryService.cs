namespace Finance.Application.Reports;

public interface ICategorySummaryService
{
    Task<IReadOnlyList<CategorySummaryDto>> GetCategorySummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
