using Finance.Domain;
using Finance.Domain.Entities;
using NUnit.Framework;

namespace Finance.Tests.Categorization;

[TestFixture]
public class RuleBasedCategorizationEngineTests
{
    private Finance.Domain.RuleBasedCategorizationEngine _engine = null!;

    [SetUp]
    public void Setup() => _engine = new Finance.Domain.RuleBasedCategorizationEngine();

    [Test]
    public void IBAN_ExactMatch_Works()
    {
        var tx = new TransactionCategorizationInput { CounterpartyIban = "NL01BANK0123456789" };
        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
            Priority = 1,
            CategoryId = Guid.NewGuid(),
            Conditions = new() { new RuleCondition { Field = "CounterpartyIban", Operator = "Equals", Value = "NL01BANK0123456789" } }
        };
        var result = _engine.Categorize(tx, new[] { rule });
        Assert.That(result.IsMatched, Is.True);
        Assert.That(result.CategoryId, Is.EqualTo(rule.CategoryId.ToString()));
        Assert.That(result.RuleId, Is.EqualTo(rule.Id.ToString()));
    }

    [Test]
    public void Name_Contains_And_Direction_Works()
    {
        var tx = new TransactionCategorizationInput { CounterpartyName = "Albert Heijn Amsterdam", Direction = Direction.Debit };
        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
            Priority = 1,
            CategoryId = Guid.NewGuid(),
            Conditions = new()
            {
                new RuleCondition { Field = "CounterpartyName", Operator = "Contains", Value = "heijn" },
                new RuleCondition { Field = "Direction", Operator = "Equals", Value = "Debit" }
            }
        };
        var result = _engine.Categorize(tx, new[] { rule });
        Assert.That(result.IsMatched, Is.True);
        Assert.That(result.CategoryId, Is.EqualTo(rule.CategoryId.ToString()));
        Assert.That(result.RuleId, Is.EqualTo(rule.Id.ToString()));
    }

    [Test]
    public void Description_Contains_Works()
    {
        var tx = new TransactionCategorizationInput { Description = "Spotify Family Subscription" };
        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
            Priority = 1,
            CategoryId = Guid.NewGuid(),
            Conditions = new() { new RuleCondition { Field = "Description", Operator = "Contains", Value = "family" } }
        };
        var result = _engine.Categorize(tx, new[] { rule });
        Assert.That(result.IsMatched, Is.True);
        Assert.That(result.CategoryId, Is.EqualTo(rule.CategoryId.ToString()));
        Assert.That(result.RuleId, Is.EqualTo(rule.Id.ToString()));
    }

    [Test]
    public void Priority_Ordering_FirstMatchWins()
    {
        var tx = new TransactionCategorizationInput { Description = "Spotify Family Subscription" };
        var rules = new List<CategorizationRule>
        {
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                Priority = 2,
                CategoryId = Guid.NewGuid(),
                Conditions = new() { new RuleCondition { Field = "Description", Operator = "Contains", Value = "family" } }
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                Priority = 1,
                CategoryId = Guid.NewGuid(),
                Conditions = new() { new RuleCondition { Field = "Description", Operator = "Contains", Value = "family" } }
            }
        };
        var result = _engine.Categorize(tx, rules);
        Assert.That(result.IsMatched, Is.True);
        Assert.That(result.CategoryId, Is.EqualTo(rules[0].CategoryId.ToString()));
        Assert.That(result.RuleId, Is.EqualTo(rules[0].Id.ToString()));
    }

    [Test]
    public void Disabled_Rules_Are_Ignored()
    {
        var tx = new TransactionCategorizationInput { Description = "Spotify Family Subscription" };
        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            IsEnabled = false,
            Priority = 10,
            CategoryId = Guid.NewGuid(),
            Conditions = new() { new RuleCondition { Field = "Description", Operator = "Contains", Value = "family" } }
        };
        var result = _engine.Categorize(tx, new[] { rule });
        Assert.That(result.IsMatched, Is.False);
        Assert.That(result.CategoryId, Is.Null);
        Assert.That(result.RuleId, Is.Null);
    }
}
