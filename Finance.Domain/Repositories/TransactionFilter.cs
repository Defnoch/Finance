namespace Finance.Domain.Repositories;

public class TransactionFilter
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? SearchText { get; init; }
    public Guid? ImportBatchId { get; init; } // Toegevoegd voor batch-filtering
    public string? Name { get; set; } // Filter op transactie naam
    public Guid[]? AccountIds { get; init; } // Filter op account IDs
}