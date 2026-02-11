using System.Text;
using Finance.Application.Import;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Import;
using NSubstitute;
using NUnit.Framework;

namespace Finance.Tests.Import;

[TestFixture]
public class TransactionLinkerIntegrationTests
{
    [Test]
    public async Task AsnSpaar_KoppeltAanNormaleTransactie_ExacteMatch()
    {
        // Arrange: ASN normaal en ASN spaar draft met matchende data
        var bookingDate = new DateOnly(2023, 12, 25);
        var rekeningNormaal = "NL30ASNB8822000854";
        var rekeningSpaar = "NL30ASNB8822000854";
        var bedragNormaal = 1.00m;
        var bedragSpaar = -1.00m;
        var omschrijving = "Overboeking naar spaar";

        var normaal = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            BookingDate = bookingDate,
            Amount = bedragNormaal,
            AccountId = Guid.NewGuid(),
            CounterpartyAccountId = null, // wordt hieronder gezet
            Description = omschrijving
        };
        var spaar = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            BookingDate = bookingDate,
            Amount = bedragSpaar,
            AccountId = Guid.NewGuid(),
            CounterpartyAccountId = null, // wordt hieronder gezet
            Description = omschrijving
        };
        // Zet CounterpartyAccountId zodat ze naar elkaar verwijzen
        normaal.CounterpartyAccountId = spaar.AccountId;
        spaar.CounterpartyAccountId = normaal.AccountId;
        var allAccounts = new List<Account>
        {
            new Account { AccountId = (Guid)normaal.AccountId!, AccountIdentifier = rekeningNormaal, Provider = "ASN", AccountType = "Normaal" },
            new Account { AccountId = (Guid)spaar.AccountId!, AccountIdentifier = rekeningSpaar, Provider = "ASN", AccountType = "Spaar" }
        };
        var allTransactions = new List<Transaction> { normaal, spaar };

        var transactionRepository = Substitute.For<ITransactionRepository>();
        var transactionLinkRepository = Substitute.For<ITransactionLinkRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        transactionRepository.GetByFilterAsync(Arg.Any<Finance.Domain.Repositories.TransactionFilter>(), Arg.Any<CancellationToken>()).Returns(allTransactions);
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allAccounts);
        transactionLinkRepository.ExistsAsync(spaar.TransactionId, normaal.TransactionId, Arg.Any<CancellationToken>()).Returns(false);
        transactionLinkRepository.ExistsAsync(normaal.TransactionId, spaar.TransactionId, Arg.Any<CancellationToken>()).Returns(false);

        var linker = new TransactionLinkerService(transactionRepository, transactionLinkRepository, accountRepository);

        // Act
        await linker.LinkTransactionsAsync(new[] { spaar });

        // Assert: er wordt een link aangemaakt tussen spaar en normaal
        await transactionLinkRepository.Received(1).AddAsync(
            Arg.Is<TransactionLink>(l =>
                (l.TransactionId1 == spaar.TransactionId && l.TransactionId2 == normaal.TransactionId) ||
                (l.TransactionId1 == normaal.TransactionId && l.TransactionId2 == spaar.TransactionId)
            ),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AsnSpaar_KoppeltNietAlsGeenMatch()
    {
        // Arrange: geen matchende normale transactie
        var spaar = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            BookingDate = new DateOnly(2023, 12, 25),
            Amount = -1.00m,
            AccountId = Guid.NewGuid(),
            CounterpartyAccountId = null,
            Description = "Overboeking naar spaar"
        };
        var allAccounts = new List<Account>
        {
            new Account { AccountId = (Guid)spaar.AccountId!, AccountIdentifier = "NL30ASNB8822000854", Provider = "ASN", AccountType = "Spaar" }
        };
        var allTransactions = new List<Transaction> { spaar };
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var transactionLinkRepository = Substitute.For<ITransactionLinkRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        transactionRepository.GetByFilterAsync(Arg.Any<Finance.Domain.Repositories.TransactionFilter>(), Arg.Any<CancellationToken>()).Returns(allTransactions);
        accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allAccounts);
        var linker = new TransactionLinkerService(transactionRepository, transactionLinkRepository, accountRepository);

        // Act
        await linker.LinkTransactionsAsync(new[] { spaar });

        // Assert: er wordt geen link aangemaakt
        await transactionLinkRepository.DidNotReceive().AddAsync(Arg.Any<TransactionLink>(), Arg.Any<CancellationToken>());
    }
}
