using backend_spo.Entities;
using BCrypt.Net;

namespace backend_spo.Data
{
	public static class DbSeeder
	{
		public static void Seed(ApplicationDbContext context)
		{
			SeedRoles(context);
			SeedSuppliers(context);
			SeedAdmin(context);

			context.SaveChanges();
		}

		private static void SeedRoles(ApplicationDbContext context)
		{
			if (context.Roles.Any())
				return;

			var roles = new List<Role>
			{
				new Role
				{
					Name = "Админ",
					Description = "Полный доступ к системе"
				},
				new Role
				{
					Name = "Менеджер",
					Description = "Оформление продаж и работа с клиентами"
				},
				new Role
				{
					Name = "Руководитель",
					Description = "Просмотр аналитики и контроль работы"
				},
				new Role
				{
					Name = "Клиент",
					Description = "Клиент автосалона"
				}
			};

			context.Roles.AddRange(roles);
			context.SaveChanges();
		}

		private static void SeedSuppliers(ApplicationDbContext context)
		{
			if (context.Suppliers.Any())
				return;

			var supplier = new Supplier
			{
				Name = "Toyota Center",
				ContactPerson = "Петров Алексей",
				Phone = "+79990000001",
				Email = "toyota@supplier.ru",
				Address = "Москва, Ленинградское шоссе, 10"
			};

			context.Suppliers.Add(supplier);
			context.SaveChanges();
		}

		private static void SeedAdmin(ApplicationDbContext context)
		{
			if (context.Users.Any(u => u.Email == "admin@autosalon.ru"))
				return;

			var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Админ");
			if (adminRole == null)
				return;

			var admin = new User
			{
				FirstName = "Александр",
				LastName = "Админов",
				Email = "admin@autosalon.ru",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				RoleId = adminRole.Id
			};

			context.Users.Add(admin);
			context.SaveChanges();
		}
	}
}