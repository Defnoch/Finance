namespace Finance.Application.Reports;

public sealed class TransactionDto
{
    public Guid TransactionId { get; init; }
    public DateOnly BookingDate { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Description { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal? ResultingBalance { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// "Normaal" voor gewone rekening, "Spaar" voor spaarrekening, o.b.v. SourceSystem
    /// </summary>
    public string AccountType { get; init; } = string.Empty;
    public Guid? AccountId { get; init; }
    public Guid? CounterpartyAccountId { get; init; }
    public Guid? LinkedTransactionId { get; set; }
}
