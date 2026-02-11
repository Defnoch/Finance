namespace Finance.Application.Reports;

public sealed class GetTransactionsQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? SearchText { get; init; }
    public string? Name { get; init; } // Filter op transactie naam
    public Guid? AccountId { get; init; } // Filter op rekening
    public Guid[]? AccountIds { get; init; } // Filter op meerdere rekeningen
}
