using Finance.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;

    public AccountsController(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        var result = accounts.Select(a => new AccountDto
        {
            AccountId = a.AccountId,
            Name = a.AccountIdentifier, // Geen Name property, dus gebruik AccountIdentifier als placeholder
            Type = a.AccountType,
            Provider = a.Provider,
            AccountIdentifier = a.AccountIdentifier
        }).ToList();
        return Ok(result);
    }
}

public class AccountDto
{
    public Guid AccountId { get; set; }
    public string? Name { get; set; } // Placeholder, want Account heeft geen Name
    public string? Type { get; set; }
    public string? Provider { get; set; }
    public string? AccountIdentifier { get; set; }
}
