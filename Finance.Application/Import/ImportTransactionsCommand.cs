namespace Finance.Application.Import;

public sealed class ImportTransactionsCommand
{
    public string SourceSystem { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public Stream FileStream { get; init; } = Stream.Null;

    /// <summary>
    /// Als true: alle bestaande transacties voor deze bron worden verwijderd voordat de nieuwe set wordt opgeslagen.
    /// </summary>
    public bool OverrideExisting { get; init; }
}
