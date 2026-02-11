namespace Finance.Domain.Entities;

public class CategorizationRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public Guid CategoryId { get; set; }
    public List<RuleCondition> Conditions { get; set; } = new();
    public bool IsIgnored { get; set; } // Toegevoegd veld voor genegeerde regels
}

public class RuleCondition
{
    public Guid Id { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
