using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories;

public class CategorizationRuleRepository : ICategorizationRuleRepository
{
    private readonly FinanceDbContext _db;
    public CategorizationRuleRepository(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(CategorizationRule rule)
    {
        _db.CategorizationRules.Add(rule);
        await _db.SaveChangesAsync();
    }

    public async Task<List<CategorizationRule>> GetAllAsync()
    {
        return await _db.CategorizationRules.Include(r => r.Conditions).ToListAsync();
    }

    public async Task<CategorizationRule?> GetByIdAsync(Guid id)
    {
        return await _db.CategorizationRules.Include(r => r.Conditions).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task UpdateAsync(CategorizationRule rule)
    {
        _db.CategorizationRules.Update(rule);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await _db.CategorizationRules.FindAsync(id);
        if (rule != null)
        {
            _db.CategorizationRules.Remove(rule);
            await _db.SaveChangesAsync();
        }
    }
}
