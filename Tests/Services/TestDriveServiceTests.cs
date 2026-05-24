using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class TestDriveServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        context.Suppliers.Add(new Supplier
        {
            Id = 1,
            Name = "Test Supplier",
            ContactPerson = "Test Contact",
            Phone = "+79990000000",
            Email = "supplier@test.com",
            Address = "Test Address"
        });

        context.Roles.AddRange(
            new Role
            {
                Id = 1,
                Name = "Менеджер",
                Description = "Manager role"
            },
            new Role
            {
                Id = 2,
                Name = "Клиент",
                Description = "Client role"
            }
        );

        context.Users.AddRange(
            new User
            {
                Id = 1,
                FirstName = "Иван",
                LastName = "Иванов",
                Email = "manager1@test.com",
                PasswordHash = "hash",
                RoleId = 1,
                IsActive = true
            },
            new User
            {
                Id = 2,
                FirstName = "Петр",
                LastName = "Петров",
                Email = "manager2@test.com",
                PasswordHash = "hash",
                RoleId = 1,
                IsActive = true
            },
            new User
            {
                Id = 3,
                FirstName = "Клиент",
                LastName = "Клиентов",
                Email = "client-user@test.com",
                PasswordHash = "hash",
                RoleId = 2,
                IsActive = true
            }
        );

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", email: "client1@test.com"),
            CreateClient(id: 2, firstName: "Мария", lastName: "Иванова", email: "client2@test.com"),
            CreateClient(id: 3, firstName: "Пользователь", lastName: "Клиент", email: "client-user@test.com", userId: 3)
        );

        context.Cars.AddRange(
            CreateCar(id: 1, brand: "Toyota", model: "Camry", vin: "VIN00000000000001"),
            CreateCar(id: 2, brand: "BMW", model: "X5", vin: "VIN00000000000002"),
            CreateCar(id: 3, brand: "Audi", model: "A6", vin: "VIN00000000000003", isActive: false),
            CreateCar(id: 4, brand: "Mercedes", model: "E-Class", vin: "VIN00000000000004", status: "Sold")
        );

        context.SaveChanges();

        return context;
    }

    private static TestDriveService CreateService(ApplicationDbContext context)
    {
        return new TestDriveService(
            new TestDriveRepository(context),
            new CarRepository(context),
            new ClientRepository(context),
            new AuditService(new AuditRepository(context)));
    }

    private static Client CreateClient(
        int id,
        string firstName,
        string lastName,
        string email,
        int? userId = null)
    {
        return new Client
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Phone = "+79991112233",
            Email = email,
            UserId = userId
        };
    }

    private static Car CreateCar(
        int id,
        string brand,
        string model,
        string vin,
        bool isActive = true,
        string status = "Available")
    {
        return new Car
        {
            Id = id,
            Brand = brand,
            Model = model,
            Year = 2022,
            Price = 2_000_000,
            Vin = vin,
            Color = "Black",
            Type = "Новый",
            Status = status,
            IsActive = isActive,
            SupplierId = 1
        };
    }

    private static TestDrive CreateTestDrive(
        int clientId = 1,
        int carId = 1,
        int managerId = 1,
        DateTime? scheduledAt = null,
        string status = "Scheduled",
        string result = "",
        string comment = "Клиент записан на тест-драйв",
        DateTime? createdAt = null)
    {
        return new TestDrive
        {
            ClientId = clientId,
            CarId = carId,
            ManagerId = managerId,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow.AddDays(2),
            Status = status,
            Result = result,
            Comment = comment,
            CreatedAt = createdAt ?? new DateTime(2026, 5, 1)
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenTestDrivesExist_ReturnsTestDrivesWithClientCarAndManagerOrderedByCreatedAtDescending()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.TestDrives.AddRange(
            CreateTestDrive(clientId: 1, carId: 1, managerId: 1, createdAt: new DateTime(2026, 5, 1)),
            CreateTestDrive(clientId: 2, carId: 2, managerId: 2, createdAt: new DateTime(2026, 5, 10))
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().Be(new DateTime(2026, 5, 10));
        result[1].CreatedAt.Should().Be(new DateTime(2026, 5, 1));

        result.Should().OnlyContain(testDrive =>
            testDrive.Client != null &&
            testDrive.Car != null &&
            testDrive.Manager != null);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoTestDrivesExist_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTestDriveExists_ReturnsTestDriveWithRelatedEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var testDrive = CreateTestDrive();

        context.TestDrives.Add(testDrive);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(testDrive.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testDrive.Id);
        result.Client.Should().NotBeNull();
        result.Car.Should().NotBeNull();
        result.Manager.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTestDriveDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByClientIdAsync_WhenClientHasTestDrives_ReturnsOnlyClientTestDrivesOrderedByCreatedAtDescending()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.TestDrives.AddRange(
            CreateTestDrive(clientId: 1, carId: 1, managerId: 1, createdAt: new DateTime(2026, 5, 1)),
            CreateTestDrive(clientId: 1, carId: 2, managerId: 2, createdAt: new DateTime(2026, 5, 5)),
            CreateTestDrive(clientId: 2, carId: 2, managerId: 1, createdAt: new DateTime(2026, 5, 10))
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByClientIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(testDrive => testDrive.ClientId == 1);
        result[0].CreatedAt.Should().Be(new DateTime(2026, 5, 5));
        result[1].CreatedAt.Should().Be(new DateTime(2026, 5, 1));
    }

    [Fact]
    public async Task GetByClientIdAsync_WhenClientHasNoTestDrives_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.TestDrives.Add(CreateTestDrive(clientId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByClientIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByManagerIdAsync_WhenManagerHasTestDrives_ReturnsOnlyManagerTestDrivesOrderedByCreatedAtDescending()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.TestDrives.AddRange(
            CreateTestDrive(clientId: 1, carId: 1, managerId: 1, createdAt: new DateTime(2026, 5, 1)),
            CreateTestDrive(clientId: 2, carId: 2, managerId: 1, createdAt: new DateTime(2026, 5, 5)),
            CreateTestDrive(clientId: 1, carId: 2, managerId: 2, createdAt: new DateTime(2026, 5, 10))
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByManagerIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(testDrive => testDrive.ManagerId == 1);
        result[0].CreatedAt.Should().Be(new DateTime(2026, 5, 5));
        result[1].CreatedAt.Should().Be(new DateTime(2026, 5, 1));
    }

    [Fact]
    public async Task GetByManagerIdAsync_WhenManagerHasNoTestDrives_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.TestDrives.Add(CreateTestDrive(managerId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByManagerIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidTestDrive_CreatesScheduledTestDriveWithCurrentManagerAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var scheduledAt = DateTime.UtcNow.AddDays(3);
        var testDrive = CreateTestDrive(
            clientId: 1,
            carId: 1,
            managerId: 999,
            scheduledAt: scheduledAt,
            status: "Completed",
            result: "Interested",
            comment: "Тестовый комментарий",
            createdAt: new DateTime(2000, 1, 1));

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Тест-драйв успешно создан");
        result.testDrive.Should().NotBeNull();

        var testDriveFromDb = await context.TestDrives
            .Include(td => td.Client)
            .Include(td => td.Car)
            .Include(td => td.Manager)
            .FirstAsync(td => td.Id == testDrive.Id);

        testDriveFromDb.ClientId.Should().Be(1);
        testDriveFromDb.CarId.Should().Be(1);
        testDriveFromDb.ManagerId.Should().Be(1);
        testDriveFromDb.ScheduledAt.Should().Be(scheduledAt);
        testDriveFromDb.Status.Should().Be("Scheduled");
        testDriveFromDb.CreatedAt.Should().BeAfter(new DateTime(2000, 1, 1));

        testDriveFromDb.Client.Should().NotBeNull();
        testDriveFromDb.Car.Should().NotBeNull();
        testDriveFromDb.Manager.Should().NotBeNull();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "TESTDRIVE_CREATE" &&
            log.EntityName == "TestDrive" &&
            log.EntityId == testDrive.Id &&
            log.Result == "Success");
    }

    [Fact]
    public async Task CreateAsync_WhenCarDoesNotExist_ReturnsErrorAndWritesFailedAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var testDrive = CreateTestDrive(clientId: 1, carId: 999);

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль не найден");
        result.testDrive.Should().BeNull();

        context.TestDrives.Should().BeEmpty();
        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "TESTDRIVE_CREATE" &&
            log.EntityName == "TestDrive" &&
            log.EntityId == null &&
            log.Result == "Failed" &&
            log.Details.Contains("несуществующего автомобиля"));
    }

    [Fact]
    public async Task CreateAsync_WhenClientDoesNotExist_ReturnsErrorAndWritesFailedAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var testDrive = CreateTestDrive(clientId: 999, carId: 1);

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Клиент не найден");
        result.testDrive.Should().BeNull();

        context.TestDrives.Should().BeEmpty();
        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "TESTDRIVE_CREATE" &&
            log.EntityName == "TestDrive" &&
            log.EntityId == null &&
            log.Result == "Failed" &&
            log.Details.Contains("несуществующего клиента"));
    }

    [Fact]
    public async Task CreateAsync_WhenCarIsInactive_ReturnsErrorAndDoesNotCreateTestDrive()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var testDrive = CreateTestDrive(clientId: 1, carId: 3);

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль неактивен");
        result.testDrive.Should().BeNull();

        context.TestDrives.Should().BeEmpty();
        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenCarIsSold_ReturnsErrorAndDoesNotCreateTestDrive()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var testDrive = CreateTestDrive(clientId: 1, carId: 4);

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Нельзя записать на тест-драйв проданный автомобиль");
        result.testDrive.Should().BeNull();

        context.TestDrives.Should().BeEmpty();
        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenScheduledAtIsPast_ReturnsErrorAndDoesNotCreateTestDrive()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var testDrive = CreateTestDrive(scheduledAt: DateTime.UtcNow.AddMinutes(-1));

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Дата тест-драйва должна быть в будущем");
        result.testDrive.Should().BeNull();

        context.TestDrives.Should().BeEmpty();
        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleConflictExists_ReturnsErrorAndWritesFailedAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var scheduledAt = DateTime.UtcNow.AddDays(2);

        context.TestDrives.Add(CreateTestDrive(
            clientId: 1,
            carId: 1,
            managerId: 1,
            scheduledAt: scheduledAt,
            status: "Scheduled"));

        await context.SaveChangesAsync();

        var conflictingTestDrive = CreateTestDrive(
            clientId: 2,
            carId: 1,
            managerId: 2,
            scheduledAt: scheduledAt.AddMinutes(30));

        // Act
        var result = await service.CreateAsync(conflictingTestDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("На это время уже запланирован тест-драйв для данного автомобиля");
        result.testDrive.Should().BeNull();

        context.TestDrives.Should().ContainSingle();
        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "TESTDRIVE_CREATE" &&
            log.EntityName == "TestDrive" &&
            log.EntityId == null &&
            log.Result == "Failed" &&
            log.Details.Contains("Конфликт расписания"));
    }

    [Fact]
    public async Task CreateAsync_WhenExistingTestDriveForOtherCarAtSameTime_CreatesTestDrive()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var scheduledAt = DateTime.UtcNow.AddDays(2);

        context.TestDrives.Add(CreateTestDrive(
            clientId: 1,
            carId: 1,
            scheduledAt: scheduledAt));

        await context.SaveChangesAsync();

        var testDrive = CreateTestDrive(
            clientId: 2,
            carId: 2,
            scheduledAt: scheduledAt);

        // Act
        var result = await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        context.TestDrives.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithNullTestDrive_ThrowsNullReferenceException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var act = async () => await service.CreateAsync(null!, currentUserId: 1);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task CompleteAsync_WhenScheduledTestDriveExists_CompletesTestDriveAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();

        var testDrive = CreateTestDrive(status: "Scheduled");
        context.TestDrives.Add(testDrive);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CompleteAsync(
            testDrive.Id,
            "Interested",
            "Клиент заинтересован",
            currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Тест-драйв успешно завершен");

        var testDriveFromDb = await context.TestDrives.FirstAsync(td => td.Id == testDrive.Id);

        testDriveFromDb.Status.Should().Be("Completed");
        testDriveFromDb.Result.Should().Be("Interested");
        testDriveFromDb.Comment.Should().Be("Клиент заинтересован");

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "TESTDRIVE_COMPLETE" &&
            log.EntityName == "TestDrive" &&
            log.EntityId == testDrive.Id &&
            log.Result == "Success");
    }

    [Fact]
    public async Task CompleteAsync_WhenTestDriveDoesNotExist_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.CompleteAsync(
            999,
            "Interested",
            "Комментарий",
            currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Тест-драйв не найден");
        context.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task CompleteAsync_WhenTestDriveIsNotScheduled_ReturnsErrorAndDoesNotChangeTestDrive(string status)
    {
        // Arrange
        await using var context = CreateContext();

        var testDrive = CreateTestDrive(
            status: status,
            result: "OldResult",
            comment: "Старый комментарий");

        context.TestDrives.Add(testDrive);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CompleteAsync(
            testDrive.Id,
            "Interested",
            "Новый комментарий",
            currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Завершить можно только тест-драйв со статусом Scheduled");

        var testDriveFromDb = await context.TestDrives.FirstAsync(td => td.Id == testDrive.Id);

        testDriveFromDb.Status.Should().Be(status);
        testDriveFromDb.Result.Should().Be("OldResult");
        testDriveFromDb.Comment.Should().Be("Старый комментарий");

        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelAsync_WhenScheduledTestDriveExists_CancelsTestDriveAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();

        var testDrive = CreateTestDrive(
            status: "Scheduled",
            result: "Interested",
            comment: "Старый комментарий");

        context.TestDrives.Add(testDrive);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CancelAsync(
            testDrive.Id,
            "Клиент отменил запись",
            currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Тест-драйв успешно отменен");

        var testDriveFromDb = await context.TestDrives.FirstAsync(td => td.Id == testDrive.Id);

        testDriveFromDb.Status.Should().Be("Cancelled");
        testDriveFromDb.Result.Should().BeEmpty();
        testDriveFromDb.Comment.Should().Be("Клиент отменил запись");

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "TESTDRIVE_CANCEL" &&
            log.EntityName == "TestDrive" &&
            log.EntityId == testDrive.Id &&
            log.Result == "Success");
    }

    [Fact]
    public async Task CancelAsync_WhenTestDriveDoesNotExist_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.CancelAsync(
            999,
            "Комментарий",
            currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Тест-драйв не найден");
        context.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task CancelAsync_WhenTestDriveIsNotScheduled_ReturnsErrorAndDoesNotChangeTestDrive(string status)
    {
        // Arrange
        await using var context = CreateContext();

        var testDrive = CreateTestDrive(
            status: status,
            result: "Interested",
            comment: "Старый комментарий");

        context.TestDrives.Add(testDrive);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.CancelAsync(
            testDrive.Id,
            "Новый комментарий",
            currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Отменить можно только тест-драйв со статусом Scheduled");

        var testDriveFromDb = await context.TestDrives.FirstAsync(td => td.Id == testDrive.Id);

        testDriveFromDb.Status.Should().Be(status);
        testDriveFromDb.Result.Should().Be("Interested");
        testDriveFromDb.Comment.Should().Be("Старый комментарий");

        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.GetAllAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CreateAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        var testDrive = CreateTestDrive();

        // Act
        var act = async () => await service.CreateAsync(testDrive, currentUserId: 1);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CompleteAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.CompleteAsync(
            1,
            "Interested",
            "Комментарий",
            currentUserId: 1);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CancelAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.CancelAsync(
            1,
            "Комментарий",
            currentUserId: 1);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}