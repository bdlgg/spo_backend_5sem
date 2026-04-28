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
			SeedCars(context);

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

			var suppliers = new List<Supplier>
			{
				new Supplier
				{
					Name = "Polestar Spaces",
					ContactPerson = "Андрей Морозов",
					Phone = "+74950000010",
					Email = "polestar@autos.ru",
					Address = "Москва, Ленинградский проспект, 39"
				},
				new Supplier
				{
					Name = "Volvo Cars Showroom",
					ContactPerson = "Ольга Беляева",
					Phone = "+74950000011",
					Email = "volvo@autos.ru",
					Address = "Москва, Кутузовский проспект, 24"
				},
				new Supplier
				{
					Name = "Lucid Atelier",
					ContactPerson = "Денис Соколов",
					Phone = "+74950000012",
					Email = "lucid@autos.ru",
					Address = "Москва, Пресненская набережная, 12"
				},
				new Supplier
				{
					Name = "Lotus Studio",
					ContactPerson = "Мария Петрова",
					Phone = "+74950000013",
					Email = "lotus@autos.ru",
					Address = "Москва, Большая Якиманка, 18"
				},
				new Supplier
				{
					Name = "Concept & Heritage",
					ContactPerson = "Иван Громов",
					Phone = "+74950000014",
					Email = "concept@autos.ru",
					Address = "Москва, Малая Дмитровка, 7"
				}
			};

			context.Suppliers.AddRange(suppliers);
			context.SaveChanges();
		}

		private static void SeedAdmin(ApplicationDbContext context)
		{
			if (context.Users.Any(u => u.Email == "admin@autos.ru"))
				return;

			var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Админ");
			if (adminRole == null)
				return;

			var admin = new User
			{
				FirstName = "Александр",
				LastName = "Админов",
				Email = "admin@autos.ru",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				RoleId = adminRole.Id
			};

			context.Users.Add(admin);
			context.SaveChanges();
		}

		private static void SeedCars(ApplicationDbContext context)
		{
			if (context.Cars.Any())
				return;

			var suppliers = context.Suppliers.ToDictionary(s => s.Name);

			Supplier S(string name) => suppliers[name];

			var cars = new List<Car>
			{
				// Polestar — Polestar Spaces
				BuildCar(S("Polestar Spaces"), "Polestar", "2", 2024, "Седан", "Серый",
					4_800_000m, "YV1ZP02EDP1234567", "Available", "polestar-2", 5),
				BuildCar(S("Polestar Spaces"), "Polestar", "3", 2024, "Кроссовер", "Чёрный",
					7_200_000m, "YV1ZP03EDP1234568", "Available", "polestar-3", 5),
				BuildCar(S("Polestar Spaces"), "Polestar", "4", 2024, "Кроссовер", "Серый",
					6_400_000m, "YV1ZP04EDP1234569", "Available", "polestar-4", 5),
				BuildCar(S("Polestar Spaces"), "Polestar", "5", 2025, "Седан", "Синий",
					9_800_000m, "YV1ZP05EDP1234570", "Available", "polestar-5", 5),
				BuildCar(S("Polestar Spaces"), "Polestar", "6", 2026, "Родстер", "Голубой",
					14_500_000m, "YV1ZP06EDP1234571", "Reserved", "polestar-6", 5),

				// Volvo — Volvo Cars Showroom
				BuildCar(S("Volvo Cars Showroom"), "Volvo", "C40 Recharge", 2024, "Кроссовер", "Серый",
					5_200_000m, "YV1ZVC40RP1234572", "Available", "volvo-c40-recharge", 5),
				BuildCar(S("Volvo Cars Showroom"), "Volvo", "EM90", 2024, "Минивэн", "Чёрный",
					8_600_000m, "YV1ZVEM90P1234573", "Available", "volvo-em90", 5),
				BuildCar(S("Volvo Cars Showroom"), "Volvo", "ES90", 2025, "Седан", "Белый",
					7_400_000m, "YV1ZVES90P1234574", "Available", "volvo-es90", 5),
				BuildCar(S("Volvo Cars Showroom"), "Volvo", "EX30", 2024, "Кроссовер", "Синий",
					4_200_000m, "YV1ZVEX30P1234575", "Sold", "volvo-ex30", 5),
				BuildCar(S("Volvo Cars Showroom"), "Volvo", "EX90", 2024, "Кроссовер", "Серый",
					9_100_000m, "YV1ZVEX90P1234576", "Available", "volvo-ex90", 5),

				// Lucid — Lucid Atelier
				BuildCar(S("Lucid Atelier"), "Lucid", "Air Grand Touring", 2024, "Седан", "Белый",
					14_500_000m, "50EA1FFA1RA001234", "Available", "lucid-air-grand-touring", 8),
				BuildCar(S("Lucid Atelier"), "Lucid", "Air Sapphire", 2024, "Седан", "Чёрный",
					26_800_000m, "50EA1FFA1RA001235", "Reserved", "lucid-air-sapphire", 8),

				// Lotus — Lotus Studio
				BuildCar(S("Lotus Studio"), "Lotus", "Eletre", 2024, "Кроссовер", "Зелёный",
					12_900_000m, "SCCEFG1SXRH001234", "Available", "lotus-eletre", 8),
				BuildCar(S("Lotus Studio"), "Lotus", "Emeya", 2024, "Седан", "Серый",
					14_200_000m, "SCCEFG2SXRH001235", "Available", "lotus-emeya", 10),
				BuildCar(S("Lotus Studio"), "Lotus", "Evija", 2024, "Гиперкар", "Жёлтый",
					165_000_000m, "SCCEFG3SXRH001236", "Reserved", "lotus-evija", 8),
				BuildCar(S("Lotus Studio"), "Lotus", "Theory 1", 2025, "Концепт", "Серебристый",
					85_000_000m, "SCCEFG4SXRH001237", "Reserved", "lotus-theory-1", 8),

				// Concepts & Heritage
				BuildCar(S("Concept & Heritage"), "Alpine", "A290", 2024, "Хэтчбек", "Голубой",
					3_800_000m, "VF1A290FRP00012345", "Available", "alpine-a290", 8),
				BuildCar(S("Concept & Heritage"), "Cadillac", "Opulent Velocity", 2025, "Концепт", "Белый",
					52_000_000m, "1G6DV5OV0R0001234", "Reserved", "cadillac-opulent-velocity", 8),
				BuildCar(S("Concept & Heritage"), "DeLorean", "Alpha5", 2025, "Концепт", "Серебристый",
					28_000_000m, "1DL5ALPHA0RA01234", "Reserved", "delorean-alpha5", 8),
			};

			context.Cars.AddRange(cars);
			context.SaveChanges();
		}

		private static Car BuildCar(
			Supplier supplier,
			string brand,
			string model,
			int year,
			string type,
			string color,
			decimal price,
			string vin,
			string status,
			string slug,
			int photoCount)
		{
			var photos = new List<CarPhoto>();
			for (var i = 1; i <= photoCount; i++)
			{
				photos.Add(new CarPhoto
				{
					Url = $"http://localhost:8080/cars/{slug}/{i}.png",
					SortOrder = i
				});
			}

			return new Car
			{
				Brand = brand,
				Model = model,
				Year = year,
				Type = type,
				Color = color,
				Price = price,
				Vin = vin,
				Status = status,
				IsActive = true,
				SupplierId = supplier.Id,
				Photos = photos
			};
		}
	}
}
