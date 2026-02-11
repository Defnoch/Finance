using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using TransactionFilter = Finance.Domain.Repositories.TransactionFilter;

namespace Finance.Application.Import;

public interface ITransactionLinkerService
{
    Task LinkTransactionsAsync(IEnumerable<Transaction> newTransactions, CancellationToken cancellationToken = default);
}

public sealed class TransactionLinkerService : ITransactionLinkerService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionLinkRepository _transactionLinkRepository;
    private readonly IAccountRepository _accountRepository;

    public TransactionLinkerService(
        ITransactionRepository transactionRepository,
        ITransactionLinkRepository transactionLinkRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _transactionLinkRepository = transactionLinkRepository;
        _accountRepository = accountRepository;
    }

    public async Task LinkTransactionsAsync(IEnumerable<Transaction> newTransactions, CancellationToken cancellationToken = default)
    {
        var newList = newTransactions.ToList();
        if (!newList.Any()) return;

        // Haal alle bestaande transacties op van andere rekeningtypes
        var allAccounts = await _accountRepository.GetAllAsync(cancellationToken);
        var allTransactions = await _transactionRepository.GetByFilterAsync(new TransactionFilter(), cancellationToken);

        foreach (var newTx in newList)
        {
            // Zoek alleen naar transacties van een ander accounttype
            var newAccount = allAccounts.FirstOrDefault(a => a.AccountId == newTx.AccountId);
            if (newAccount == null) continue;
            var otherType = newAccount.AccountType == "Spaar" ? "Normaal" : "Spaar";
            var candidates = allTransactions.Where(t =>
                t.TransactionId != newTx.TransactionId &&
                t.AccountId != null &&
                allAccounts.Any(a => a.AccountId == t.AccountId && a.AccountType == otherType)
            );

            foreach (var candidate in candidates)
            {
                // Matching criteria: bedrag (tegengesteld), datum (gelijk of +/- 1 dag), tegenrekening
                if (Math.Abs(candidate.Amount + newTx.Amount) < 0.01m &&
                    Math.Abs((candidate.BookingDate.ToDateTime(TimeOnly.MinValue) - newTx.BookingDate.ToDateTime(TimeOnly.MinValue)).TotalDays) <= 1 &&
                    (candidate.CounterpartyAccountId == newTx.AccountId || newTx.CounterpartyAccountId == candidate.AccountId))
                {
                    // Bestaat de link al?
                    bool exists = await _transactionLinkRepository.ExistsAsync(newTx.TransactionId, candidate.TransactionId, cancellationToken);
                    if (!exists)
                    {
                        var link = new TransactionLink
                        {
                            TransactionLinkId = Guid.NewGuid(),
                            TransactionId1 = newTx.TransactionId,
                            TransactionId2 = candidate.TransactionId,
                            LinkedAt = DateTime.UtcNow
                        };
                        await _transactionLinkRepository.AddAsync(link, cancellationToken);
                    }
                }
            }
        }
    }
}
