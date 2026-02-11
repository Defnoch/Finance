namespace Finance.Application.Reports;

public interface IMonthlySummaryService
{
    Task<IReadOnlyList<MonthlySummaryDto>> GetMonthlySummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
