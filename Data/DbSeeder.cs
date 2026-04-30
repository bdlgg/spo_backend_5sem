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
			SeedLead(context);
			SeedManagers(context);
			SeedCars(context);
			SeedClients(context);
			SeedClientUsers(context);
			SeedTestDrives(context);
			SeedSales(context);
			SeedServiceOrders(context);

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

		private static void SeedLead(ApplicationDbContext context)
		{
			if (context.Users.Any(u => u.Email == "lead@autos.ru"))
				return;

			var leadRole = context.Roles.FirstOrDefault(r => r.Name == "Руководитель");
			if (leadRole == null)
				return;

			var lead = new User
			{
				FirstName = "Елена",
				LastName = "Руководящая",
				Email = "lead@autos.ru",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("Lead123!"),
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				RoleId = leadRole.Id
			};

			context.Users.Add(lead);
			context.SaveChanges();
		}

		private static void SeedManagers(ApplicationDbContext context)
		{
			var managerRole = context.Roles.FirstOrDefault(r => r.Name == "Менеджер");
			if (managerRole == null)
				return;

			var passwordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!");

			var managersToSeed = new[]
			{
				new { FirstName = "Артём", LastName = "Лебедев",  Email = "artem@autos.ru" },
				new { FirstName = "Екатерина", LastName = "Орлова", Email = "ekaterina@autos.ru" },
				new { FirstName = "Михаил", LastName = "Власов",  Email = "mikhail@autos.ru" }
			};

			foreach (var m in managersToSeed)
			{
				if (context.Users.Any(u => u.Email == m.Email))
					continue;

				context.Users.Add(new User
				{
					FirstName = m.FirstName,
					LastName = m.LastName,
					Email = m.Email,
					PasswordHash = passwordHash,
					IsActive = true,
					CreatedAt = DateTime.UtcNow.AddDays(-90),
					RoleId = managerRole.Id
				});
			}

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

		private static void SeedClients(ApplicationDbContext context)
		{
			if (context.Clients.Any())
				return;

			var clients = new List<Client>
			{
				// «Горячие» — есть тест-драйвы и/или сделки
				new Client { FirstName = "Дмитрий",  LastName = "Соколов",  Phone = "+79161234501", Email = "sokolov@autos.ru" },
				new Client { FirstName = "Анна",     LastName = "Кузнецова", Phone = "+79161234502", Email = "kuznetsova@autos.ru" },
				new Client { FirstName = "Сергей",   LastName = "Иванов",   Phone = "+79161234503", Email = "ivanov@autos.ru" },
				new Client { FirstName = "Ольга",    LastName = "Смирнова", Phone = "+79161234504", Email = "smirnova@autos.ru" },
				new Client { FirstName = "Павел",    LastName = "Морозов",  Phone = "+79161234505", Email = "morozov@autos.ru" },

				// «Тёплые» — есть тест-драйвы, но нет сделки
				new Client { FirstName = "Игорь",    LastName = "Петров",   Phone = "+79161234506", Email = "petrov@autos.ru" },
				new Client { FirstName = "Татьяна",  LastName = "Лебедева", Phone = "+79161234507", Email = "lebedeva@autos.ru" },
				new Client { FirstName = "Андрей",   LastName = "Новиков",  Phone = "+79161234508", Email = "novikov@autos.ru" },
				new Client { FirstName = "Наталья",  LastName = "Волкова",  Phone = "+79161234509", Email = "volkova@autos.ru" },

				// «Холодные» — только в базе, без активности
				new Client { FirstName = "Михаил",   LastName = "Алексеев", Phone = "+79161234510", Email = "alekseev@autos.ru" },
				new Client { FirstName = "Юлия",     LastName = "Зайцева",  Phone = "+79161234511", Email = "zaytseva@autos.ru" },
				new Client { FirstName = "Виктор",   LastName = "Романов",  Phone = "+79161234512", Email = "romanov@autos.ru" },
			};

			context.Clients.AddRange(clients);
			context.SaveChanges();
		}

		private static void SeedClientUsers(ApplicationDbContext context)
		{
			var clientRole = context.Roles.FirstOrDefault(r => r.Name == "Клиент");
			if (clientRole == null)
				return;

			// Готовые тестовые аккаунты — по одному из каждой "температуры".
			// Пароль один на всех, чтобы было удобно переключаться между ролями.
			var seedUsers = new[]
			{
				new { FirstName = "Дмитрий", LastName = "Соколов",  Email = "sokolov@autos.ru" },
				new { FirstName = "Игорь",   LastName = "Петров",   Email = "petrov@autos.ru" },
				new { FirstName = "Михаил",  LastName = "Алексеев", Email = "alekseev@autos.ru" },
			};

			var passwordHash = BCrypt.Net.BCrypt.HashPassword("Client123!");

			foreach (var seed in seedUsers)
			{
				if (context.Users.Any(u => u.Email == seed.Email))
					continue;

				var user = new User
				{
					FirstName = seed.FirstName,
					LastName = seed.LastName,
					Email = seed.Email,
					PasswordHash = passwordHash,
					IsActive = true,
					CreatedAt = DateTime.UtcNow,
					RoleId = clientRole.Id,
				};
				context.Users.Add(user);
				context.SaveChanges();

				var client = context.Clients.FirstOrDefault(c => c.Email == seed.Email);
				if (client != null && client.UserId == null)
				{
					client.UserId = user.Id;
					context.SaveChanges();
				}
			}
		}

		private static void SeedTestDrives(ApplicationDbContext context)
		{
			if (context.TestDrives.Any())
				return;

			var clients = context.Clients.ToList();
			var cars = context.Cars.ToList();
			var managers = context.Users
				.Where(u => u.Role!.Name == "Менеджер")
				.ToList();

			if (clients.Count == 0 || cars.Count == 0 || managers.Count == 0)
				return;

			Client C(string lastName) => clients.First(c => c.LastName == lastName);
			Car V(string brand, string model) => cars.First(c => c.Brand == brand && c.Model == model);
			User M(int idx) => managers[idx % managers.Count];

			var now = DateTime.UtcNow;
			var testDrives = new List<TestDrive>
			{
				// Соколов: горячий, 3 авто посмотрел, один завершён успешно
				NewTestDrive(C("Соколов"), V("Polestar", "3"), M(0), now.AddDays(-45), "Completed", "Interested", "Готов к сделке"),
				NewTestDrive(C("Соколов"), V("Polestar", "4"), M(0), now.AddDays(-30), "Completed", "Interested", "Сравнивает с Volvo EX90"),
				NewTestDrive(C("Соколов"), V("Volvo", "EX90"), M(1), now.AddDays(-12), "Completed", "Interested", "Привёл супругу, понравилось"),

				// Кузнецова: горячая, тест-драйв на Lotus Eletre, потом сделка
				NewTestDrive(C("Кузнецова"), V("Lotus", "Eletre"), M(1), now.AddDays(-35), "Completed", "Interested", "Понравилась подвеска"),
				NewTestDrive(C("Кузнецова"), V("Lucid", "Air Grand Touring"), M(2), now.AddDays(-20), "Completed", "Interested", "Проверила автопилот"),

				// Иванов: горячий, два тест-драйва на Volvo
				NewTestDrive(C("Иванов"), V("Volvo", "EX30"), M(2), now.AddDays(-60), "Completed", "Interested", "Брал жене на пробу"),
				NewTestDrive(C("Иванов"), V("Volvo", "EX90"), M(0), now.AddDays(-50), "Completed", "Interested", "Решились на EX90"),

				// Смирнова: горячая, недавний тест Lucid Sapphire
				NewTestDrive(C("Смирнова"), V("Lucid", "Air Sapphire"), M(0), now.AddDays(-7), "Completed", "Interested", "Готова к brutto-сделке"),
				NewTestDrive(C("Смирнова"), V("Lotus", "Emeya"), M(1), now.AddDays(-22), "Completed", "Interested", "Сравнивала по динамике"),

				// Морозов: горячий, Polestar 6 запросил
				NewTestDrive(C("Морозов"), V("Polestar", "6"), M(2), now.AddDays(-3), "Scheduled", "", "Записан на демо"),
				NewTestDrive(C("Морозов"), V("Polestar", "5"), M(2), now.AddDays(-25), "Completed", "Interested", "Резерв 7 дней"),

				// Тёплые — только тест-драйвы, без покупок
				NewTestDrive(C("Петров"), V("Volvo", "ES90"), M(0), now.AddDays(-15), "Completed", "NeedConsultation", "Уточняет лизинг"),
				NewTestDrive(C("Лебедева"), V("Polestar", "2"), M(1), now.AddDays(-10), "Completed", "NeedConsultation", "Сравнивает с Tesla Model 3"),
				NewTestDrive(C("Лебедева"), V("Volvo", "C40 Recharge"), M(1), now.AddDays(-4), "Scheduled", "", "Просит привести на квартиру для теста"),
				NewTestDrive(C("Новиков"), V("Lotus", "Eletre"), M(2), now.AddDays(-18), "Cancelled", "Rejected", "Передумал"),
				NewTestDrive(C("Новиков"), V("Alpine", "A290"), M(2), now.AddDays(-2), "Scheduled", "", "Оставил заявку через сайт"),
				NewTestDrive(C("Волкова"), V("Volvo", "EM90"), M(0), now.AddDays(-21), "Completed", "NeedConsultation", "Думает про второе авто в семью"),
			};

			context.TestDrives.AddRange(testDrives);
			context.SaveChanges();
		}

		private static TestDrive NewTestDrive(
			Client client,
			Car car,
			User manager,
			DateTime scheduledAt,
			string status,
			string result,
			string comment)
		{
			return new TestDrive
			{
				ClientId = client.Id,
				CarId = car.Id,
				ManagerId = manager.Id,
				ScheduledAt = scheduledAt,
				Status = status,
				Result = result,
				Comment = comment,
				CreatedAt = scheduledAt.AddDays(-3),
			};
		}

		private static void SeedSales(ApplicationDbContext context)
		{
			if (context.Sales.Any())
				return;

			var clients = context.Clients.ToList();
			var cars = context.Cars.ToList();
			var managers = context.Users
				.Where(u => u.Role!.Name == "Менеджер")
				.ToList();

			if (clients.Count == 0 || cars.Count == 0 || managers.Count == 0)
				return;

			Client C(string lastName) => clients.First(c => c.LastName == lastName);
			Car V(string brand, string model) => cars.First(c => c.Brand == brand && c.Model == model);
			User M(int idx) => managers[idx % managers.Count];

			var now = DateTime.UtcNow;
			var sales = new List<Sale>
			{
				// Иванов купил Volvo EX30
				new Sale
				{
					CarId = V("Volvo", "EX30").Id,
					ClientId = C("Иванов").Id,
					UserId = M(0).Id,
					SaleDate = now.AddDays(-40),
					FinalPrice = 4_100_000m,
					Status = "Completed"
				},
				// Соколов взял Polestar 3
				new Sale
				{
					CarId = V("Polestar", "3").Id,
					ClientId = C("Соколов").Id,
					UserId = M(1).Id,
					SaleDate = now.AddDays(-25),
					FinalPrice = 7_050_000m,
					Status = "Completed"
				},
				// Кузнецова — Lotus Eletre
				new Sale
				{
					CarId = V("Lotus", "Eletre").Id,
					ClientId = C("Кузнецова").Id,
					UserId = M(1).Id,
					SaleDate = now.AddDays(-15),
					FinalPrice = 12_700_000m,
					Status = "Completed"
				},
				// Смирнова — Lotus Emeya, ещё не закрыта
				new Sale
				{
					CarId = V("Lotus", "Emeya").Id,
					ClientId = C("Смирнова").Id,
					UserId = M(2).Id,
					SaleDate = now.AddDays(-2),
					FinalPrice = 14_000_000m,
					Status = "Pending"
				},
				// Морозов — Polestar 5
				new Sale
				{
					CarId = V("Polestar", "5").Id,
					ClientId = C("Морозов").Id,
					UserId = M(0).Id,
					SaleDate = now.AddDays(-10),
					FinalPrice = 9_500_000m,
					Status = "Completed"
				}
			};

			context.Sales.AddRange(sales);
			context.SaveChanges();
		}

		private static void SeedServiceOrders(ApplicationDbContext context)
		{
			if (context.ServiceOrders.Any())
				return;

			var clients = context.Clients.ToList();
			var cars = context.Cars.ToList();
			var managers = context.Users
				.Where(u => u.Role!.Name == "Менеджер")
				.ToList();

			if (clients.Count == 0 || cars.Count == 0 || managers.Count == 0)
				return;

			Client C(string lastName) => clients.First(c => c.LastName == lastName);
			Car V(string brand, string model) => cars.First(c => c.Brand == brand && c.Model == model);
			User M(int idx) => managers[idx % managers.Count];

			var now = DateTime.UtcNow;
			var orders = new List<ServiceOrder>
			{
				new ServiceOrder
				{
					ClientId = C("Иванов").Id,
					CarId = V("Volvo", "EX30").Id,
					ManagerId = M(0).Id,
					WorkDescription = "Плановое ТО-1, замена воздушного фильтра",
					PlannedCost = 35_000m,
					FinalCost = 32_500m,
					Status = "Completed",
					CreatedAt = now.AddDays(-30),
					CompletedAt = now.AddDays(-29)
				},
				new ServiceOrder
				{
					ClientId = C("Соколов").Id,
					CarId = V("Polestar", "3").Id,
					ManagerId = M(1).Id,
					WorkDescription = "Диагностика подвески после удара",
					PlannedCost = 18_000m,
					FinalCost = null,
					Status = "InProgress",
					CreatedAt = now.AddDays(-2),
					CompletedAt = null
				},
				new ServiceOrder
				{
					ClientId = C("Кузнецова").Id,
					CarId = V("Lotus", "Eletre").Id,
					ManagerId = M(2).Id,
					WorkDescription = "Замена шин на летние",
					PlannedCost = 22_000m,
					FinalCost = null,
					Status = "Accepted",
					CreatedAt = now.AddDays(-1),
					CompletedAt = null
				}
			};

			context.ServiceOrders.AddRange(orders);
			context.SaveChanges();
		}
	}
}
