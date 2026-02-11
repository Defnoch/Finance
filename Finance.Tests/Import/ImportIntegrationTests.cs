using System.Reflection;
using Finance.Application.Import;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Import;
using Finance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Finance.Tests.Import;

public class ImportIntegrationTests
{
    private static Stream OpenSampleCsv()
    {
        // Pad naar upload.csv in de Finance/Upload-map
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        // Ga vanuit test-bin omhoog naar solution-root en dan naar Finance/Upload/upload.csv
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var csvPath = Path.Combine(root, "Finance", "Upload", "upload.csv");

        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"Testbestand niet gevonden: {csvPath}");

        return File.OpenRead(csvPath);
    }

    private static FinanceDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinanceDbContext(options);
    }

    [Test]
    public async Task Import_WithSampleIngCsv_ParsesAndStoresTransactionsAndBatch()
    {
        await using var context = CreateInMemoryContext();

        ITransactionRepository transactionRepository = new TransactionRepository(context);
        IImportBatchRepository importBatchRepository = new ImportBatchRepository(context);
        IAccountRepository accountRepository = new AccountRepository(context);
        IBankImportStrategyResolver bankImportStrategyResolver = new BankImportStrategyResolver(new[] { new IngCsvImportStrategy() });

        ITransactionLinkerService transactionLinkerService = new TransactionLinkerService(
            transactionRepository,
            new TransactionLinkRepository(context),
            accountRepository);
        IAccountFiscalYearRepository accountFiscalYearRepository = new AccountFiscalYearRepository(context);
        IImportService importService = new ImportService(
            bankImportStrategyResolver,
            transactionRepository,
            importBatchRepository,
            accountRepository,
            transactionLinkerService,
            accountFiscalYearRepository);

        await using var csvStream = OpenSampleCsv();

        var command = new ImportTransactionsCommand
        {
            SourceSystem = "ING",
            FileName = "upload.csv",
            FileStream = csvStream
        };

        var result = await importService.ImportTransactionsAsync(command, CancellationToken.None);

        Assert.That(result.TotalRecords, Is.GreaterThan(0), "Geen records gevonden in CSV");
        Assert.That(result.InsertedRecords, Is.GreaterThan(0), "Geen records geïmporteerd");
        Assert.That(result.InsertedRecords, Is.LessThanOrEqualTo(result.TotalRecords), "Meer records geïmporteerd dan aanwezig");
        Assert.That(result.Errors.Count, Is.EqualTo(0), $"Import errors: {string.Join(", ", result.Errors)}");

        // Controleer dat de batch is opgeslagen
        var batch = await importBatchRepository.GetByIdAsync(result.ImportBatchId, CancellationToken.None);
        Assert.That(batch, Is.Not.Null, "Batch niet gevonden in database");
        Assert.That(batch!.TotalRecords, Is.EqualTo(result.TotalRecords));
        Assert.That(batch.InsertedRecords, Is.EqualTo(result.InsertedRecords));

        // Controleer dat de transacties zijn opgeslagen met de juiste BatchId
        var transactions = await transactionRepository.GetByFilterAsync(new TransactionFilter(), CancellationToken.None);
        Assert.That(transactions.Count, Is.EqualTo(result.InsertedRecords), "Aantal transacties in DB komt niet overeen met aantal geïmporteerde records");
        Assert.That(transactions.All(t => t.ImportBatchId == result.ImportBatchId), Is.True, "Niet alle transacties hebben het juiste ImportBatchId");
    }

    [Test]
    public async Task Import_SameCsvTwice_SecondImportRegistersDuplicates()
    {
        await using var context = CreateInMemoryContext();

        ITransactionRepository transactionRepository = new TransactionRepository(context);
        IImportBatchRepository importBatchRepository = new ImportBatchRepository(context);
        IAccountRepository accountRepository = new AccountRepository(context);
        IBankImportStrategyResolver bankImportStrategyResolver = new BankImportStrategyResolver(new[] { new IngCsvImportStrategy() });

        ITransactionLinkerService transactionLinkerService = new TransactionLinkerService(
            transactionRepository,
            new TransactionLinkRepository(context),
            accountRepository);
        IAccountFiscalYearRepository accountFiscalYearRepository = new AccountFiscalYearRepository(context);
        IImportService importService = new ImportService(
            bankImportStrategyResolver,
            transactionRepository,
            importBatchRepository,
            accountRepository,
            transactionLinkerService,
            accountFiscalYearRepository);

        // Eerste import
        await using (var csv1 = OpenSampleCsv())
        {
            var command1 = new ImportTransactionsCommand
            {
                SourceSystem = "ING",
                FileName = "upload.csv",
                FileStream = csv1
            };

            var result1 = await importService.ImportTransactionsAsync(command1, CancellationToken.None);

            Assert.That(result1.InsertedRecords, Is.GreaterThan(0), "Eerste import: geen records geïmporteerd");
            Assert.That(result1.DuplicateRecords, Is.EqualTo(0), "Eerste import: duplicaten gevonden");
        }

        // Tweede import met dezelfde file
        await using (var csv2 = OpenSampleCsv())
        {
            var command2 = new ImportTransactionsCommand
            {
                SourceSystem = "ING",
                FileName = "upload.csv",
                FileStream = csv2
            };

            var result2 = await importService.ImportTransactionsAsync(command2, CancellationToken.None);

            Assert.That(result2.DuplicateRecords, Is.EqualTo(result2.TotalRecords), "Tweede import: niet alle records als duplicaat herkend");
            Assert.That(result2.InsertedRecords, Is.EqualTo(0), "Tweede import: records toegevoegd terwijl alles duplicaat is");
        }

        // Controleer dat er uiteindelijk maar één set transacties in de database staat
        var allTransactions = await transactionRepository.GetByFilterAsync(new TransactionFilter(), CancellationToken.None);
        Assert.That(allTransactions.Count, Is.GreaterThan(0), "Geen transacties gevonden na dubbele import");
    }
}
