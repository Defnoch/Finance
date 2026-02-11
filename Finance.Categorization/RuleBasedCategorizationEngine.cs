using System.Collections.Generic;
using System.Linq;

namespace Finance.Categorization;

public class RuleBasedCategorizationEngine : ICategorizationEngine
{
    public CategorizationResult Categorize(Transaction transaction, IEnumerable<CategorizationRule> rules)
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
                    CategoryId = rule.CategoryId,
                    RuleId = rule.Id
                };
            }
        }
        return new CategorizationResult { IsMatched = false };
    }
}

