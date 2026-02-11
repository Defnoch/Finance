namespace Finance.Domain.Import;

public interface IBankImportStrategy
{
    string SourceSystem { get; }

    Task<IReadOnlyList<TransactionDraft>> ParseAsync(
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default);

    // Returns true if this strategy can handle the given sourceSystem and fileName
    bool CanHandle(string sourceSystem, string fileName);
}