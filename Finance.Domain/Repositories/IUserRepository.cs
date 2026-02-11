using System.Threading.Tasks;
using Finance.Domain.Entities;

namespace Finance.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserAsync();
        Task AddUserAsync(User user);
        Task<bool> UserExistsAsync();
    }
}
