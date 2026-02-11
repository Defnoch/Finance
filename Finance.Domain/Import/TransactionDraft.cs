namespace Finance.Domain.Import;

public sealed class TransactionDraft
{
    public string SourceSystem { get; init; } = string.Empty;
    public string SourceReference { get; init; } = string.Empty;
    public DateOnly BookingDate { get; init; }
    public DateOnly? ValueDate { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";

    public decimal? ResultingBalance { get; init; }

    public string TransactionType { get; init; } = string.Empty;

    public string? Notifications { get; init; }

    public string AccountIdentifier { get; init; } = string.Empty;
    public string? CounterpartyIdentifier { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? RawData { get; init; }
    public string Name { get; init; } = string.Empty;
}