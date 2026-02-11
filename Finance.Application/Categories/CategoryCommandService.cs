using Finance.Domain.Entities;
using Finance.Domain.Repositories;

namespace Finance.Application.Categories;

public sealed class CategoryCommandService : ICategoryCommandService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryCommandService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> AddCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var kind = Enum.TryParse<CategoryKind>(command.Kind, out var parsedKind) ? parsedKind : CategoryKind.Expense;
        var category = new Category(Guid.NewGuid(), command.Name, kind, command.ColorHex, false);
        await _categoryRepository.AddAsync(category, cancellationToken);
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Kind = category.Kind.ToString(),
            ColorHex = category.ColorHex
        };
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
            return null;
        category.Name = command.Name;
        category.Kind = Enum.TryParse<CategoryKind>(command.Kind, out var parsedKind) ? parsedKind : CategoryKind.Expense;
        category.ColorHex = command.ColorHex;
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Kind = category.Kind.ToString(),
            ColorHex = category.ColorHex
        };
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category == null)
            return false;
        await _categoryRepository.DeleteAsync(category, cancellationToken);
        return true;
    }
}
