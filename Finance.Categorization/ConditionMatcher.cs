using System;
using System.Linq;
using System.Globalization;

namespace Finance.Categorization;

public static class ConditionMatcher
{
    public static bool IsMatch(RuleCondition cond, Transaction tx)
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

