using Finance.Domain.Entities;
using Finance.Domain.Repositories;

namespace Finance.Application.Import;

public interface IAsnTransactionLinkerService
{
    Task LinkTransactionsAsync(IEnumerable<Transaction> newTransactions, CancellationToken cancellationToken = default);
}

public sealed class AsnTransactionLinkerService : IAsnTransactionLinkerService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionLinkRepository _transactionLinkRepository;

    public AsnTransactionLinkerService(
        ITransactionRepository transactionRepository,
        ITransactionLinkRepository transactionLinkRepository)
    {
        _transactionRepository = transactionRepository;
        _transactionLinkRepository = transactionLinkRepository;
    }

    public async Task LinkTransactionsAsync(IEnumerable<Transaction> newTransactions, CancellationToken cancellationToken = default)
    {
        var spaarTrans = newTransactions.Where(t => t.SourceSystem == "ASN_SPAAR").ToList();
        if (!spaarTrans.Any()) return;
        var allAsn = await _transactionRepository.GetBySourceSystemAsync("ASN", cancellationToken);
        foreach (var spaar in spaarTrans)
        {
            var match = allAsn.FirstOrDefault(t => t.SourceReference == spaar.SourceReference);
            if (match == null) continue;
            bool exists = await _transactionLinkRepository.ExistsAsync(spaar.TransactionId, match.TransactionId, cancellationToken);
            if (!exists)
            {
                var link = new TransactionLink
                {
                    TransactionId1 = spaar.TransactionId,
                    TransactionId2 = match.TransactionId,
                    LinkedAt = DateTime.UtcNow
                };
                await _transactionLinkRepository.AddAsync(link, cancellationToken);
            }
        }
    }
}
