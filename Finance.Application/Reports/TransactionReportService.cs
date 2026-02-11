using Finance.Domain.Repositories;
using TransactionFilter = Finance.Domain.Repositories.TransactionFilter;

namespace Finance.Application.Reports;

public sealed class TransactionReportService : ITransactionReportService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionLinkRepository _transactionLinkRepository;

    public TransactionReportService(ITransactionRepository transactionRepository, ITransactionLinkRepository transactionLinkRepository)
    {
        _transactionRepository = transactionRepository;
        _transactionLinkRepository = transactionLinkRepository;
    }

    public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(GetTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        var filter = new TransactionFilter
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            CategoryId = query.CategoryId,
            MinAmount = query.MinAmount,
            MaxAmount = query.MaxAmount,
            SearchText = query.SearchText,
            Name = query.Name, // <-- voeg Name toe aan filter
            AccountIds = query.AccountIds
        };

        var transactions = await _transactionRepository.GetByFilterAsync(filter, cancellationToken);
        
        // Filter op accountIds indien meegegeven
        if (query.AccountIds != null && query.AccountIds.Length > 0)
        {
            transactions = transactions.Where(t => t.AccountId != null && query.AccountIds.Contains((Guid)t.AccountId)).ToList();
        }
        // Filter op accountId indien meegegeven
        else if (query.AccountId.HasValue)
        {
            transactions = transactions.Where(t => t.AccountId == query.AccountId.Value).ToList();
        }

        var transactionDtos = new List<TransactionDto>();
        foreach (var t in transactions)
        {
            var links = await _transactionLinkRepository.GetLinksForTransactionAsync(t.TransactionId, cancellationToken);
            Guid? linkedId = links.FirstOrDefault(l => l.TransactionId1 == t.TransactionId)?.TransactionId2
                ?? links.FirstOrDefault(l => l.TransactionId2 == t.TransactionId)?.TransactionId1;
            transactionDtos.Add(new TransactionDto
            {
                TransactionId = t.TransactionId,
                BookingDate = t.BookingDate,
                Amount = t.Amount,
                Currency = t.Currency,
                Description = t.Description,
                CategoryId = t.CategoryId,
                CategoryName = null,
                ResultingBalance = t.ResultingBalance,
                TransactionType = t.TransactionType,
                Name = t.Name,
                AccountType = t.SourceSystem.Equals("ING_SPAAR", StringComparison.OrdinalIgnoreCase) ? "Spaar" : "Normaal",
                AccountId = t.AccountId,
                CounterpartyAccountId = t.CounterpartyAccountId,
                LinkedTransactionId = linkedId
            });
        }
        return transactionDtos;
    }
}
