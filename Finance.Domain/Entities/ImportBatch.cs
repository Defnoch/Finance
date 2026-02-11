namespace Finance.Domain.Entities;

public class ImportBatch
{
    public Guid ImportBatchId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }

    public int TotalRecords { get; set; }
    public int InsertedRecords { get; set; }
    public int DuplicateRecords { get; set; }

    public ImportBatchStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}