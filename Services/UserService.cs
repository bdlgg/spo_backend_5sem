using Microsoft.EntityFrameworkCore;
using backend_spo.Data;
using backend_spo.Interfaces;

namespace backend_spo.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> UpdateUserRoleAsync(int userId, string roleName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return (false, "Пользователь не найден");

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return (false, "Роль не найдена");
        
        user.RoleId = role.Id;
        await _context.SaveChangesAsync();
        return (true, "Роль пользователя успешно обновлена");
    }
}