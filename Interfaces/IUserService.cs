using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<(bool Success, string Message)> UpdateUserRoleAsync(int userId, string roleName);
    Task<(bool Success, string Message)> UpdateUserStatusAsync(int userId, bool isActive);
}