using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class SupplierServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static SupplierService CreateService(ApplicationDbContext context)
    {
        return new SupplierService(new SupplierRepository(context));
    }

    private static Supplier CreateSupplier(
        string name = "Toyota Supplier",
        string contactPerson = "Иван Иванов",
        string phone = "+79990000000",
        string email = "supplier@test.com",
        string address = "Москва, ул. Тестовая, 1")
    {
        return new Supplier
        {
            Name = name,
            ContactPerson = contactPerson,
            Phone = phone,
            Email = email,
            Address = address
        };
    }

    private static Car CreateCar(int supplierId)
    {
        return new Car
        {
            Brand = "Toyota",
            Model = "Camry",
            Year = 2022,
            Price = 2_500_000,
            Vin = "VIN00000000000001",
            Color = "Black",
            Type = "Новый",
            Status = "Available",
            SupplierId = supplierId,
            IsActive = true
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenSuppliersExist_ReturnsAllSuppliers()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.AddRange(
            CreateSupplier(name: "Toyota Supplier"),
            CreateSupplier(name: "BMW Supplier", email: "bmw@test.com")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(s => s.Name).Should().Contain("Toyota Supplier");
        result.Select(s => s.Name).Should().Contain("BMW Supplier");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSuppliersExist_ReturnsEmptyList()
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
    public async Task GetByIdAsync_WhenSupplierExists_ReturnsSupplier()
    {
        // Arrange
        await using var context = CreateContext();
        var supplier = CreateSupplier();

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(supplier.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(supplier.Id);
        result.Name.Should().Be("Toyota Supplier");
        result.ContactPerson.Should().Be("Иван Иванов");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSupplierDoesNotExist_ReturnsNull()
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
    public async Task CreateAsync_WithValidSupplier_AddsSupplierAndReturnsCreatedSupplier()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var supplier = CreateSupplier();

        // Act
        var result = await service.CreateAsync(supplier);

        // Assert
        result.Should().BeSameAs(supplier);
        result.Id.Should().BeGreaterThan(0);

        var supplierFromDb = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == result.Id);
        supplierFromDb.Should().NotBeNull();
        supplierFromDb!.Name.Should().Be("Toyota Supplier");
        supplierFromDb.Email.Should().Be("supplier@test.com");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFields_AddsSupplier()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var supplier = CreateSupplier(
            name: string.Empty,
            contactPerson: string.Empty,
            phone: string.Empty,
            email: string.Empty,
            address: string.Empty);

        // Act
        var result = await service.CreateAsync(supplier);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().BeEmpty();
        result.ContactPerson.Should().BeEmpty();

        var suppliersCount = await context.Suppliers.CountAsync();
        suppliersCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WithNullSupplier_ThrowsNullReferenceException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var act = async () => await service.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenSupplierExists_UpdatesAllFieldsAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var supplier = CreateSupplier();

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedSupplier = CreateSupplier(
            name: "Updated Supplier",
            contactPerson: "Петр Петров",
            phone: "+78881112233",
            email: "updated@test.com",
            address: "Санкт-Петербург, Невский проспект, 10");

        // Act
        var result = await service.UpdateAsync(supplier.Id, updatedSupplier);

        // Assert
        result.Should().BeTrue();

        var supplierFromDb = await context.Suppliers.FirstAsync(s => s.Id == supplier.Id);
        supplierFromDb.Name.Should().Be("Updated Supplier");
        supplierFromDb.ContactPerson.Should().Be("Петр Петров");
        supplierFromDb.Phone.Should().Be("+78881112233");
        supplierFromDb.Email.Should().Be("updated@test.com");
        supplierFromDb.Address.Should().Be("Санкт-Петербург, Невский проспект, 10");
    }

    [Fact]
    public async Task UpdateAsync_WhenSupplierDoesNotExist_ReturnsFalseAndDoesNotCreateSupplier()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var updatedSupplier = CreateSupplier(name: "Updated Supplier");

        // Act
        var result = await service.UpdateAsync(999, updatedSupplier);

        // Assert
        result.Should().BeFalse();

        var suppliersCount = await context.Suppliers.CountAsync();
        suppliersCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WithNullSupplierAndExistingSupplier_ThrowsNullReferenceException()
    {
        // Arrange
        await using var context = CreateContext();
        var supplier = CreateSupplier();

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var act = async () => await service.UpdateAsync(supplier.Id, null!);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenSupplierExistsAndHasNoCars_DeletesSupplierAndReturnsSuccessMessage()
    {
        // Arrange
        await using var context = CreateContext();
        var supplier = CreateSupplier();

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(supplier.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Поставщик удален");

        var supplierFromDb = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id);
        supplierFromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenSupplierDoesNotExist_ReturnsFalseAndNotFoundMessage()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Поставщик не найден");
    }

    [Fact]
    public async Task DeleteAsync_WhenSupplierHasCars_ReturnsFalseAndDoesNotDeleteSupplier()
    {
        // Arrange
        await using var context = CreateContext();
        var supplier = CreateSupplier();

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        context.Cars.Add(CreateCar(supplier.Id));
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(supplier.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Нельзя удалить поставщика, так как к нему привязаны автомобили");

        var supplierFromDb = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id);
        supplierFromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_WithNameFilter_ReturnsMatchingSuppliers()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.AddRange(
            CreateSupplier(name: "Toyota Supplier"),
            CreateSupplier(name: "BMW Supplier", email: "bmw@test.com")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.SearchAsync("Toyota", null, null)).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Toyota Supplier");
    }

    [Fact]
    public async Task SearchAsync_WithContactPersonFilter_ReturnsMatchingSuppliers()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.AddRange(
            CreateSupplier(contactPerson: "Иван Иванов"),
            CreateSupplier(name: "BMW Supplier", contactPerson: "Петр Петров", email: "bmw@test.com")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.SearchAsync(null, "Петр", null)).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].ContactPerson.Should().Be("Петр Петров");
    }

    [Fact]
    public async Task SearchAsync_WithPhoneFilter_ReturnsMatchingSuppliers()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.AddRange(
            CreateSupplier(phone: "+79990000000"),
            CreateSupplier(name: "BMW Supplier", phone: "+78881112233", email: "bmw@test.com")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.SearchAsync(null, null, "888")).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Phone.Should().Be("+78881112233");
    }

    [Fact]
    public async Task SearchAsync_WithSeveralFilters_ReturnsIntersection()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.AddRange(
            CreateSupplier(name: "Toyota Supplier", contactPerson: "Иван Иванов", phone: "+79990000000"),
            CreateSupplier(name: "Toyota Parts", contactPerson: "Петр Петров", phone: "+78881112233", email: "parts@test.com"),
            CreateSupplier(name: "BMW Supplier", contactPerson: "Петр Петров", phone: "+78881112233", email: "bmw@test.com")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.SearchAsync("Toyota", "Петр", "888")).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Toyota Parts");
    }

    [Fact]
    public async Task SearchAsync_WithNullAndEmptyFilters_ReturnsAllSuppliers()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.AddRange(
            CreateSupplier(name: "Toyota Supplier"),
            CreateSupplier(name: "BMW Supplier", email: "bmw@test.com")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.SearchAsync(null, string.Empty, "   ")).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WhenNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Suppliers.Add(CreateSupplier(name: "Toyota Supplier"));
        await context.SaveChangesAsync();

        // Act
        var result = await service.SearchAsync("Mercedes", null, null);

        // Assert
        result.Should().BeEmpty();
    }
}