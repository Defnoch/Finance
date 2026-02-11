namespace Finance.Domain.Entities;

public class Category
{
    public Guid CategoryId { get; private set; }
    public string Name { get; set; } = string.Empty;
    public CategoryKind Kind { get; set; }
    public string? ColorHex { get; set; }
    public bool IsDefault { get; private set; }

    public Category(Guid categoryId, string name, CategoryKind kind, string? colorHex, bool isDefault)
    {
        CategoryId = categoryId;
        Name = name;
        Kind = kind;
        ColorHex = colorHex;
        IsDefault = isDefault;
    }
}