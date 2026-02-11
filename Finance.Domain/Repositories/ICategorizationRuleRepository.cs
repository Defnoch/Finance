using Finance.Domain.Entities;

namespace Finance.Domain.Repositories;

public interface ICategorizationRuleRepository
{
    Task AddAsync(CategorizationRule rule);
    Task<List<CategorizationRule>> GetAllAsync();
    Task<CategorizationRule?> GetByIdAsync(Guid id);
    Task UpdateAsync(CategorizationRule rule);
    Task DeleteAsync(Guid id);
}
