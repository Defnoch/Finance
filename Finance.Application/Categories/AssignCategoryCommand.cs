namespace Finance.Application.Categories;

public sealed class AssignCategoryCommand
{
    public Guid TransactionId { get; init; }
    public Guid CategoryId { get; init; }
}

