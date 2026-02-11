using System.Text;

namespace Finance.Application.Validators;

public static class CsvHeaderValidator
{
    private static readonly string[][] IngHeadersVariants = new[]
    {
        new[] { "Date","Name / Description","Account","Counterparty","Code","Debit/credit","Amount (EUR)","Transaction type","Notifications","Resulting balance","Tag" },
        new[] { "Datum","Naam / Omschrijving","Rekening","Tegenrekening","Code","Af Bij","Bedrag (EUR)","Mutatiesoort","Mededelingen","Saldo na trn","Tag" },
        new[] { "Datum","Naam / Omschrijving","Rekening","Tegenrekening","Code","Af Bij","Bedrag (EUR)","Transactietype","Mededelingen","Saldo na mutatie","Tag" },
        new[] { "Datum","Naam / Omschrijving","Rekening","Tegenrekening","Code","Af Bij","Bedrag (EUR)","Mutatiesoort","Mededelingen","Saldo na mutatie","Tag" },
    };

    private static readonly string[][] IngSpaarHeadersVariants = new[]
    {
        new[] { "Datum","Omschrijving","Rekening","Rekening naam","Tegenrekening","Af Bij","Bedrag","Valuta","Mutatiesoort","Mededelingen","Saldo na mutatie" },
    };

    private static readonly string[] AsnRequiredHeaders = new[]
    {
        "Datum","Je rekening","Van / naar","Naam","Omschrijving","Bedrag bij / af","Valuta","Saldo voor boeking"
    };

    private static readonly string[] AsnSpaarRequiredHeaders = new[]
    {
        "Datum","Je rekening","Van / naar","Naam","Omschrijving","Bedrag bij / af"
    };

    public static bool Validate(Stream fileStream, string type, out string error)
    {
        error = string.Empty;
        fileStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            error = "Bestand bevat geen header.";
            return false;
        }
        var delimiter = headerLine.Contains(';') ? ';' : ',';
        var actual = headerLine.Split(delimiter).Select(h => h.Trim('"')).ToArray();
        if (type == "ASN")
        {
            // ASN: alleen checken of alle vereiste kolommen aanwezig zijn, volgorde en extra kolommen negeren
            foreach (var required in AsnRequiredHeaders)
            {
                if (!actual.Any(a => string.Equals(a, required, StringComparison.OrdinalIgnoreCase)))
                {
                    error = $"Kolom '{required}' ontbreekt in ASN header.";
                    return false;
                }
            }
            return true;
        }
        if (type == "ASN_SPAAR")
        {
            foreach (var required in AsnSpaarRequiredHeaders)
            {
                if (!actual.Any(a => string.Equals(a, required, StringComparison.OrdinalIgnoreCase)))
                {
                    error = $"Kolom '{required}' ontbreekt in ASN Spaar header.";
                    return false;
                }
            }
            return true;
        }
        string[][] variants = type switch
        {
            "ING" => IngHeadersVariants,
            "ING_SPAAR" => IngSpaarHeadersVariants,
            _ => Array.Empty<string[]>()
        };
        foreach (var expected in variants)
        {
            if (actual.Length != expected.Length)
                continue;
            bool allMatch = true;
            for (int i = 0; i < expected.Length; i++)
            {
                if (!string.Equals(actual[i], expected[i], StringComparison.OrdinalIgnoreCase))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch)
                return true;
        }
        error = $"Header komt niet overeen met een van de verwachte varianten voor {type}.";
        return false;
    }
}
