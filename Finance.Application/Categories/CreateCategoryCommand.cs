namespace Finance.Application.Categories;

public sealed class CreateCategoryCommand
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}
