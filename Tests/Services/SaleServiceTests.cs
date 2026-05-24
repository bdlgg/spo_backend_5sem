using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class SaleServiceTests
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
            CreateClient(id: 2, firstName: "Мария", lastName: "Иванова", email: "client2@test.com")
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

    private static SaleService CreateService(ApplicationDbContext context)
    {
        return new SaleService(
            new SaleRepository(context),
            new CarRepository(context),
            new ClientRepository(context),
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
        bool isActive = true,
        string status = "Available",
        decimal price = 2_000_000)
    {
        return new Car
        {
            Id = id,
            Brand = brand,
            Model = model,
            Year = 2022,
            Price = price,
            Vin = vin,
            Color = "Black",
            Type = "Новый",
            Status = status,
            IsActive = isActive,
            SupplierId = 1
        };
    }

    private static Sale CreateSale(
        int clientId = 1,
        int carId = 1,
        int userId = 1,
        decimal finalPrice = 1_800_000,
        string status = "Completed",
        DateTime? saleDate = null)
    {
        return new Sale
        {
            ClientId = clientId,
            CarId = carId,
            UserId = userId,
            FinalPrice = finalPrice,
            Status = status,
            SaleDate = saleDate ?? new DateTime(2026, 5, 1)
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenSalesExist_ReturnsSalesWithClientCarAndUser()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(clientId: 1, carId: 1, userId: 1, finalPrice: 1_500_000),
            CreateSale(clientId: 2, carId: 2, userId: 2, finalPrice: 3_000_000)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(sale =>
            sale.Client != null &&
            sale.Car != null &&
            sale.User != null);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSalesExist_ReturnsEmptyList()
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
    public async Task GetByIdAsync_WhenSaleExists_ReturnsSaleWithRelatedEntities()
    {
        // Arrange
        await using var context = CreateContext();

        var sale = CreateSale();
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(sale.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sale.Id);
        result.Client.Should().NotBeNull();
        result.Car.Should().NotBeNull();
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenSaleDoesNotExist_ReturnsNull()
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
    public async Task GetByClientIdAsync_WhenSalesExist_ReturnsOnlyClientSales()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(clientId: 1, carId: 1, userId: 1),
            CreateSale(clientId: 1, carId: 2, userId: 2),
            CreateSale(clientId: 2, carId: 2, userId: 1)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByClientIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(sale => sale.ClientId == 1);
        result.Should().OnlyContain(sale =>
            sale.Client != null &&
            sale.Car != null &&
            sale.User != null);
    }

    [Fact]
    public async Task GetByClientIdAsync_WhenClientHasNoSales_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.Add(CreateSale(clientId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByClientIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenSalesExist_ReturnsOnlyUserSales()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(clientId: 1, carId: 1, userId: 1),
            CreateSale(clientId: 2, carId: 2, userId: 1),
            CreateSale(clientId: 1, carId: 2, userId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetByUserIdAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(sale => sale.UserId == 1);
        result.Should().OnlyContain(sale =>
            sale.Client != null &&
            sale.Car != null &&
            sale.User != null);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenUserHasNoSales_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.Add(CreateSale(userId: 1));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByUserIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidSaleAndEmptyStatus_CreatesSaleWithCompletedStatusMarksCarAsSoldAndWritesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = new Sale
        {
            ClientId = 1,
            CarId = 1,
            UserId = 999,
            FinalPrice = 1_700_000,
            Status = "",
            SaleDate = new DateTime(2000, 1, 1)
        };

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Сделка успешно создана");
        result.sale.Should().NotBeNull();

        var saleFromDb = await context.Sales
            .Include(s => s.Client)
            .Include(s => s.Car)
            .Include(s => s.User)
            .FirstAsync(s => s.Id == sale.Id);

        saleFromDb.ClientId.Should().Be(1);
        saleFromDb.CarId.Should().Be(1);
        saleFromDb.UserId.Should().Be(1);
        saleFromDb.FinalPrice.Should().Be(1_700_000);
        saleFromDb.Status.Should().Be("Completed");
        saleFromDb.SaleDate.Should().NotBe(new DateTime(2000, 1, 1));

        var carFromDb = await context.Cars.FirstAsync(c => c.Id == 1);
        carFromDb.Status.Should().Be("Sold");

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SALE_CREATE" &&
            log.EntityName == "Sale" &&
            log.EntityId == sale.Id &&
            log.Result == "Success");
    }

    [Fact]
    public async Task CreateAsync_WithValidSaleAndCustomStatus_PreservesStatus()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = CreateSale(
            clientId: 1,
            carId: 1,
            userId: 999,
            finalPrice: 1_700_000,
            status: "Pending");

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeTrue();

        var saleFromDb = await context.Sales.FirstAsync(s => s.Id == sale.Id);
        saleFromDb.Status.Should().Be("Pending");
        saleFromDb.UserId.Should().Be(1);

        var carFromDb = await context.Cars.FirstAsync(c => c.Id == 1);
        carFromDb.Status.Should().Be("Sold");
    }

    [Fact]
    public async Task CreateAsync_WhenCarDoesNotExist_ReturnsErrorAndDoesNotCreateSale()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = CreateSale(clientId: 1, carId: 999);

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль не найден");
        result.sale.Should().BeNull();

        context.Sales.Should().BeEmpty();
        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenClientDoesNotExist_ReturnsErrorAndDoesNotCreateSale()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = CreateSale(clientId: 999, carId: 1);

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Клиент не найден");
        result.sale.Should().BeNull();

        context.Sales.Should().BeEmpty();

        var carFromDb = await context.Cars.FirstAsync(c => c.Id == 1);
        carFromDb.Status.Should().Be("Available");

        context.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1000")]
    public async Task CreateAsync_WithNotPositiveFinalPrice_ReturnsErrorAndDoesNotCreateSale(string finalPriceText)
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = CreateSale(
            clientId: 1,
            carId: 1,
            finalPrice: decimal.Parse(finalPriceText));

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Итоговая цена должна быть больше нуля");
        result.sale.Should().BeNull();

        context.Sales.Should().BeEmpty();

        var carFromDb = await context.Cars.FirstAsync(c => c.Id == 1);
        carFromDb.Status.Should().Be("Available");

        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenCarAlreadySold_ReturnsErrorWritesFailedAuditLogAndDoesNotCreateSale()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = CreateSale(clientId: 1, carId: 4);

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Этот автомобиль уже продан");
        result.sale.Should().BeNull();

        context.Sales.Should().BeEmpty();

        context.AuditLogs.Should().ContainSingle(log =>
            log.ActionType == "SALE_CREATE" &&
            log.EntityName == "Sale" &&
            log.EntityId == null &&
            log.Result == "Failed");
    }

    [Fact]
    public async Task CreateAsync_WhenCarIsInactive_ReturnsErrorAndDoesNotCreateSale()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var sale = CreateSale(clientId: 1, carId: 3);

        // Act
        var result = await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль неактивен");
        result.sale.Should().BeNull();

        context.Sales.Should().BeEmpty();
        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithNullSale_ThrowsNullReferenceException()
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
    public async Task UpdateAsync_WhenSaleExists_UpdatesMainFieldsAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();

        var sale = CreateSale(
            clientId: 1,
            carId: 1,
            userId: 1,
            finalPrice: 1_500_000,
            status: "Completed",
            saleDate: new DateTime(2026, 5, 1));

        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedSale = CreateSale(
            clientId: 2,
            carId: 2,
            userId: 2,
            finalPrice: 2_500_000,
            status: "Refunded",
            saleDate: new DateTime(2030, 1, 1));

        // Act
        var result = await service.UpdateAsync(sale.Id, updatedSale);

        // Assert
        result.Should().BeTrue();

        var saleFromDb = await context.Sales.FirstAsync(s => s.Id == sale.Id);
        saleFromDb.ClientId.Should().Be(2);
        saleFromDb.CarId.Should().Be(2);
        saleFromDb.FinalPrice.Should().Be(2_500_000);
        saleFromDb.Status.Should().Be("Refunded");

        saleFromDb.UserId.Should().Be(1);
        saleFromDb.SaleDate.Should().Be(new DateTime(2026, 5, 1));
    }

    [Fact]
    public async Task UpdateAsync_WhenSaleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var updatedSale = CreateSale(
            clientId: 2,
            carId: 2,
            finalPrice: 2_500_000,
            status: "Refunded");

        // Act
        var result = await service.UpdateAsync(999, updatedSale);

        // Assert
        result.Should().BeFalse();
        context.Sales.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithNullSale_WhenExistingSaleFound_ThrowsNullReferenceException()
    {
        // Arrange
        await using var context = CreateContext();

        var sale = CreateSale();
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var act = async () => await service.UpdateAsync(sale.Id, null!);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();

        var saleFromDb = await context.Sales.FirstAsync(s => s.Id == sale.Id);
        saleFromDb.FinalPrice.Should().Be(1_800_000);
        saleFromDb.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task UpdateAsync_WithNullSale_WhenSaleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateAsync(999, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenSaleExists_RemovesSaleAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();

        var sale = CreateSale();
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(sale.Id);

        // Assert
        result.Should().BeTrue();

        var saleFromDb = await context.Sales.FindAsync(sale.Id);
        saleFromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenSaleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
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

        var sale = CreateSale();

        // Act
        var act = async () => await service.CreateAsync(sale, currentUserId: 1);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        var sale = CreateSale();

        // Act
        var act = async () => await service.UpdateAsync(1, sale);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.DeleteAsync(1);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}