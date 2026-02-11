namespace Finance.Application.Categories;

public interface ICategoryAssignmentService
{
    Task AssignCategoryAsync(AssignCategoryCommand command, CancellationToken cancellationToken = default);
    Task UnassignCategoryAsync(UnassignCategoryCommand command, CancellationToken cancellationToken = default);
}

public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}

public interface ICategoryCommandService
{
    Task<CategoryDto> AddCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default);
    Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
