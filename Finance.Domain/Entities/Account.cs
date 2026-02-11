namespace Finance.Domain.Entities;

public class Account
{
    public Guid AccountId { get; set; }
    public string AccountIdentifier { get; set; } = string.Empty; // IBAN of rekeningnummer
    public string Provider { get; set; } = string.Empty; // ING, etc.
    public string AccountType { get; set; } = string.Empty; // "Normaal" of "Spaar"
}
