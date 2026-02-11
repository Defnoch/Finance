namespace Finance.Domain.Entities;

public class Transaction
{
    public Guid TransactionId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }
    public DateOnly? ValueDate { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";

    public decimal? ResultingBalance { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public string? Notifications { get; set; }

    public string AccountIdentifier { get; set; } = string.Empty;
    public string? CounterpartyIdentifier { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? RawData { get; set; }

    public Guid? CategoryId { get; set; }
    public Guid ImportBatchId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? AccountId { get; set; }
    public Guid? CounterpartyAccountId { get; set; }
}