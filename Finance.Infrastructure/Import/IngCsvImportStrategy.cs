using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Finance.Domain.Import;

namespace Finance.Infrastructure.Import;

/// <summary>
/// ING CSV importstrategie afgestemd op het formaat van Upload/upload.csv.
/// Headers:
/// "Date";"Name / Description";"Account";"Counterparty";"Code";"Debit/credit";"Amount (EUR)";"Transaction type";"Notifications";"Resulting balance";"Tag"
/// </summary>
public sealed class IngCsvImportStrategy : IBankImportStrategy
{
    public string SourceSystem => "ING";

    public bool CanHandle(string sourceSystem, string fileName)
    {
        // Alleen op sourceSystem selecteren
        return sourceSystem.Equals("ING", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<TransactionDraft>> ParseAsync(
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (csvStream == Stream.Null)
            return Array.Empty<TransactionDraft>();

        // CsvHelper kan direct over de stream lezen; we gebruiken UTF8 met BOM-detectie.
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            BadDataFound = null, // overslaan van slechte records
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };

        using var csv = new CsvReader(reader, config);

        // Lezen van header
        if (!await csv.ReadAsync() || !csv.ReadHeader())
            return Array.Empty<TransactionDraft>();

        // Mapping van kolomnamen (NL/EN) naar interne veldnamen
        var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Engels
            {"Date", "Date"},
            {"Name / Description", "Description"},
            {"Account", "Account"},
            {"Counterparty", "Counterparty"},
            {"Debit/credit", "DebitCredit"},
            {"Amount (EUR)", "Amount"},
            {"Transaction type", "TransactionType"},
            {"Notifications", "Notifications"},
            {"Resulting balance", "ResultingBalance"},
            // Nederlands
            {"Datum", "Date"},
            {"Naam / Omschrijving", "Description"},
            {"Rekening", "Account"},
            {"Tegenrekening", "Counterparty"},
            {"Af Bij", "DebitCredit"},
            {"Bedrag (EUR)", "Amount"},
            {"Transactietype", "TransactionType"},
            {"Mutatiesoort", "TransactionType"}, // extra NL variant
            {"Mededelingen", "Notifications"},
            {"Saldo na trn", "ResultingBalance"},
            {"Saldo na mutatie", "ResultingBalance"}, // extra NL variant
        };

        // Bepaal de daadwerkelijke header mapping op basis van de ingelezen header
        var csvHeader = csv.HeaderRecord ?? Array.Empty<string>();
        var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in csvHeader)
        {
            if (headerMap.TryGetValue(col, out var internalName))
            {
                columnMap[internalName] = col;
            }
        }

        var drafts = new List<TransactionDraft>();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? Get(string internalName)
            {
                if (columnMap.TryGetValue(internalName, out var colName))
                {
                    try { return csv.GetField(colName); } catch { return null; }
                }
                return null;
            }

            var dateStr       = Get("Date");
            var description   = Get("Description") ?? string.Empty;
            var account       = Get("Account") ?? string.Empty;
            var counterparty  = Get("Counterparty");
            var debitCredit   = Get("DebitCredit");
            var amountStrRaw  = Get("Amount");
            var transactionType = Get("TransactionType") ?? string.Empty;
            var notifications = Get("Notifications");
            var resultingBalanceStrRaw = Get("ResultingBalance");

            if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(amountStrRaw))
                continue;

            // Datum: "20201230" → DateOnly (yyyyMMdd)
            if (!DateOnly.TryParseExact(dateStr.Trim('"'), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bookingDate))
                continue;

            // Bedrag: "72,48" → decimal, teken o.b.v. Debit/Credit
            var amountStr = amountStrRaw
                .Replace("€", string.Empty)
                .Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace(',', '.');

            if (!decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                continue;

            if (string.Equals(debitCredit, "Debit", StringComparison.OrdinalIgnoreCase))
            {
                amount = -Math.Abs(amount);
            }
            else if (string.Equals(debitCredit, "Credit", StringComparison.OrdinalIgnoreCase))
            {
                amount = Math.Abs(amount);
            }

            decimal? resultingBalance = null;
            if (!string.IsNullOrWhiteSpace(resultingBalanceStrRaw))
            {
                var rbStr = resultingBalanceStrRaw
                    .Replace("€", string.Empty)
                    .Replace(" ", string.Empty)
                    .Replace(".", string.Empty)
                    .Replace(',', '.');
                if (decimal.TryParse(rbStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var rb))
                {
                    resultingBalance = rb;
                }
            }

            // SourceReference: Datum + Bedrag + Rekening + Omschrijving
            var sourceReference = $"{bookingDate:yyyy-MM-dd}|{amount.ToString(CultureInfo.InvariantCulture)}|{account}|{description}|{notifications}";

            // Extract Name and Description from Notifications if present
            string parsedName = string.Empty;
            string parsedDescription = description;
            if (!string.IsNullOrWhiteSpace(notifications))
            {
                var nameMatch = System.Text.RegularExpressions.Regex.Match(notifications, @"Name:(.*?)Description:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                    parsedName = nameMatch.Groups[1].Value.Trim();
                // Robuuste regex voor Description: pak alles tot eerstvolgende sleutel of einde
                var descMatch = System.Text.RegularExpressions.Regex.Match(notifications, @"Description:(.*?)(IBAN:|Reference:|Value date:|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (descMatch.Success)
                    parsedDescription = descMatch.Groups[1].Value.Trim();
            }

            var draft = new TransactionDraft
            {
                SourceSystem = SourceSystem,
                SourceReference = sourceReference,
                BookingDate = bookingDate,
                ValueDate = null,
                Amount = amount,
                Currency = "EUR",
                ResultingBalance = resultingBalance,
                TransactionType = transactionType,
                Notifications = notifications,
                AccountIdentifier = account,
                CounterpartyIdentifier = string.IsNullOrWhiteSpace(counterparty) ? null : counterparty,
                Description = parsedDescription,
                RawData = null,
                Name = parsedName
            };

            drafts.Add(draft);
        }

        return drafts;
    }
}
