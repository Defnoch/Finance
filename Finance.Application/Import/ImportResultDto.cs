namespace Finance.Application.Import;

public sealed class ImportResultDto
{
    public Guid ImportBatchId { get; init; }
    public int TotalRecords { get; init; }
    public int InsertedRecords { get; init; }
    public int DuplicateRecords { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

