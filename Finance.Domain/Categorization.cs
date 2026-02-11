using System.Text.RegularExpressions;
using System.Globalization;

namespace Finance.Domain;

public enum Direction { Debit, Credit }

public class TransactionCategorizationInput
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

public class CategorizationResult
{
    public bool IsMatched { get; set; }
    public string? CategoryId { get; set; }
    public string? RuleId { get; set; }
}

public interface ICategorizationEngine
{
    CategorizationResult Categorize(TransactionCategorizationInput transaction, IEnumerable<Entities.CategorizationRule> rules);
}

public static class Normalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var text = input.Trim().ToUpperInvariant();
        text = Regex.Replace(text, "\\s+", " ");
        return text;
    }
}

public static class ConditionMatcher
{
    public static bool IsMatch(Entities.RuleCondition cond, TransactionCategorizationInput tx)
    {
        var field = cond.Field.ToLowerInvariant();
        var op = cond.Operator.ToLowerInvariant();
        var value = cond.Value;

        string? txValue = field switch
        {
            "direction" => tx.Direction.ToString(),
            "amount" => tx.Amount.ToString(CultureInfo.InvariantCulture),
            "currency" => tx.Currency,
            "counterpartyiban" => tx.CounterpartyIban,
            "counterpartyname" => tx.CounterpartyName,
            "description" => tx.Description,
            "paymentmethod" => tx.PaymentMethod,
            _ => null
        };

        // Normalize for string fields
        if (field is "counterpartyname" or "description")
        {
            txValue = Normalizer.Normalize(txValue);
            value = Normalizer.Normalize(value);
        }

        switch (op)
        {
            case "equals":
                return string.Equals(txValue, value, StringComparison.OrdinalIgnoreCase);
            case "contains":
                return txValue != null && txValue.Contains(value, StringComparison.OrdinalIgnoreCase);
            case "startswith":
                return txValue != null && txValue.StartsWith(value, StringComparison.OrdinalIgnoreCase);
            case "inlist":
                var items = value.Split('|').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                if (field == "description" || field == "counterpartyname")
                    return items.Any(item => txValue != null && txValue.Contains(Normalizer.Normalize(item), StringComparison.OrdinalIgnoreCase));
                if (field == "counterpartyiban" || field == "currency" || field == "direction")
                    return items.Any(item => string.Equals(txValue, item, StringComparison.OrdinalIgnoreCase));
                if (field == "amount")
                    return items.Any(item => decimal.TryParse(item, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && tx.Amount == v);
                return false;
            default:
                return false;
        }
    }
}

public class RuleBasedCategorizationEngine : ICategorizationEngine
{
    public CategorizationResult Categorize(TransactionCategorizationInput transaction, IEnumerable<Entities.CategorizationRule> rules)
    {
        var enabledRules = rules.Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToList();

        foreach (var rule in enabledRules)
        {
            bool allMatch = rule.Conditions.All(cond => ConditionMatcher.IsMatch(cond, transaction));
            if (allMatch)
            {
                return new CategorizationResult
                {
                    IsMatched = true,
                    CategoryId = rule.CategoryId.ToString(),
                    RuleId = rule.Id.ToString()
                };
            }
        }
        return new CategorizationResult { IsMatched = false };
    }
}
