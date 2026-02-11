using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Finance.Domain.Import;

namespace Finance.Infrastructure.Import;

/// <summary>
/// ASN CSV importstrategie afgestemd op het formaat van Examples/asn_account.csv.
/// </summary>
public sealed class AsnCsvImportStrategy : IBankImportStrategy
{
    public string SourceSystem => "ASN";

    public bool CanHandle(string sourceSystem, string fileName)
    {
        // Herkenning op sourceSystem of bestandsnaam
        return sourceSystem.Equals("ASN", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("asn", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<TransactionDraft>> ParseAsync(
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (csvStream == Stream.Null)
            return Array.Empty<TransactionDraft>();

        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        // ASN export: alles in één veld per regel, gescheiden door ; en met dubbele quotes
        // Detecteer of de eerste regel (header) maar één veld bevat
        var peek = reader.Peek();
        if (peek == -1)
            return Array.Empty<TransactionDraft>();
        var firstLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(firstLine))
            return Array.Empty<TransactionDraft>();
        // Detectie: test-bestand (geen quotes, gewone delimiter) of ASN-export (alles in quotes)
        var isTestFormat = firstLine.Contains("Datum") && firstLine.Contains("Je rekening") && firstLine.Split(';').Length > 2;
        var isSingleField = !isTestFormat && firstLine.Count(c => c == ';') > 0 && firstLine.Count(c => c == '"') > 2 && firstLine.Split(';').Length == 1;
        // Zet alle variabelen bovenaan zodat ze overal beschikbaar zijn
        List<string[]> rows = new();
        string[] headerMap = Array.Empty<string>();
        Dictionary<string, int>? headerMapDict = null;
        if (isSingleField)
        {
            // ASN: elke regel is één veld met daarin een ;-gescheiden string, velden omgeven door dubbele/dubbele quotes
            string[] ParseAsnLine(string line)
            {
                // Strip buitenste quotes (en evt. komma's aan eind)
                line = line.Trim();
                if (line.StartsWith('"') && line.EndsWith('"'))
                    line = line.Substring(1, line.Length - 2);
                // Split op ;
                var fields = line.Split(';');
                // Strip per veld alle quotes (ook dubbele)
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = fields[i].Trim().Trim('"');
                }
                return fields;
            }
            // Header
            var headerFields = ParseAsnLine(firstLine);
            // ASN: headerFields bevat nu strings als: Datum, Je rekening, Van / naar, Naam, ...
            // Map kolomnamen naar index
            headerMapDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerFields.Length; i++)
            {
                var clean = headerFields[i].Replace("\"", "").Trim();
                headerMapDict[clean] = i;
            }
            rows.Add(headerFields);
            // Data
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = ParseAsnLine(line).ToList();
                // Combineer bedragen: zoek alle velden die exact een getal zijn en gevolgd worden door een veld van exact twee cijfers
                for (int i = 0; i < fields.Count - 1; i++)
                {
                    if ((fields[i].All(char.IsDigit) || (fields[i].StartsWith("-") && fields[i].Substring(1).All(char.IsDigit)))
                        && fields[i + 1].Length == 2 && fields[i + 1].All(char.IsDigit))
                    {
                        fields[i] = fields[i] + "," + fields[i + 1];
                        fields.RemoveAt(i + 1);
                        i--; // blijf op deze index voor meerdere bedragen
                    }
                }
                rows.Add(fields.ToArray());
            }
            // Vervang headerMap door headerMapDict voor dynamische kolomindexen
            headerMap = headerFields;
        }
        else if (isTestFormat)
        {
            // Test-CSV: gewone CSV, geen quotes, delimiter ;
            var headerFields = firstLine.Split(';').Select(f => f.Trim('"')).ToArray();
            headerMap = headerFields;
            headerMapDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerFields.Length; i++)
                headerMapDict[headerFields[i]] = i;
            rows.Add(headerFields);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = line.Split(';').Select(f => f.Trim('"')).ToArray();
                rows.Add(fields);
            }
        }
        else
        {
            // Normale CSV: gebruik CsvHelper
            csvStream.Seek(0, SeekOrigin.Begin);
            using var normalReader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var config = new CsvConfiguration(CultureInfo.GetCultureInfo("nl-NL"))
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };
            using var csv = new CsvReader(normalReader, config);
            if (!await csv.ReadAsync() || !csv.ReadHeader())
                return Array.Empty<TransactionDraft>();
            var header = csv.HeaderRecord ?? Array.Empty<string>();
            rows.Add(header);
            while (await csv.ReadAsync())
            {
                var fields = header.Select(h => csv.GetField(h) ?? string.Empty).ToArray();
                rows.Add(fields);
            }
        }
        if (rows.Count < 2)
            return Array.Empty<TransactionDraft>();
        // Verwijder dubbele declaratie van headerMap
        var drafts = new List<TransactionDraft>();
        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            // Verwijder check op row.Length != headerMap.Length
            string Get(string col)
            {
                if (headerMapDict != null && headerMapDict.TryGetValue(col, out var idx))
                    return idx >= 0 && idx < row.Length ? row[idx] : string.Empty;
                var idx2 = Array.FindIndex(headerMap, h => string.Equals(h, col, StringComparison.OrdinalIgnoreCase));
                return idx2 >= 0 && idx2 < row.Length ? row[idx2] : string.Empty;
            }
            var dateStr = Get("Datum").Trim('"');
            var rekening = Get("Je rekening");
            var tegenrekening = Get("Van / naar");
            var naam = Get("Naam");
            var bedragStr = Get("Bedrag bij / af");
            var omschrijving = Get("Omschrijving");
            var valuta = Get("Valuta");
            var saldoVoorStr = Get("Saldo voor boeking");
            var volgnummer = Get("Volgnummer");
            var afschriftnummer = Get("Afschriftnummer");
            if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(bedragStr))
                continue;
            if (!DateOnly.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bookingDate))
                continue;
            var bedragNorm = bedragStr.Replace("\"", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
            if (!decimal.TryParse(bedragNorm, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                continue;
            decimal? saldoVoor = null;
            decimal? resultingBalance = null;
            if (!string.IsNullOrWhiteSpace(saldoVoorStr))
            {
                var saldoNorm = saldoVoorStr.Replace("\"", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(saldoNorm, NumberStyles.Number, CultureInfo.InvariantCulture, out var s))
                {
                    saldoVoor = s;
                    resultingBalance = saldoVoor + amount;
                }
            }
            var sourceReference = $"{bookingDate:yyyy-MM-dd}|{amount.ToString(CultureInfo.InvariantCulture)}|{rekening}|{naam}|{omschrijving}|{volgnummer}";
            var draft = new TransactionDraft
            {
                SourceSystem = SourceSystem,
                SourceReference = sourceReference,
                BookingDate = bookingDate,
                ValueDate = bookingDate,
                Amount = amount,
                Currency = string.IsNullOrWhiteSpace(valuta) ? "EUR" : valuta,
                ResultingBalance = resultingBalance,
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
        // Debug: controleer op dubbele SourceReference in drafts
        var duplicateRefs = drafts.GroupBy(d => d.SourceReference)
            .Where(g => g.Count() > 1)
            .Select(g => new { SourceReference = g.Key, Count = g.Count() })
            .ToList();
        if (duplicateRefs.Any())
        {
            // Log of gooi een exception voor debug-doeleinden
            var msg = $"DUBBELE SourceReference(s) gevonden:\n" + string.Join("\n", duplicateRefs.Select(d => $"{d.SourceReference} (x{d.Count})"));
            throw new InvalidOperationException(msg);
        }
        return drafts;
    }
}
