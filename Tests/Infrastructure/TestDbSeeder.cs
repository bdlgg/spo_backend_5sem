using backend_spo.Data;
using backend_spo.Entities;

namespace backend_spo.Tests.Infrastructure;

public static class TestDbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Roles.Any())
            return;

        context.Roles.Add(new Role
        {
            Id = 1,
            Name = "Админ"
        });

        context.SaveChanges();
    }
}