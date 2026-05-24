using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class CarServiceTests
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

        context.SaveChanges();

        return context;
    }

    private static CarService CreateService(ApplicationDbContext context)
    {
        return new CarService(new CarRepository(context));
    }

    private static Car CreateValidCar(
        string brand = "Toyota",
        string model = "Camry",
        int year = 2020,
        decimal price = 2_500_000,
        string vin = "VIN00000000000001",
        string status = "Available")
    {
        return new Car
        {
            Brand = brand,
            Model = model,
            Year = year,
            Price = price,
            Vin = vin,
            Color = "Black",
            Type = "Новый",
            Status = status,
            SupplierId = 1,
            IsActive = true,
            Photos = new List<CarPhoto>
            {
                new CarPhoto
                {
                    Url = "https://example.com/car.jpg",
                    SortOrder = 1
                }
            }
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenCarsExist_ReturnsCarsWithSupplierAndPhotos()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        await context.Cars.AddRangeAsync(
            CreateValidCar(vin: "VIN00000000000001"),
            CreateValidCar(brand: "BMW", model: "X5", year: 2022, price: 5_500_000, vin: "VIN00000000000002")
        );
        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Brand == "Toyota" && c.Supplier != null && c.Photos.Any());
        result.Should().Contain(c => c.Brand == "BMW" && c.Supplier != null && c.Photos.Any());
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCars_ReturnsEmptyList()
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
    public async Task GetByIdAsync_WhenCarExists_ReturnsCar()
    {
        // Arrange
        await using var context = CreateContext();
        var car = CreateValidCar();
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(car.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(car.Id);
        result.Brand.Should().Be("Toyota");
        result.Supplier.Should().NotBeNull();
        result.Photos.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCarDoesNotExist_ReturnsNull()
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
    public async Task CreateAsync_WithValidCar_SavesAndReturnsCar()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar();

        // Act
        var result = await service.CreateAsync(car);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Brand.Should().Be("Toyota");

        var savedCar = await context.Cars
            .Include(c => c.Photos)
            .FirstOrDefaultAsync(c => c.Id == result.Id);

        savedCar.Should().NotBeNull();
        savedCar!.Vin.Should().Be("VIN00000000000001");
        savedCar.Photos.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyBrand_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(brand: " ");

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Марка автомобиля обязательна");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyModel_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(model: " ");

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Модель автомобиля обязательна");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyVin_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(vin: " ");

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("VIN обязателен");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WithNotPositivePrice_ThrowsArgumentException(decimal price)
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(price: price);

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Цена должна быть больше нуля");
    }

    [Theory]
    [InlineData(1899)]
    public async Task CreateAsync_WithInvalidYear_ThrowsArgumentException(int year)
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(year: year);

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Некорректный год выпуска");
    }

    [Fact]
    public async Task CreateAsync_WithFutureYearGreaterThanAllowed_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(year: DateTime.UtcNow.Year + 2);

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Некорректный год выпуска");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyStatus_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var car = CreateValidCar(status: " ");

        // Act
        var act = async () => await service.CreateAsync(car);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Статус автомобиля обязателен");
    }

    [Fact]
    public async Task UpdateAsync_WhenCarExists_UpdatesFieldsAndPhotos()
    {
        // Arrange
        await using var context = CreateContext();
        var existingCar = CreateValidCar();
        context.Cars.Add(existingCar);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedCar = CreateValidCar(
            brand: "BMW",
            model: "X5",
            year: 2022,
            price: 5_500_000,
            vin: "VIN00000000000099",
            status: "Reserved");

        updatedCar.Color = "White";
        updatedCar.Type = "С пробегом";
        updatedCar.IsActive = false;
        updatedCar.Photos = new List<CarPhoto>
        {
            new CarPhoto { Url = " ", SortOrder = 1 },
            new CarPhoto { Url = " https://example.com/second.jpg ", SortOrder = 2 },
            new CarPhoto { Url = "https://example.com/first.jpg", SortOrder = 1 }
        };

        // Act
        var result = await service.UpdateAsync(existingCar.Id, updatedCar);

        // Assert
        result.Should().BeTrue();

        var carFromDb = await context.Cars
            .Include(c => c.Photos)
            .FirstAsync(c => c.Id == existingCar.Id);

        carFromDb.Brand.Should().Be("BMW");
        carFromDb.Model.Should().Be("X5");
        carFromDb.Year.Should().Be(2022);
        carFromDb.Price.Should().Be(5_500_000);
        carFromDb.Vin.Should().Be("VIN00000000000099");
        carFromDb.Color.Should().Be("White");
        carFromDb.Type.Should().Be("С пробегом");
        carFromDb.Status.Should().Be("Reserved");
        carFromDb.IsActive.Should().BeFalse();

        carFromDb.Photos.Select(p => p.Url).Should().Equal(
            "https://example.com/first.jpg",
            "https://example.com/second.jpg");
    }

    [Fact]
    public async Task UpdateAsync_WhenCarDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var updatedCar = CreateValidCar();

        // Act
        var result = await service.UpdateAsync(999, updatedCar);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidCar_ThrowsArgumentExceptionAndDoesNotChangeExistingCar()
    {
        // Arrange
        await using var context = CreateContext();
        var existingCar = CreateValidCar();
        context.Cars.Add(existingCar);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var invalidCar = CreateValidCar(brand: " ");

        // Act
        var act = async () => await service.UpdateAsync(existingCar.Id, invalidCar);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Марка автомобиля обязательна");

        var carFromDb = await context.Cars.FirstAsync(c => c.Id == existingCar.Id);
        carFromDb.Brand.Should().Be("Toyota");
    }

    [Fact]
    public async Task DeleteAsync_WhenCarExists_RemovesCarAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var car = CreateValidCar();
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(car.Id);

        // Assert
        result.Should().BeTrue();
        (await context.Cars.FindAsync(car.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenCarDoesNotExist_ReturnsFalse()
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
    public async Task SearchAsync_WithBrandModelAndYear_ReturnsMatchingCars()
    {
        // Arrange
        await using var context = CreateContext();

        await context.Cars.AddRangeAsync(
            CreateValidCar(brand: "Toyota", model: "Camry", year: 2020, vin: "VIN00000000000001"),
            CreateValidCar(brand: "Toyota", model: "RAV4", year: 2021, vin: "VIN00000000000002"),
            CreateValidCar(brand: "BMW", model: "X5", year: 2020, vin: "VIN00000000000003")
        );

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = (await service.SearchAsync("Toyota", "Camry", 2020)).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Brand.Should().Be("Toyota");
        result[0].Model.Should().Be("Camry");
        result[0].Year.Should().Be(2020);
    }

    [Fact]
    public async Task SearchAsync_WithNullFilters_ReturnsAllCars()
    {
        // Arrange
        await using var context = CreateContext();

        await context.Cars.AddRangeAsync(
            CreateValidCar(vin: "VIN00000000000001"),
            CreateValidCar(brand: "BMW", model: "X5", year: 2022, vin: "VIN00000000000002")
        );

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SearchAsync(null, null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WhenNoCarsMatch_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        context.Cars.Add(CreateValidCar());
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SearchAsync("Mercedes", null, null);

        // Assert
        result.Should().BeEmpty();
    }
}