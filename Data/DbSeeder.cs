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
			SeedCarImages(context);

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

		private static void SeedCarImages(ApplicationDbContext context)
		{
			var carImages = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
			{
				["Toyota|Camry"] = "https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?auto=format&fit=crop&w=1200&q=80",
				["BMW|X5"] = "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1200&q=80",
				["Kia|K5"] = "https://images.unsplash.com/photo-1549924231-f129b911e442?auto=format&fit=crop&w=1200&q=80",
				["Hyundai|Tucson"] = "https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?auto=format&fit=crop&w=1200&q=80",
				["Mercedes-Benz|E-Class"] = "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=1200&q=80",
			};

			foreach (var car in context.Cars.ToList())
			{
				if (!string.IsNullOrWhiteSpace(car.ImageUrl))
					continue;

				var key = $"{car.Brand.Trim()}|{car.Model.Trim()}";
				if (carImages.TryGetValue(key, out var imageUrl))
					car.ImageUrl = imageUrl;
			}
		}
	}
}
