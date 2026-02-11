namespace Finance.Application.Categories;

public sealed class UpdateCategoryCommand
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}
