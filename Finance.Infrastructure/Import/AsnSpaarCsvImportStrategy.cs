using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Finance.Domain.Import;

namespace Finance.Infrastructure.Import;

/// <summary>
/// ASN Spaarrekening CSV importstrategie, koppelt spaartransacties aan normale ASN transacties.
/// </summary>
public sealed class AsnSpaarCsvImportStrategy : IBankImportStrategy
{
    public string SourceSystem => "ASN_SPAAR";

    public bool CanHandle(string sourceSystem, string fileName)
    {
        return sourceSystem.Equals("ASN_SPAAR", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("asn_spaar", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<TransactionDraft>> ParseAsync(
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (csvStream == Stream.Null)
            return Array.Empty<TransactionDraft>();

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);
        if (!await csv.ReadAsync() || !csv.ReadHeader())
            return Array.Empty<TransactionDraft>();
        var header = csv.HeaderRecord ?? Array.Empty<string>();
        var drafts = new List<TransactionDraft>();
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string Get(string col) => csv.TryGetField(col, out string? val) ? val ?? string.Empty : string.Empty;
            var dateStr = Get("Datum");
            var rekening = Get("Je rekening");
            var tegenrekening = Get("Van / naar");
            var naam = Get("Naam");
            var bedragStr = Get("Bedrag bij / af");
            var omschrijving = Get("Omschrijving");
            if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(bedragStr))
                continue;
            if (!DateOnly.TryParseExact(dateStr.Trim('"'), new[] { "yyyy-MM-dd", "dd-MM-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var bookingDate))
                continue;
            var bedragNorm = bedragStr.Replace("\"", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
            if (!decimal.TryParse(bedragNorm, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                continue;
            var sourceReference = $"{bookingDate:yyyy-MM-dd}|{amount.ToString(CultureInfo.InvariantCulture)}|{rekening}|{naam}|{omschrijving}";
            var draft = new TransactionDraft
            {
                SourceSystem = SourceSystem,
                SourceReference = sourceReference,
                BookingDate = bookingDate,
                ValueDate = bookingDate,
                Amount = amount,
                Currency = "EUR",
                ResultingBalance = null,
                TransactionType = string.Empty,
                Notifications = null,
                AccountIdentifier = rekening,
                CounterpartyIdentifier = string.IsNullOrWhiteSpace(tegenrekening) ? null : tegenrekening,
                Description = omschrijving,
                RawData = null,
                Name = naam
            };
            drafts.Add(draft);
        }
        return drafts;
    }
}
