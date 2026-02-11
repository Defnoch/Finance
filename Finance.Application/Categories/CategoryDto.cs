namespace Finance.Application.Categories;

public sealed class CategoryDto
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string? ColorHex { get; init; }
}

