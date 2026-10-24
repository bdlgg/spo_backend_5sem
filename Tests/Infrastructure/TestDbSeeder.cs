using backend_spo.Data;
using backend_spo.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend_spo.Tests.Infrastructure;

public static class TestDbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        context.Database.ExecuteSqlRaw("""
                                       SELECT setval(
                                           pg_get_serial_sequence('roles', 'id'),
                                           COALESCE((SELECT MAX(id) FROM roles), 1),
                                           EXISTS (SELECT 1 FROM roles)
                                       );
                                       """);
        string[] roleNames =
        [
            "Админ",
            "Клиент",
            "Менеджер",
            "Руководитель"
        ];

        foreach (var roleName in roleNames)
        {
            if (!context.Roles.Any(r => r.Name == roleName))
            {
                context.Roles.Add(new Role
                {
                    Name = roleName
                });
            }
        }

        context.SaveChanges();

        // Некоторые тесты используют ID пользователя 1 в JWT.
        if (!context.Users.Any(u => u.Id == 1))
        {
            var managerRole = context.Roles
                .Single(r => r.Name == "Менеджер");

            context.Users.Add(new User
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Manager",
                Email = "test-manager@local.test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    "TestPassword123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RoleId = managerRole.Id
            });

            context.SaveChanges();
        }

        // Синхронизируем PostgreSQL-последовательность
        // после явного добавления пользователя с ID = 1.
        context.Database.ExecuteSqlRaw("""
                                       SELECT setval(
                                           pg_get_serial_sequence('users', 'id'),
                                           COALESCE((SELECT MAX(id) FROM users), 1),
                                           true
                                       );
                                       """);
    }
}