using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task UpdateAsync(User user);
}
