using backend_spo.Interfaces;
using backend_spo.Entities;

namespace backend_spo.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message)> UpdateUserRoleAsync(int userId, string roleName)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return (false, "Пользователь не найден");
        var role = await _userRepository.GetRoleByNameAsync(roleName);
        if (role == null) return (false, "Роль не найдена");
        user.RoleId = role.Id;
        user.Role = role;
        await _userRepository.UpdateAsync(user);
        return (true, "Роль пользователя успешно обновлена");
    }

    public async Task<(bool Success, string Message)> UpdateUserStatusAsync(int userId, bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return (false, "Пользователь не найден");
        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user);
        return isActive
            ? (true, "Пользователь успешно разблокирован")
            : (true, "Пользователь успешно заблокирован");
    }

    
}

