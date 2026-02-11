using System.Collections.Generic;

namespace Finance.Categorization;

public interface ICategorizationEngine
{
    CategorizationResult Categorize(Transaction transaction, IEnumerable<CategorizationRule> rules);
}

