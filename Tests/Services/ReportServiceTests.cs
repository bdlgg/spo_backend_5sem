using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class ReportServiceTests
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

        context.Clients.Add(new Client
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Client",
            Phone = "+79991112233",
            Email = "client@test.com"
        });

        context.Cars.AddRange(
            CreateCar(id: 1, vin: "VIN00000000000001"),
            CreateCar(id: 2, brand: "BMW", model: "X5", vin: "VIN00000000000002")
        );

        context.SaveChanges();

        return context;
    }

    private static ReportService CreateService(ApplicationDbContext context)
    {
        return new ReportService(context);
    }

    private static Car CreateCar(
        int id,
        string brand = "Toyota",
        string model = "Camry",
        string vin = "VIN00000000000001")
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
            IsActive = true,
            SupplierId = 1
        };
    }

    private static Sale CreateSale(
        int userId,
        decimal finalPrice,
        DateTime saleDate,
        string status = "Completed",
        int carId = 1,
        int clientId = 1)
    {
        return new Sale
        {
            UserId = userId,
            ClientId = clientId,
            CarId = carId,
            FinalPrice = finalPrice,
            SaleDate = saleDate,
            Status = status
        };
    }

    [Fact]
    public async Task GetRevenueReportAsync_WithNullDates_ReturnsTotalsForAllCompletedSalesOnly()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(userId: 1, finalPrice: 1_000_000, saleDate: new DateTime(2026, 5, 1)),
            CreateSale(userId: 2, finalPrice: 2_000_000, saleDate: new DateTime(2026, 5, 2), carId: 2),
            CreateSale(userId: 1, finalPrice: 5_000_000, saleDate: new DateTime(2026, 5, 3), status: "Canceled")
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetRevenueReportAsync(null, null);

        // Assert
        result.From.Should().BeNull();
        result.To.Should().BeNull();
        result.TotalRevenue.Should().Be(3_000_000);
        result.CompletedSalesCount.Should().Be(2);
        result.AverageSalesAmount.Should().Be(1_500_000);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WithDateRange_FiltersCompletedSalesInclusively()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var from = new DateTime(2026, 5, 1);
        var to = new DateTime(2026, 5, 31);

        context.Sales.AddRange(
            CreateSale(userId: 1, finalPrice: 100_000, saleDate: new DateTime(2026, 4, 30)),
            CreateSale(userId: 1, finalPrice: 1_000_000, saleDate: from),
            CreateSale(userId: 2, finalPrice: 2_000_000, saleDate: to, carId: 2),
            CreateSale(userId: 2, finalPrice: 300_000, saleDate: new DateTime(2026, 6, 1), carId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetRevenueReportAsync(from, to);

        // Assert
        result.From.Should().Be(from);
        result.To.Should().Be(to);
        result.TotalRevenue.Should().Be(3_000_000);
        result.CompletedSalesCount.Should().Be(2);
        result.AverageSalesAmount.Should().Be(1_500_000);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WhenNoSalesExist_ReturnsZeroReport()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetRevenueReportAsync(null, null);

        // Assert
        result.TotalRevenue.Should().Be(0);
        result.CompletedSalesCount.Should().Be(0);
        result.AverageSalesAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WhenOnlyNotCompletedSalesExist_ReturnsZeroReport()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(userId: 1, finalPrice: 1_000_000, saleDate: new DateTime(2026, 5, 1), status: "Canceled"),
            CreateSale(userId: 2, finalPrice: 2_000_000, saleDate: new DateTime(2026, 5, 2), status: "Pending", carId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetRevenueReportAsync(null, null);

        // Assert
        result.TotalRevenue.Should().Be(0);
        result.CompletedSalesCount.Should().Be(0);
        result.AverageSalesAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WhenFromIsGreaterThanTo_ReturnsZeroReport()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.Add(CreateSale(
            userId: 1,
            finalPrice: 1_000_000,
            saleDate: new DateTime(2026, 5, 15)));
        await context.SaveChangesAsync();

        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 5, 1);

        // Act
        var result = await service.GetRevenueReportAsync(from, to);

        // Assert
        result.From.Should().Be(from);
        result.To.Should().Be(to);
        result.TotalRevenue.Should().Be(0);
        result.CompletedSalesCount.Should().Be(0);
        result.AverageSalesAmount.Should().Be(0);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.GetRevenueReportAsync(null, null);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WithNullDates_GroupsManagersAndOrdersByRevenueDescending()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(userId: 1, finalPrice: 1_000_000, saleDate: new DateTime(2026, 5, 1)),
            CreateSale(userId: 1, finalPrice: 500_000, saleDate: new DateTime(2026, 5, 2)),
            CreateSale(userId: 2, finalPrice: 3_000_000, saleDate: new DateTime(2026, 5, 3), carId: 2),
            CreateSale(userId: 2, finalPrice: 10_000_000, saleDate: new DateTime(2026, 5, 4), status: "Canceled", carId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetManagersPerformanceAsync(null, null)).ToList();

        // Assert
        result.Should().HaveCount(2);

        result[0].ManagerId.Should().Be(2);
        result[0].ManagerFullName.Should().Be("Петр Петров");
        result[0].SalesCount.Should().Be(1);
        result[0].TotalRevenue.Should().Be(3_000_000);

        result[1].ManagerId.Should().Be(1);
        result[1].ManagerFullName.Should().Be("Иван Иванов");
        result[1].SalesCount.Should().Be(2);
        result[1].TotalRevenue.Should().Be(1_500_000);
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WithDateRange_FiltersSalesInclusively()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var from = new DateTime(2026, 5, 1);
        var to = new DateTime(2026, 5, 31);

        context.Sales.AddRange(
            CreateSale(userId: 1, finalPrice: 100_000, saleDate: new DateTime(2026, 4, 30)),
            CreateSale(userId: 1, finalPrice: 1_000_000, saleDate: from),
            CreateSale(userId: 2, finalPrice: 2_000_000, saleDate: to, carId: 2),
            CreateSale(userId: 2, finalPrice: 300_000, saleDate: new DateTime(2026, 6, 1), carId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetManagersPerformanceAsync(from, to)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(new
        {
            ManagerId = 1,
            ManagerFullName = "Иван Иванов",
            SalesCount = 1,
            TotalRevenue = 1_000_000m
        });
        result.Should().ContainEquivalentOf(new
        {
            ManagerId = 2,
            ManagerFullName = "Петр Петров",
            SalesCount = 1,
            TotalRevenue = 2_000_000m
        });
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WhenNoSalesExist_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetManagersPerformanceAsync(null, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WhenOnlyNotCompletedSalesExist_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.AddRange(
            CreateSale(userId: 1, finalPrice: 1_000_000, saleDate: new DateTime(2026, 5, 1), status: "Canceled"),
            CreateSale(userId: 2, finalPrice: 2_000_000, saleDate: new DateTime(2026, 5, 2), status: "Pending", carId: 2)
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetManagersPerformanceAsync(null, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WhenCompletedSaleHasNoUser_IgnoresSale()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.Add(CreateSale(
            userId: 999,
            finalPrice: 1_000_000,
            saleDate: new DateTime(2026, 5, 1)));
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetManagersPerformanceAsync(null, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WhenFromIsGreaterThanTo_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Sales.Add(CreateSale(
            userId: 1,
            finalPrice: 1_000_000,
            saleDate: new DateTime(2026, 5, 15)));
        await context.SaveChangesAsync();

        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 5, 1);

        // Act
        var result = await service.GetManagersPerformanceAsync(from, to);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManagersPerformanceAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.GetManagersPerformanceAsync(null, null);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}