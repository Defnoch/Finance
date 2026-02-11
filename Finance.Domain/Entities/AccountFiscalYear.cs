using System;

namespace Finance.Domain.Entities;

public class AccountFiscalYear
{
    public Guid AccountId { get; set; }
    public int Year { get; set; }

    public AccountFiscalYear(Guid accountId, int year)
    {
        AccountId = accountId;
        Year = year;
    }
}
