namespace Finance.Application.Reports;

public interface ITransactionReportService
{
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(GetTransactionsQuery query, CancellationToken cancellationToken = default);
}
