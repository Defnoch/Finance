using System;
using System.Collections.Generic;

namespace Finance.Categorization;

public enum Direction { Debit, Credit }

public class Transaction
{
    public Direction Direction { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string? CounterpartyIban { get; set; }
    public string? CounterpartyName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
}

public class CategorizationRule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public List<RuleCondition> Conditions { get; set; } = new();
}

public class RuleCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class CategorizationResult
{
    public bool IsMatched { get; set; }
    public string? CategoryId { get; set; }
    public string? RuleId { get; set; }
}

