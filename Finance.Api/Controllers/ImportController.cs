using Finance.Application.Import;
using Finance.Application.Validators;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ImportController : ControllerBase
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("ing")]
    public async Task<ActionResult<ImportResultDto>> ImportIngCsv(
        IFormFile file,
        [FromQuery] bool overrideExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var stream = file.OpenReadStream();
        if (!CsvHeaderValidator.Validate(stream, "ING", out var error))
        {
            return BadRequest($"CSV header ongeldig voor ING betaalrekening: {error}");
        }
        stream.Seek(0, SeekOrigin.Begin);

        var command = new ImportTransactionsCommand
        {
            SourceSystem = "ING",
            FileName = file.FileName,
            FileStream = stream,
            OverrideExisting = overrideExisting
        };

        var result = await _importService.ImportTransactionsAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Import ING Spaarrekening CSV. Expected columns: Datum;"Omschrijving";"Rekening";"Rekening naam";"Tegenrekening";"Af Bij";"Bedrag";"Valuta";"Mutatiesoort";"Mededelingen";"Saldo na mutatie"
    /// </summary>
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("ing-spaar")]
    public async Task<ActionResult<ImportResultDto>> ImportIngSpaarCsv(
        IFormFile file,
        [FromQuery] bool overrideExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var stream = file.OpenReadStream();
        if (!CsvHeaderValidator.Validate(stream, "ING_SPAAR", out var error))
        {
            return BadRequest($"CSV header ongeldig voor ING spaarrekening: {error}");
        }
        stream.Seek(0, SeekOrigin.Begin);

        var command = new ImportTransactionsCommand
        {
            SourceSystem = "ING_SPAAR",
            FileName = file.FileName,
            FileStream = stream,
            OverrideExisting = overrideExisting
        };

        var result = await _importService.ImportTransactionsAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Import ASN rekening CSV. Verwachte kolommen: Datum;Je rekening;Tegenrekening;Naam;Omschrijving;Bedrag bij / af;Valuta;Saldo voor boeking;Van / naar
    /// </summary>
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("asn")]
    public async Task<ActionResult<ImportResultDto>> ImportAsnCsv(
        IFormFile file,
        [FromQuery] bool overrideExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var stream = file.OpenReadStream();
        if (!CsvHeaderValidator.Validate(stream, "ASN", out var error))
        {
            return BadRequest($"CSV header ongeldig voor ASN rekening: {error}");
        }
        stream.Seek(0, SeekOrigin.Begin);

        var command = new ImportTransactionsCommand
        {
            SourceSystem = "ASN",
            FileName = file.FileName,
            FileStream = stream,
            OverrideExisting = overrideExisting
        };

        var result = await _importService.ImportTransactionsAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Import ASN Spaarrekening CSV. Verwachte kolommen: Datum;Je rekening;Van / naar;Naam;Omschrijving;Bedrag bij / af
    /// </summary>
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("asn-spaar")]
    public async Task<ActionResult<ImportResultDto>> ImportAsnSpaarCsv(
        IFormFile file,
        [FromQuery] bool overrideExisting = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var stream = file.OpenReadStream();
        // ASN spaar: alleen checken op presence van basisvelden, volgorde negeren
        if (!CsvHeaderValidator.Validate(stream, "ASN_SPAAR", out var error))
        {
            return BadRequest($"CSV header ongeldig voor ASN spaarrekening: {error}");
        }
        stream.Seek(0, SeekOrigin.Begin);

        var command = new ImportTransactionsCommand
        {
            SourceSystem = "ASN_SPAAR",
            FileName = file.FileName,
            FileStream = stream,
            OverrideExisting = overrideExisting
        };

        var result = await _importService.ImportTransactionsAsync(command, cancellationToken);
        return Ok(result);
    }
}
