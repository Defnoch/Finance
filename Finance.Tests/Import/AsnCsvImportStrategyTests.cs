using System.Globalization;
using System.Text;
using Finance.Domain.Import;
using Finance.Infrastructure.Import;
using NUnit.Framework;

namespace Finance.Tests.Import;

[TestFixture]
public class AsnCsvImportStrategyTests
{
    private static string SampleCsv = @"Datum;Je rekening;Tegenrekening;Naam;Omschrijving;Bedrag bij / af;Valuta;Saldo voor boeking;Van / naar
18-07-2023;NL12ASN0123456789;NL34RABO1234567890;J. Janssen;Boodschappen;-12,34;EUR;1000,00;NL34RABO1234567890
19-07-2023;NL12ASN0123456789;NL56INGB9876543210;A. de Vries;Salaris;1500,00;EUR;987,66;NL56INGB9876543210
";

    [Test]
    public async Task ParseAsync_ValidAsnCsv_ReturnsCorrectDrafts()
    {
        var strategy = new AsnCsvImportStrategy();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SampleCsv));

        var drafts = await strategy.ParseAsync(stream, "asn_account.csv");

        Assert.That(drafts, Has.Count.EqualTo(2));
        Assert.That(drafts[0].BookingDate, Is.EqualTo(new DateOnly(2023, 7, 18)));
        Assert.That(drafts[0].Amount, Is.EqualTo(-12.34m));
        Assert.That(drafts[0].AccountIdentifier, Is.EqualTo("NL12ASN0123456789"));
        Assert.That(drafts[0].CounterpartyIdentifier, Is.EqualTo("NL34RABO1234567890"));
        Assert.That(drafts[0].Name, Is.EqualTo("J. Janssen"));
        Assert.That(drafts[0].Description, Is.EqualTo("Boodschappen"));
        Assert.That(drafts[0].Currency, Is.EqualTo("EUR"));
        Assert.That(drafts[0].ResultingBalance, Is.EqualTo(987.66m));
        Assert.That(drafts[1].Amount, Is.EqualTo(1500.00m));
        Assert.That(drafts[1].Name, Is.EqualTo("A. de Vries"));
    }

    [Test]
    public async Task ParseAsync_InvalidOrEmptyCsv_ReturnsEmpty()
    {
        var strategy = new AsnCsvImportStrategy();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var drafts = await strategy.ParseAsync(stream, "asn_account.csv");
        Assert.That(drafts, Is.Empty);
    }
}
