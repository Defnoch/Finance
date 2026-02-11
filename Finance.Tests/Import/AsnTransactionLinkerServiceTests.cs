using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Finance.Application.Import;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using NSubstitute;
using NUnit.Framework;

namespace Finance.Tests.Import;

[TestFixture]
public class AsnTransactionLinkerServiceTests
{
    [Test]
    public async Task LinkTransactionsAsync_LinksOnSourceReference()
    {
        // Arrange
        var sourceRef = "2023-07-19|-1000.00|NL30ASNB8822000854|ASN Ideaalsparen|Referentie: a30a8577-40bc-4b89-8e3d-40a28946be|243857";
        var asn = new Transaction { TransactionId = Guid.NewGuid(), SourceSystem = "ASN", SourceReference = sourceRef };
        var spaar = new Transaction { TransactionId = Guid.NewGuid(), SourceSystem = "ASN_SPAAR", SourceReference = sourceRef };
        var allAsn = new List<Transaction> { asn };
        var repo = Substitute.For<ITransactionRepository>();
        var linkRepo = Substitute.For<ITransactionLinkRepository>();
        repo.GetBySourceSystemAsync("ASN", Arg.Any<CancellationToken>()).Returns(allAsn);
        linkRepo.ExistsAsync(spaar.TransactionId, asn.TransactionId, Arg.Any<CancellationToken>()).Returns(false);
        var service = new AsnTransactionLinkerService(repo, linkRepo);

        // Act
        await service.LinkTransactionsAsync(new[] { spaar });

        // Assert
        await linkRepo.Received(1).AddAsync(
            Arg.Is<TransactionLink>(l =>
                (l.TransactionId1 == spaar.TransactionId && l.TransactionId2 == asn.TransactionId) ||
                (l.TransactionId1 == asn.TransactionId && l.TransactionId2 == spaar.TransactionId)
            ),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LinkTransactionsAsync_DoesNotLinkIfNoMatch()
    {
        // Arrange
        var spaar = new Transaction { TransactionId = Guid.NewGuid(), SourceSystem = "ASN_SPAAR", SourceReference = "ref1" };
        var repo = Substitute.For<ITransactionRepository>();
        var linkRepo = Substitute.For<ITransactionLinkRepository>();
        repo.GetBySourceSystemAsync("ASN", Arg.Any<CancellationToken>()).Returns(new List<Transaction>());
        var service = new AsnTransactionLinkerService(repo, linkRepo);

        // Act
        await service.LinkTransactionsAsync(new[] { spaar });

        // Assert
        await linkRepo.DidNotReceive().AddAsync(Arg.Any<TransactionLink>(), Arg.Any<CancellationToken>());
    }
}
