using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class ServiceOrderServiceTests
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

        context.Roles.Add(new Role
        {
            Id = 1,
            Name = "Менеджер",
            Description = "Test manager role"
        });

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
            }
        );

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", email: "client1@test.com"),
            CreateClient(id: 2, firstName: "Иван", lastName: "Петров", email: "client2@test.com")
        );

        context.Cars.AddRange(
            CreateCar(id: 1, brand: "Toyota", model: "Camry", vin: "VIN00000000000001"),
            CreateCar(id: 2, brand: "BMW", model: "X5", vin: "VIN00000000000002"),
            CreateCar(id: 3, brand: "Audi", model: "A6", vin: "VIN00000000000003", isActive: false)
        );

        context.SaveChanges();

        return context;
    }

    private static ServiceOrderService CreateService(ApplicationDbContext context)
    {
        return new ServiceOrderService(
            new ServiceOrderRepository(context),
            new ClientRepository(context),
            new CarRepository(context),
            new AuditService(new AuditRepository(context)));
    }

    private static Client CreateClient(
        int id,
        string firstName,
        string lastName,
        string email)
    {
        return new Client
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Phone = "+79991112233",
            Email = email
        };
    }

    private static Car CreateCar(
        int id,
        string brand,
        string model,
        string vin,
        bool isActive = true)
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
            Status = "Available",
            IsActive = isActive,
            SupplierId = 1
        };
    }

    private static ServiceOrder CreateServiceOrder(
        int clientId = 1,
        int carId = 1,
        int managerId = 1,
        string status = "Accepted",
        decimal plannedCost = 100_000,
        decimal? finalCost = null,
        DateTime? createdAt = null,
        DateTime? completedAt = null)
    {
        return new ServiceOrder
        {
            ClientId = clientId,
            CarId = carId,
            ManagerId = managerId,
            WorkDescription = "Замена масла и диагностика",
            PlannedCost = plannedCost,
            FinalCost = finalCost,
            Status = status,
            CreatedAt = createdAt ?? new DateTime(2026, 5, 1),
            CompletedAt = completedAt
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenOrdersExist_ReturnsOrdersWithClientCarAndManagerOrderedByCreatedAtDescending()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.AddRange(
            CreateServiceOrder(clientId: 1, carId: 1, managerId: 1, createdAt: new DateTime(2026, 5, 1)),
            CreateServiceOrder(clientId: 2, carId: 2, managerId: 2, createdAt: new DateTime(2026, 5, 10))
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().Be(new DateTime(2026, 5, 10));
        result[1].CreatedAt.Should().Be(new DateTime(2026, 5, 1));

        result.Should().OnlyContain(order =>
            order.Client != null &&
            order.Car != null &&
            order.Manager != null);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoOrdersExist_ReturnsEmptyList()
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
    public async Task GetByIdAsync_WhenOrderExists_ReturnsOrderWithRelatedEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder();
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(serviceOrder.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(serviceOrder.Id);
        result.Client.Should().NotBeNull();
        result.Car.Should().NotBeNull();
        result.Manager.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
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
    public async Task GetByClientIdAsync_WhenOrdersExist_ReturnsOnlyClientOrders()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.AddRange(
            CreateServiceOrder(clientId: 1, carId: 1, managerId: 1, createdAt: new DateTime(2026, 5, 1)),
            CreateServiceOrder(clientId: 1, carId: 2, managerId: 2, createdAt: new DateTime(2026, 5, 2)),
            CreateServiceOrder(clientId: 2, carId: 2, managerId: 2, createdAt: new DateTime(2026, 5, 3))
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByClientIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(order => order.ClientId == 1);
        result[0].CreatedAt.Should().Be(new DateTime(2026, 5, 2));
        result[1].CreatedAt.Should().Be(new DateTime(2026, 5, 1));
    }

    [Fact]
    public async Task GetByClientIdAsync_WhenClientHasNoOrders_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.Add(CreateServiceOrder(clientId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByClientIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByManagerIdAsync_WhenOrdersExist_ReturnsOnlyManagerOrders()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.AddRange(
            CreateServiceOrder(clientId: 1, carId: 1, managerId: 1),
            CreateServiceOrder(clientId: 2, carId: 2, managerId: 1),
            CreateServiceOrder(clientId: 1, carId: 2, managerId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByManagerIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(order => order.ManagerId == 1);
    }

    [Fact]
    public async Task GetByManagerIdAsync_WhenManagerHasNoOrders_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.Add(CreateServiceOrder(managerId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByManagerIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCarIdAsync_WhenOrdersExist_ReturnsOnlyCarOrders()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.AddRange(
            CreateServiceOrder(clientId: 1, carId: 1, managerId: 1),
            CreateServiceOrder(clientId: 2, carId: 1, managerId: 2),
            CreateServiceOrder(clientId: 1, carId: 2, managerId: 1)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByCarIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(order => order.CarId == 1);
    }

    [Fact]
    public async Task GetByCarIdAsync_WhenCarHasNoOrders_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.ServiceOrders.Add(CreateServiceOrder(carId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByCarIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidOrder_CreatesOrderSetsDefaultFieldsAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var serviceOrder = new ServiceOrder
        {
            ClientId = 1,
            CarId = 1,
            ManagerId = 999,
            WorkDescription = "Плановое техническое обслуживание",
            PlannedCost = 50_000,
            FinalCost = 70_000,
            Status = "Completed",
            CompletedAt = new DateTime(2026, 5, 1)
        };

        // Act
        var result = await service.CreateAsync(serviceOrder, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Заказ-наряд успешно создан");
        result.serviceOrder.Should().NotBeNull();

        var orderFromDb = await context.ServiceOrders
            .Include(order => order.Client)
            .Include(order => order.Car)
            .Include(order => order.Manager)
            .FirstAsync(order => order.Id == serviceOrder.Id);

        orderFromDb.ClientId.Should().Be(1);
        orderFromDb.CarId.Should().Be(1);
        orderFromDb.ManagerId.Should().Be(1);
        orderFromDb.Status.Should().Be("Accepted");
        orderFromDb.FinalCost.Should().BeNull();
        orderFromDb.CompletedAt.Should().BeNull();
        orderFromDb.CreatedAt.Should().NotBe(default);

        orderFromDb.Client.Should().NotBeNull();
        orderFromDb.Car.Should().NotBeNull();
        orderFromDb.Manager.Should().NotBeNull();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SERVICE_ORDER_CREATE" &&
            log.EntityName == "ServiceOrder" &&
            log.EntityId == serviceOrder.Id &&
            log.Result == "Success");
    }

    [Fact]
    public async Task CreateAsync_WhenClientDoesNotExist_ReturnsErrorAndWritesFailedAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var serviceOrder = CreateServiceOrder(clientId: 999, carId: 1);

        // Act
        var result = await service.CreateAsync(serviceOrder, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Клиент не найден");
        result.serviceOrder.Should().BeNull();

        context.ServiceOrders.Should().BeEmpty();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SERVICE_ORDER_CREATE" &&
            log.EntityName == "ServiceOrder" &&
            log.EntityId == null &&
            log.Result == "Failed" &&
            log.Details.Contains("несуществующего клиента"));
    }

    [Fact]
    public async Task CreateAsync_WhenCarDoesNotExist_ReturnsErrorAndWritesFailedAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var serviceOrder = CreateServiceOrder(clientId: 1, carId: 999);

        // Act
        var result = await service.CreateAsync(serviceOrder, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль не найден");
        result.serviceOrder.Should().BeNull();

        context.ServiceOrders.Should().BeEmpty();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SERVICE_ORDER_CREATE" &&
            log.EntityName == "ServiceOrder" &&
            log.EntityId == null &&
            log.Result == "Failed" &&
            log.Details.Contains("несуществующего автомобиля"));
    }

    [Fact]
    public async Task CreateAsync_WhenCarIsInactive_ReturnsErrorAndDoesNotCreateOrder()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var serviceOrder = CreateServiceOrder(clientId: 1, carId: 3);

        // Act
        var result = await service.CreateAsync(serviceOrder, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль неактивен");
        result.serviceOrder.Should().BeNull();

        context.ServiceOrders.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateAsync_WithEmptyWorkDescription_ReturnsErrorAndDoesNotCreateOrder(string workDescription)
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var serviceOrder = CreateServiceOrder();
        serviceOrder.WorkDescription = workDescription;

        // Act
        var result = await service.CreateAsync(serviceOrder, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Описание работ обязательно");
        result.serviceOrder.Should().BeNull();

        context.ServiceOrders.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task CreateAsync_WithNotPositivePlannedCost_ReturnsErrorAndDoesNotCreateOrder(decimal plannedCost)
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var serviceOrder = CreateServiceOrder(plannedCost: plannedCost);

        // Act
        var result = await service.CreateAsync(serviceOrder, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Плановая стоимость должна быть больше нуля");
        result.serviceOrder.Should().BeNull();

        context.ServiceOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithNullServiceOrder_ThrowsNullReferenceException()
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
    public async Task UpdateStatusAsync_WhenOrderDoesNotExist_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(999, "InProgress", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Заказ-наряд не найден");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UpdateStatusAsync_WithEmptyStatus_ReturnsError(string? status)
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "Accepted");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, status!, null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Статус обязателен");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "Accepted");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Paused", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Недопустимый статус, возможные статусы: Completed, InProgress, Cancelled, Accepted(возврат невозможен)");
    }

    [Fact]
    public async Task UpdateStatusAsync_FromAcceptedToInProgress_UpdatesStatusAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "Accepted");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, " InProgress ", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Статус заказ-наряда успешно обновлен");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("InProgress");
        orderFromDb.FinalCost.Should().BeNull();
        orderFromDb.CompletedAt.Should().BeNull();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SERVICE_ORDER_STATUS_UPDATE" &&
            log.EntityName == "ServiceOrder" &&
            log.EntityId == serviceOrder.Id &&
            log.Result == "Success" &&
            log.Details.Contains("InProgress"));
    }

    [Fact]
    public async Task UpdateStatusAsync_FromInProgressToCompleted_WithValidFinalCost_CompletesOrderAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "InProgress");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Completed", 120_000, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Статус заказ-наряда успешно обновлен");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("Completed");
        orderFromDb.FinalCost.Should().Be(120_000);
        orderFromDb.CompletedAt.Should().NotBeNull();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SERVICE_ORDER_STATUS_UPDATE" &&
            log.EntityName == "ServiceOrder" &&
            log.EntityId == serviceOrder.Id &&
            log.Result == "Success" &&
            log.Details.Contains("Completed"));
    }

    [Fact]
    public async Task UpdateStatusAsync_FromAcceptedToCompleted_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "Accepted");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Completed", 120_000, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Нельзя завершить заказ-наряд без статуса InProgress");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("Accepted");
        orderFromDb.FinalCost.Should().BeNull();
        orderFromDb.CompletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1000")]
    public async Task UpdateStatusAsync_ToCompleted_WithInvalidFinalCost_ReturnsError(string? finalCostText)
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "InProgress");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        decimal? finalCost = finalCostText is null
            ? null
            : decimal.Parse(finalCostText);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Completed", finalCost, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Для завершения заказ-наряда нужно указать итоговую стоимость больше нуля");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("InProgress");
        orderFromDb.FinalCost.Should().BeNull();
        orderFromDb.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_FromAcceptedToCancelled_UpdatesStatusAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "Accepted");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Cancelled", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Статус заказ-наряда успешно обновлен");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("Cancelled");

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SERVICE_ORDER_STATUS_UPDATE" &&
            log.EntityName == "ServiceOrder" &&
            log.EntityId == serviceOrder.Id &&
            log.Result == "Success" &&
            log.Details.Contains("Cancelled"));
    }

    [Fact]
    public async Task UpdateStatusAsync_ToAccepted_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "InProgress");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Accepted", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Возврат в статус Accepted невозможен");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOrderAlreadyCompleted_ReturnsErrorAndDoesNotChangeOrder()
    {
        // Arrange
        await using var context = CreateContext();
        var completedAt = new DateTime(2026, 5, 10);
        var serviceOrder = CreateServiceOrder(
            status: "Completed",
            finalCost: 100_000,
            completedAt: completedAt);

        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "Cancelled", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Завершенный заказ-наряд нельзя изменить");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("Completed");
        orderFromDb.FinalCost.Should().Be(100_000);
        orderFromDb.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOrderAlreadyCancelled_ReturnsErrorAndDoesNotChangeOrder()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "Cancelled");

        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "InProgress", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Отмененный заказ-наряд нельзя изменить");

        var orderFromDb = await context.ServiceOrders.FirstAsync(order => order.Id == serviceOrder.Id);
        orderFromDb.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task UpdateStatusAsync_FromInProgressToInProgress_ReturnsError()
    {
        // Arrange
        await using var context = CreateContext();
        var serviceOrder = CreateServiceOrder(status: "InProgress");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateStatusAsync(serviceOrder.Id, "InProgress", null, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("в InProgress можно перевести только заказ-наряд со статусом Accepted");
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
    public async Task UpdateStatusAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.UpdateStatusAsync(1, "InProgress", null, currentUserId: 1);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}