using Finance.Application.Reports;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly ITransactionReportService _transactionReportService;
    private readonly IMonthlySummaryService _monthlySummaryService;
    private readonly ICategorySummaryService _categorySummaryService;
    private readonly IAccountFiscalYearRepository _accountFiscalYearRepository;

    public ReportsController(
        ITransactionReportService transactionReportService,
        IMonthlySummaryService monthlySummaryService,
        ICategorySummaryService categorySummaryService,
        IAccountFiscalYearRepository accountFiscalYearRepository)
    {
        _transactionReportService = transactionReportService;
        _monthlySummaryService = monthlySummaryService;
        _categorySummaryService = categorySummaryService;
        _accountFiscalYearRepository = accountFiscalYearRepository;
    }

    [ProducesResponseType(typeof(IReadOnlyList<TransactionDto>), StatusCodes.Status200OK)]
    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetTransactions(
        [FromQuery] Guid[]? accountIds,
        [FromQuery] int? year,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? searchText,
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        if (accountIds != null && accountIds.Length == 0)
            return BadRequest("accountIds mag niet leeg zijn als het wordt meegegeven.");
        var query = new GetTransactionsQuery
        {
            AccountIds = accountIds,
            FromDate = year.HasValue ? new DateOnly(year.Value, 1, 1) : from,
            ToDate = year.HasValue ? new DateOnly(year.Value, 12, 31) : to,
            CategoryId = categoryId,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            SearchText = searchText,
            Name = name
        };
        var result = await _transactionReportService.GetTransactionsAsync(query, cancellationToken);
        return Ok(result);
    }

    [ProducesResponseType(typeof(IReadOnlyList<MonthlySummaryDto>), StatusCodes.Status200OK)]
    [HttpGet("monthly-summary")]
    public async Task<ActionResult<IReadOnlyList<MonthlySummaryDto>>> GetMonthlySummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _monthlySummaryService.GetMonthlySummaryAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [ProducesResponseType(typeof(IReadOnlyList<CategorySummaryDto>), StatusCodes.Status200OK)]
    [HttpGet("category-summary")]
    public async Task<ActionResult<IReadOnlyList<CategorySummaryDto>>> GetCategorySummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _categorySummaryService.GetCategorySummaryAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [ProducesResponseType(typeof(IReadOnlyList<int>), StatusCodes.Status200OK)]
    [HttpGet("account-bookyears")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetAccountBookYears(
        [FromQuery] Guid accountId,
        CancellationToken cancellationToken)
    {
        var years = await _accountFiscalYearRepository.GetYearsForAccountAsync(accountId, cancellationToken);
        return Ok(years);
    }
}
