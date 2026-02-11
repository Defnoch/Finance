namespace Finance.Application.Reports;

public sealed class CategorySummaryDto
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}

