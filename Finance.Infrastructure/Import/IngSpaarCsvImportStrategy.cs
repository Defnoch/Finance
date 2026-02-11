using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Finance.Domain.Import;

namespace Finance.Infrastructure.Import;

/// <summary>
/// ING Spaarrekening CSV importstrategie afgestemd op het formaat van de spaarrekening CSV.
/// Headers:
/// Datum;"Omschrijving";"Rekening";"Rekening naam";"Tegenrekening";"Af Bij";"Bedrag";"Valuta";"Mutatiesoort";"Mededelingen";"Saldo na mutatie"
/// </summary>
public sealed class IngSpaarCsvImportStrategy : IBankImportStrategy
{
    public string SourceSystem => "ING_SPAAR";

    public bool CanHandle(string sourceSystem, string fileName)
    {
        // Alleen op sourceSystem selecteren
        return sourceSystem.Equals("ING_SPAAR", StringComparison.OrdinalIgnoreCase);
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

        // Mapping van kolomnamen (NL/EN) naar interne veldnamen
        var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Engels
            {"Date", "Date"},
            {"Description", "Description"},
            {"Account", "Account"},
            {"Account name", "AccountName"},
            {"Counterparty", "Counterparty"},
            {"Debit/credit", "DebitCredit"},
            {"Amount", "Amount"},
            {"Currency", "Currency"},
            {"Transaction type", "TransactionType"},
            {"Notifications", "Notifications"},
            {"Resulting balance", "ResultingBalance"},
            // Nederlands
            {"Datum", "Date"},
            {"Omschrijving", "Description"},
            {"Rekening", "Account"},
            {"Rekening naam", "AccountName"},
            {"Tegenrekening", "Counterparty"},
            {"Af Bij", "DebitCredit"},
            {"Bedrag", "Amount"},
            {"Valuta", "Currency"},
            {"Mutatiesoort", "TransactionType"},
            {"Mededelingen", "Notifications"},
            {"Saldo na mutatie", "ResultingBalance"},
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

            var dateStr = Get("Date");
            var description = Get("Description") ?? string.Empty;
            var account = Get("Account") ?? string.Empty;
            var accountName = Get("AccountName") ?? string.Empty;
            var counterparty = Get("Counterparty");
            var afBij = Get("DebitCredit");
            var amountStrRaw = Get("Amount");
            var currency = Get("Currency") ?? "EUR";
            var transactionType = Get("TransactionType") ?? string.Empty;
            var notifications = Get("Notifications");
            var resultingBalanceStrRaw = Get("ResultingBalance");

            if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(amountStrRaw))
                continue;

            // Datum: "2020-12-30" of "30-12-2020" → DateOnly
            DateOnly bookingDate;
            if (!DateOnly.TryParseExact(dateStr.Trim('"'), new[] { "yyyy-MM-dd", "dd-MM-yyyy", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out bookingDate))
                continue;

            // Bedrag: "72,48" → decimal, teken o.b.v. Af Bij
            var amountStr = amountStrRaw
                .Replace("€", string.Empty)
                .Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace(',', '.');

            if (!decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                continue;

            if (string.Equals(afBij, "Af", StringComparison.OrdinalIgnoreCase))
            {
                amount = -Math.Abs(amount);
            }
            else if (string.Equals(afBij, "Bij", StringComparison.OrdinalIgnoreCase))
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

            var sourceReference = $"{bookingDate:yyyy-MM-dd}|{amount.ToString(CultureInfo.InvariantCulture)}|{account}|{description}|{notifications}";

            var draft = new TransactionDraft
            {
                SourceSystem = SourceSystem,
                SourceReference = sourceReference,
                BookingDate = bookingDate,
                ValueDate = null,
                Amount = amount,
                Currency = currency,
                ResultingBalance = resultingBalance,
                TransactionType = transactionType,
                Notifications = notifications,
                AccountIdentifier = account,
                CounterpartyIdentifier = string.IsNullOrWhiteSpace(counterparty) ? null : counterparty,
                Description = description,
                RawData = null,
                Name = accountName
            };

            drafts.Add(draft);
        }

        return drafts;
    }
}
