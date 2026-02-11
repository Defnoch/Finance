using Finance.Application.Import;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using NSubstitute;
using NUnit.Framework;

namespace Finance.Tests.Import
{
    [TestFixture]
    public class TransactionLinkerServiceTests
    {
        private ITransactionRepository? _transactionRepository;
        private ITransactionLinkRepository? _transactionLinkRepository;
        private IAccountRepository? _accountRepository;
        private TransactionLinkerService? _service;

        [SetUp]
        public void SetUp()
        {
            _transactionRepository = Substitute.For<ITransactionRepository>();
            _transactionLinkRepository = Substitute.For<ITransactionLinkRepository>();
            _accountRepository = Substitute.For<IAccountRepository>();
            _service = new TransactionLinkerService(
                _transactionRepository,
                _transactionLinkRepository,
                _accountRepository);
        }

        [Test]
        public async Task LinkTransactionsAsync_LinksMatchingTransactions()
        {
            // Arrange
            var account1 = new Account { AccountId = Guid.NewGuid(), AccountIdentifier = "NL01BANK0123456789", Provider = "ING", AccountType = "Normaal" };
            var account2 = new Account { AccountId = Guid.NewGuid(), AccountIdentifier = "NL02BANK9876543210", Provider = "ING", AccountType = "Spaar" };
            var tx1 = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = 100,
                BookingDate = new DateOnly(2024, 1, 1),
                AccountId = account1.AccountId,
                CounterpartyAccountId = account2.AccountId
            };
            var tx2 = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = -100, // Moet tegengesteld zijn aan tx1.Amount
                BookingDate = new DateOnly(2024, 1, 1),
                AccountId = account2.AccountId,
                CounterpartyAccountId = account1.AccountId
            };
            var allAccounts = new List<Account> { account1, account2 };
            var allTransactions = new List<Transaction> { tx1, tx2 };

            _accountRepository!.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allAccounts);
            _transactionRepository!.GetByFilterAsync(Arg.Any<TransactionFilter>(), Arg.Any<CancellationToken>()).Returns(allTransactions);
            _transactionLinkRepository!.ExistsAsync(tx1.TransactionId, tx2.TransactionId, Arg.Any<CancellationToken>()).Returns(false);
            _transactionLinkRepository!.ExistsAsync(tx2.TransactionId, tx1.TransactionId, Arg.Any<CancellationToken>()).Returns(false);

            // Act
            await _service!.LinkTransactionsAsync(new[] { tx1, tx2 });

            // Assert
            await _transactionLinkRepository.Received(2).AddAsync(
                Arg.Is<TransactionLink>(l =>
                    (l.TransactionId1 == tx1.TransactionId && l.TransactionId2 == tx2.TransactionId) ||
                    (l.TransactionId1 == tx2.TransactionId && l.TransactionId2 == tx1.TransactionId)
                ),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task LinkTransactionsAsync_DoesNotLinkIfAlreadyExists()
        {
            // Arrange
            var account1 = new Account { AccountId = Guid.NewGuid(), AccountIdentifier = "NL01BANK0123456789", Provider = "ING", AccountType = "Normaal" };
            var account2 = new Account { AccountId = Guid.NewGuid(), AccountIdentifier = "NL02BANK9876543210", Provider = "ING", AccountType = "Spaar" };
            var tx1 = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = 100,
                BookingDate = new DateOnly(2024, 1, 1),
                AccountId = account1.AccountId,
                CounterpartyAccountId = account2.AccountId
            };
            var tx2 = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = -100, // Moet tegengesteld zijn aan tx1.Amount
                BookingDate = new DateOnly(2024, 1, 1),
                AccountId = account2.AccountId,
                CounterpartyAccountId = account1.AccountId
            };
            var allAccounts = new List<Account> { account1, account2 };
            var allTransactions = new List<Transaction> { tx1, tx2 };

            _accountRepository!.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allAccounts);
            _transactionRepository!.GetByFilterAsync(Arg.Any<TransactionFilter>(), Arg.Any<CancellationToken>()).Returns(allTransactions);
            _transactionLinkRepository!.ExistsAsync(tx1.TransactionId, tx2.TransactionId, Arg.Any<CancellationToken>()).Returns(true);
            _transactionLinkRepository!.ExistsAsync(tx2.TransactionId, tx1.TransactionId, Arg.Any<CancellationToken>()).Returns(true);

            // Act
            await _service!.LinkTransactionsAsync(new[] { tx1, tx2 });

            // Assert
            await _transactionLinkRepository.DidNotReceive().AddAsync(Arg.Any<TransactionLink>(), Arg.Any<CancellationToken>());
        }
    }
}
