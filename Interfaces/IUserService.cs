using backend_spo.DTOs;

namespace backend_spo.Interfaces;

public interface IUserService
{
    Task<(bool Success, string Message)> UpdateUserRoleAsync(int userId, string roleName);
}