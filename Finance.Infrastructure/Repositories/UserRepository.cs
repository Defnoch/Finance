using System.Threading.Tasks;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FinanceDbContext _context;
        public UserRepository(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserAsync()
        {
            return await _context.Users.FirstOrDefaultAsync();
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserExistsAsync()
        {
            return await _context.Users.AnyAsync();
        }
    }
}
