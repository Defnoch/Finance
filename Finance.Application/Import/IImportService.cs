namespace Finance.Application.Import;

public interface IImportService
{
    Task<ImportResultDto> ImportTransactionsAsync(ImportTransactionsCommand command, CancellationToken cancellationToken = default);
}
