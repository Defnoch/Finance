using System.Net.Http.Headers;
using System.Text;
using Finance.Api.Controllers;
using Finance.Application.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace Finance.Tests.Import;

[TestFixture]
public class ImportControllerTests
{
    private IImportService _importService = null!;
    private ImportController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _importService = Substitute.For<IImportService>();
        _controller = new ImportController(_importService);
    }

    [Test]
    public async Task ImportIngCsv_InvalidHeader_ReturnsBadRequest()
    {
        // Arrange: CSV met verkeerde header
        var csv = "FOUTE1;FOUTE2\n1;2";
        var file = CreateFormFile(csv, "test.csv");

        // Act
        var result = await _controller.ImportIngCsv(file, false, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(((BadRequestObjectResult)result.Result!).Value!.ToString(), Does.Contain("CSV header ongeldig"));
    }

    [Test]
    public async Task ImportIngSpaarCsv_InvalidHeader_ReturnsBadRequest()
    {
        // Arrange: CSV met verkeerde header
        var csv = "FOUTE1;FOUTE2\n1;2";
        var file = CreateFormFile(csv, "test.csv");

        // Act
        var result = await _controller.ImportIngSpaarCsv(file, false, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(((BadRequestObjectResult)result.Result!).Value!.ToString(), Does.Contain("CSV header ongeldig"));
    }

    [Test]
    public async Task ImportIngCsv_ValidHeader_CallsImportService()
    {
        // Arrange: geldige ING header
        var csv = "Date;Name / Description;Account;Counterparty;Code;Debit/credit;Amount (EUR);Transaction type;Notifications;Resulting balance;Tag\n...";
        var file = CreateFormFile(csv, "ing.csv");
        _importService.ImportTransactionsAsync(Arg.Any<ImportTransactionsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ImportResultDto { TotalRecords = 1 });

        // Act
        var result = await _controller.ImportIngCsv(file, false, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var dto = ((OkObjectResult)result.Result!).Value as ImportResultDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.TotalRecords, Is.EqualTo(1));
    }

    [Test]
    public async Task ImportIngSpaarCsv_ValidHeader_CallsImportService()
    {
        // Arrange: geldige ING Spaar header
        var csv = "Datum;Omschrijving;Rekening;Rekening naam;Tegenrekening;Af Bij;Bedrag;Valuta;Mutatiesoort;Mededelingen;Saldo na mutatie\n...";
        var file = CreateFormFile(csv, "spaar.csv");
        _importService.ImportTransactionsAsync(Arg.Any<ImportTransactionsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ImportResultDto { TotalRecords = 2 });

        // Act
        var result = await _controller.ImportIngSpaarCsv(file, false, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var dto = ((OkObjectResult)result.Result!).Value as ImportResultDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.TotalRecords, Is.EqualTo(2));
    }

    [Test]
    public async Task ImportAsnCsv_ValidExampleFile_CallsImportService()
    {
        // Arrange: gebruik het ASN voorbeeldbestand direct uit de testproject root
        var testDir = TestContext.CurrentContext.TestDirectory;
        var filePath = Path.Combine(testDir, "asn_account.csv");
        var fileContent = File.ReadAllText(filePath);
        var file = CreateFormFile(fileContent, "asn_account.csv");
        _importService.ImportTransactionsAsync(Arg.Any<ImportTransactionsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ImportResultDto { TotalRecords = 8 });

        // Act
        var result = await _controller.ImportAsnCsv(file, false, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var dto = ((OkObjectResult)result.Result!).Value as ImportResultDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.TotalRecords, Is.EqualTo(8));
    }

    [Test]
    public async Task ImportAsnSpaarCsv_ValidExampleFile_CallsImportService()
    {
        // Arrange: gebruik het ASN spaar voorbeeldbestand uit de testproject root
        var testDir = TestContext.CurrentContext.TestDirectory;
        var filePath = Path.Combine(testDir, "asn_spaar.csv");
        var fileContent = File.ReadAllText(filePath);
        var file = CreateFormFile(fileContent, "asn_spaar.csv");
        var importService = Substitute.For<IImportService>();
        importService.ImportTransactionsAsync(Arg.Any<ImportTransactionsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ImportResultDto { TotalRecords = 8 });
        var controller = new ImportController(importService);

        // Act
        var result = await controller.ImportAsnSpaarCsv(file, false, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var dto = ((OkObjectResult)result.Result!).Value as ImportResultDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.TotalRecords, Is.EqualTo(8));
    }

    private static IFormFile CreateFormFile(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }
}
