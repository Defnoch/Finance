using Finance.Domain.Entities;
using Finance.Domain.Repositories;

namespace Finance.Application.Categories;

public sealed class CategoryQueryService : ICategoryQueryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryQueryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        if (categories.Count == 0)
        {
            var seedCategories = new List<Category>
            {
                new(Guid.NewGuid(), "Boodschappen", CategoryKind.Expense, "#4CAF50", true),
                new(Guid.NewGuid(), "Abonnementen", CategoryKind.Expense, "#2196F3", true),
                new(Guid.NewGuid(), "Wonen",       CategoryKind.Expense, "#FF9800", true),
                new(Guid.NewGuid(), "Inkomen",     CategoryKind.Income,  "#9C27B0", true),
                new(Guid.NewGuid(), "Overig",      CategoryKind.Expense, "#9E9E9E", true),
            };

            foreach (var c in seedCategories)
            {
                await _categoryRepository.AddAsync(c, cancellationToken);
            }

            categories = await _categoryRepository.GetAllAsync(cancellationToken);
        }

        var dtos = categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name       = c.Name,
                Kind       = c.Kind.ToString(),
                ColorHex   = c.ColorHex
            })
            .ToList();

        return dtos;
    }
}
