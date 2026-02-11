using System.Globalization;
using System.Text;
using Finance.Infrastructure.Import;
using NUnit.Framework;

namespace Finance.Tests.Import;

[TestFixture]
public class AsnSpaarCsvImportStrategyTests
{
    private static string SampleCsv = @"Datum;Je rekening;Van / naar;Naam;Omschrijving;Bedrag bij / af
25-12-2023;NL30ASNB8822000854;NL30ASNB8822000854;ASN Spaar;Overboeking naar spaar;-1,00
25-12-2023;NL30ASNB8822000854;NL30ASNB8822000854;ASN Spaar;Overboeking naar spaar;-2,00
";

    [Test]
    public async Task ParseAsync_ValidAsnSpaarCsv_ReturnsCorrectDrafts()
    {
        var strategy = new AsnSpaarCsvImportStrategy();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleCsv));
        var drafts = await strategy.ParseAsync(stream, "asn_spaar.csv");
        Assert.That(drafts, Has.Count.EqualTo(2));
        Assert.That(drafts[0].BookingDate, Is.EqualTo(new DateOnly(2023, 12, 25)));
        Assert.That(drafts[0].Amount, Is.EqualTo(-1.00m));
        Assert.That(drafts[0].AccountIdentifier, Is.EqualTo("NL30ASNB8822000854"));
        Assert.That(drafts[0].Description, Is.EqualTo("Overboeking naar spaar"));
    }

    [Test]
    public async Task ParseAsync_InvalidOrEmptyCsv_ReturnsEmpty()
    {
        var strategy = new AsnSpaarCsvImportStrategy();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var drafts = await strategy.ParseAsync(stream, "asn_spaar.csv");
        Assert.That(drafts, Is.Empty);
    }
}
