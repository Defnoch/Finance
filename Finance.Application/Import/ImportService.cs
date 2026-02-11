using Finance.Domain.Entities;
using Finance.Domain.Import;
using Finance.Domain.Repositories;
using TransactionFilter = Finance.Domain.Repositories.TransactionFilter;

namespace Finance.Application.Import;

public sealed class ImportService : IImportService
{
    private readonly IBankImportStrategyResolver _strategyResolver;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionLinkerService _transactionLinkerService;
    private readonly IAccountFiscalYearRepository _accountFiscalYearRepository;

    public ImportService(
        IBankImportStrategyResolver bankImportStrategyResolver,
        ITransactionRepository transactionRepository,
        IImportBatchRepository importBatchRepository,
        IAccountRepository accountRepository,
        ITransactionLinkerService transactionLinkerService,
        IAccountFiscalYearRepository accountFiscalYearRepository)
    {
        _strategyResolver = bankImportStrategyResolver;
        _transactionRepository = transactionRepository;
        _importBatchRepository = importBatchRepository;
        _accountRepository = accountRepository;
        _transactionLinkerService = transactionLinkerService;
        _accountFiscalYearRepository = accountFiscalYearRepository;
    }

    public async Task<ImportResultDto> ImportTransactionsAsync(
        ImportTransactionsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.FileStream == Stream.Null)
        {
            return new ImportResultDto
            {
                ImportBatchId = Guid.Empty,
                TotalRecords = 0,
                InsertedRecords = 0,
                DuplicateRecords = 0,
                Errors = new[] { "No file content provided." }
            };
        }

        IReadOnlyList<TransactionDraft> drafts;
        try
        {
            var strategy = _strategyResolver.Resolve(command.SourceSystem, command.FileName);
            drafts = await strategy.ParseAsync(
                command.FileStream,
                command.FileName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                ImportBatchId = Guid.Empty,
                TotalRecords = 0,
                InsertedRecords = 0,
                DuplicateRecords = 0,
                Errors = new[] { $"Parsing failed: {ex.Message}" }
            };
        }

        if (drafts.Count == 0)
        {
            return new ImportResultDto
            {
                ImportBatchId = Guid.Empty,
                TotalRecords = 0,
                InsertedRecords = 0,
                DuplicateRecords = 0,
                Errors = Array.Empty<string>()
            };
        }

        // Bepaal (optioneel) rekening voor override: bij ING is dit de Account-kolom, die in alle drafts gelijk is voor dezelfde rekening.
        if (command.OverrideExisting)
        {
            var uniqueAccounts = drafts
                .Select(d => d.AccountIdentifier)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();
            if (uniqueAccounts.Count == 0)
            {
                return new ImportResultDto
                {
                    ImportBatchId = Guid.Empty,
                    TotalRecords = drafts.Count,
                    InsertedRecords = 0,
                    DuplicateRecords = 0,
                    Errors = new[] { "Geen rekeningnummers gevonden in de import." }
                };
            }
            // Voor elk uniek rekeningnummer: verwijder bestaande transacties
            foreach (var overrideAccount in uniqueAccounts)
            {
                await _transactionRepository.DeleteBySourceAsync(
                    command.SourceSystem,
                    overrideAccount,
                    cancellationToken);
            }
        }

        var totalRecords = drafts.Count;
        var newTransactions = new List<Transaction>(totalRecords);
        var duplicateCount = 0;

        foreach (var draft in drafts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceSystem = command.SourceSystem;
            var sourceReference = draft.SourceReference;

            if (!command.OverrideExisting)
            {
                var exists = await _transactionRepository.ExistsBySourceReferenceAsync(
                    sourceSystem,
                    sourceReference,
                    cancellationToken);

                if (exists)
                {
                    duplicateCount++;
                    continue;
                }
            }

            var name = string.IsNullOrWhiteSpace(draft.Name) ? draft.Description : draft.Name;
            var tx = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                SourceSystem = sourceSystem,
                SourceReference = sourceReference,
                BookingDate = draft.BookingDate,
                ValueDate = draft.ValueDate ?? draft.BookingDate,
                Amount = draft.Amount,
                Currency = draft.Currency,
                ResultingBalance = draft.ResultingBalance,
                TransactionType = draft.TransactionType,
                Notifications = draft.Notifications,
                AccountIdentifier = draft.AccountIdentifier,
                CounterpartyIdentifier = draft.CounterpartyIdentifier,
                Description = draft.Description,
                RawData = draft.RawData,
                CategoryId = null,
                ImportBatchId = Guid.Empty, // vullen we na het aanmaken van de batch
                Name = name
            };

            // In de import-loop, vóór het aanmaken van de transactie:
            // Controleer of account bestaat, zo niet: maak aan
            var provider = command.SourceSystem.StartsWith("ING") ? "ING" : command.SourceSystem;
            var accountType = command.SourceSystem == "ING_SPAAR" || command.SourceSystem == "ASN_SPAAR" ? "Spaar" : "Normaal";
            var accountIdentifier = draft.AccountIdentifier;
            if (!string.IsNullOrWhiteSpace(accountIdentifier))
            {
                var account = await _accountRepository.GetByIdentifierAsync(provider, accountIdentifier, cancellationToken);
                if (account == null)
                {
                    account = new Account
                    {
                        AccountId = Guid.NewGuid(),
                        AccountIdentifier = accountIdentifier,
                        Provider = provider,
                        AccountType = accountType
                    };
                    await _accountRepository.AddAsync(account, cancellationToken);
                }
                tx.AccountId = account.AccountId;
            }

            // Toevoegen van CounterpartyAccountId indien mogelijk
            if (!string.IsNullOrWhiteSpace(draft.CounterpartyIdentifier))
            {
                var counterpartyAccount = await _accountRepository.GetByIdentifierAsync(provider, draft.CounterpartyIdentifier, cancellationToken);
                if (counterpartyAccount != null)
                {
                    tx.CounterpartyAccountId = counterpartyAccount.AccountId;
                }
            }

            newTransactions.Add(tx);
        }

        var insertedRecords = newTransactions.Count;
        var batchId = Guid.NewGuid();

        // Batch aanmaken
        var importBatch = new ImportBatch
        {
            ImportBatchId = batchId,
            SourceSystem = command.SourceSystem,
            FileName = command.FileName,
            ImportedAt = DateTime.UtcNow,
            TotalRecords = totalRecords,
            InsertedRecords = insertedRecords,
            DuplicateRecords = duplicateCount,
            Status = ImportBatchStatus.Succeeded,
            ErrorMessage = null
        };

        // BatchId op transacties zetten
        foreach (var tx in newTransactions)
        {
            tx.ImportBatchId = batchId;
        }

        if (insertedRecords > 0)
        {
            await _transactionRepository.AddRangeAsync(newTransactions, cancellationToken);
        }

        await _importBatchRepository.AddAsync(importBatch, cancellationToken);

        // Na het opslaan van nieuwe transacties:
        var insertedTransactions = await _transactionRepository.GetByFilterAsync(
            new TransactionFilter { ImportBatchId = batchId }, cancellationToken);
        await _transactionLinkerService.LinkTransactionsAsync(insertedTransactions, cancellationToken);

        // --- Boekjaren per account toevoegen (transactioneel, als laatste stap) ---
        if (insertedRecords > 0)
        {
            // Bepaal per account alle jaren uit de nieuwe transacties
            var accountYearMap = newTransactions
                .Where(t => t.AccountId != Guid.Empty)
                .GroupBy(t => t.AccountId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(t => t.BookingDate.Year).Distinct().ToList()
                );
            foreach (var kvp in accountYearMap)
            {
                var accountId = kvp.Key;
                var yearsInImport = kvp.Value;
                var existingYears = await _accountFiscalYearRepository.GetYearsForAccountAsync(accountId, cancellationToken);
                var missingYears = yearsInImport.Except(existingYears).ToList();
                if (missingYears.Count > 0)
                {
                    var fiscalYears = missingYears.Select(y => new AccountFiscalYear(accountId, y));
                    await _accountFiscalYearRepository.AddRangeAsync(fiscalYears, cancellationToken);
                }
            }
        }
        // --- EINDE boekjaren toevoegen ---

        return new ImportResultDto
        {
            ImportBatchId = batchId,
            TotalRecords = totalRecords,
            InsertedRecords = insertedRecords,
            DuplicateRecords = duplicateCount,
            Errors = Array.Empty<string>()
        };
    }
}
