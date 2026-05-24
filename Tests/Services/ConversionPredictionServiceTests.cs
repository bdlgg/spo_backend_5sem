using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace backend_spo.Tests.Services;

public class ConversionPredictionServiceTests
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

        context.Users.Add(new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Manager",
            Email = "manager@test.com",
            PasswordHash = "hash",
            RoleId = 1,
            IsActive = true
        });

        context.SaveChanges();

        return context;
    }

    private static ConversionPredictionService CreateService(ApplicationDbContext context, string contentRootPath)
    {
        return new ConversionPredictionService(
            context,
            new TestWebHostEnvironment(contentRootPath));
    }

    private static Client CreateClient()
    {
        return new Client
        {
            FirstName = "Иван",
            LastName = "Иванов",
            Phone = "+79991112233",
            Email = $"client-{Guid.NewGuid():N}@test.com"
        };
    }

    private static Car CreateCar(
        bool isActive = true,
        string status = "Available",
        string type = "Новый",
        decimal price = 1_800_000,
        string brand = "Toyota",
        string model = "Camry")
    {
        return new Car
        {
            Brand = brand,
            Model = model,
            Year = 2022,
            Price = price,
            Vin = "VIN" + Guid.NewGuid().ToString("N")[..14],
            Color = "Black",
            Type = type,
            Status = status,
            IsActive = isActive,
            SupplierId = 1
        };
    }

    private static async Task<(int ClientId, int CarId)> SeedClientAndCarAsync(
        ApplicationDbContext context,
        bool carIsActive = true,
        string carStatus = "Available",
        decimal carPrice = 1_800_000)
    {
        var client = CreateClient();
        var car = CreateCar(isActive: carIsActive, status: carStatus, price: carPrice);

        context.Clients.Add(client);
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        return (client.Id, car.Id);
    }

    private static async Task<ConversionPredictionService> CreateTrainedServiceAsync(
        ApplicationDbContext context,
        string contentRootPath)
    {
        WriteValidDataset(contentRootPath);

        var service = CreateService(context, contentRootPath);
        await service.TrainAsync();

        return service;
    }

    [Fact]
    public void HasModel_WhenModelFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        using var context = CreateContext();

        // Act
        var service = CreateService(context, contentRoot.DirectoryPath);

        // Assert
        service.HasModel.Should().BeFalse();
    }

    [Fact]
    public async Task HasModel_WhenModelWasTrained_ReturnsTrue()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        WriteValidDataset(contentRoot.DirectoryPath);

        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);

        // Act
        await service.TrainAsync();

        // Assert
        service.HasModel.Should().BeTrue();
        File.Exists(Path.Combine(contentRoot.DirectoryPath, "MLModels", "conversion-model.zip"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task TrainAsync_WhenDatasetDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);

        // Act
        var act = async () => await service.TrainAsync();

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Не найден датасет:*car_leads_dataset.csv*");
    }

    [Fact]
    public async Task TrainAsync_WhenDatasetIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        WriteEmptyDataset(contentRoot.DirectoryPath);

        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);

        // Act
        var act = async () => await service.TrainAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Датасет пустой.");
    }

    [Fact]
    public async Task TrainAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        WriteValidDataset(contentRoot.DirectoryPath);

        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await service.TrainAsync(cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TrainAsync_WithValidDataset_ReturnsMetricsAndCreatesModelFile()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        WriteValidDataset(contentRoot.DirectoryPath, rowsCount: 40);

        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);

        // Act
        var result = await service.TrainAsync();

        // Assert
        result.DataRows.Should().Be(40);
        result.ModelFile.Should().Be("MLModels/conversion-model.zip");
        result.Accuracy.Should().BeInRange(0, 1);
        result.Auc.Should().BeInRange(0, 1);
        result.F1Score.Should().BeInRange(0, 1);

        File.Exists(Path.Combine(contentRoot.DirectoryPath, "MLModels", "conversion-model.zip"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task PredictAsync_WhenModelCannotBeLoaded_ReturnsErrorAndNullResult()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);

        // Act
        var result = await service.PredictAsync(clientId: 1, carId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().StartWith("Не найден датасет:");
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task PredictAsync_WhenClientDoesNotExist_ReturnsErrorAndNullResult()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = await CreateTrainedServiceAsync(context, contentRoot.DirectoryPath);

        var car = CreateCar();
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PredictAsync(clientId: 999, carId: car.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Клиент не найден");
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task PredictAsync_WhenCarDoesNotExist_ReturnsErrorAndNullResult()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = await CreateTrainedServiceAsync(context, contentRoot.DirectoryPath);

        var client = CreateClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        // Act
        var result = await service.PredictAsync(clientId: client.Id, carId: 999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль не найден");
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task PredictAsync_WhenCarIsInactive_ReturnsErrorAndNullResult()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = await CreateTrainedServiceAsync(context, contentRoot.DirectoryPath);

        var (clientId, carId) = await SeedClientAndCarAsync(context, carIsActive: false);

        // Act
        var result = await service.PredictAsync(clientId, carId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Автомобиль не активен");
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task PredictAsync_WhenCarIsSold_ReturnsErrorAndNullResult()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = await CreateTrainedServiceAsync(context, contentRoot.DirectoryPath);

        var (clientId, carId) = await SeedClientAndCarAsync(context, carStatus: "Sold");

        // Act
        var result = await service.PredictAsync(clientId, carId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Для проданного автомобиля прогноз недоступен");
        result.Result.Should().BeNull();
    }

    [Fact]
    public async Task PredictAsync_WithValidClientAndCar_AutoTrainsAndReturnsPrediction()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        WriteValidDataset(contentRoot.DirectoryPath);

        await using var context = CreateContext();
        var service = CreateService(context, contentRoot.DirectoryPath);
        var (clientId, carId) = await SeedClientAndCarAsync(context);

        // Act
        var result = await service.PredictAsync(clientId, carId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Прогноз успешно рассчитан");
        result.Result.Should().NotBeNull();
        result.Result!.Probability.Should().BeInRange(0.05f, 0.95f);
        result.Result.Level.Should().BeOneOf("Low", "Medium", "High");
        result.Result.Explanation.Should().NotBeNullOrWhiteSpace();
        service.HasModel.Should().BeTrue();
    }

    [Fact]
    public async Task PredictAsync_WithCompletedTestDriveAndSale_ReturnsExplanationWithInteractionDetails()
    {
        // Arrange
        using var contentRoot = new TempContentRoot();
        await using var context = CreateContext();
        var service = await CreateTrainedServiceAsync(context, contentRoot.DirectoryPath);

        var (clientId, carId) = await SeedClientAndCarAsync(context, carPrice: 1_500_000);

        context.TestDrives.Add(new TestDrive
        {
            ClientId = clientId,
            CarId = carId,
            ManagerId = 1,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Status = "Completed",
            Result = "Interested",
            Comment = "Клиент заинтересован",
            CreatedAt = new DateTime(2026, 5, 10)
        });

        context.Sales.Add(new Sale
        {
            ClientId = clientId,
            CarId = carId,
            UserId = 1,
            FinalPrice = 1_450_000,
            Status = "Completed",
            SaleDate = DateTime.UtcNow.AddDays(-10)
        });

        await context.SaveChangesAsync();

        // Act
        var result = await service.PredictAsync(clientId, carId);

        // Assert
        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.Explanation.Should().Contain("у клиента был завершенный тест-драйв");
        result.Result.Explanation.Should().Contain("клиент уже взаимодействовал с салоном");
        result.Result.Explanation.Should().Contain("у клиента уже есть история завершенных сделок");
    }

    private static void WriteEmptyDataset(string contentRootPath)
    {
        var dataDirectory = Path.Combine(contentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        File.WriteAllText(
            Path.Combine(dataDirectory, "car_leads_dataset.csv"),
            CsvHeader + Environment.NewLine);
    }

    private static void WriteValidDataset(string contentRootPath, int rowsCount = 30)
    {
        var dataDirectory = Path.Combine(contentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        var lines = new List<string> { CsvHeader };

        for (var i = 1; i <= rowsCount; i++)
        {
            var isConverted = i % 2 == 0;
            var month = i % 12 + 1;

            lines.Add(string.Join(',',
                i.ToString(),
                new DateTime(2026, month, 10).ToString("yyyy-MM-dd"),
                isConverted ? "website" : "cold_call",
                isConverted ? "Toyota" : "BMW",
                isConverted ? "Camry" : "X5",
                isConverted ? "sedan" : "suv",
                isConverted ? "1800000" : "6500000",
                isConverted ? "1" : "0",
                isConverted ? "Moscow" : "Kazan",
                isConverted ? "4" : "1",
                isConverted ? "1" : "0",
                isConverted ? "1" : "0",
                isConverted ? "0" : "1",
                isConverted ? "Hot" : "Cold",
                isConverted ? "3" : "18",
                "manager-1",
                isConverted ? "30-40" : "18-25",
                isConverted ? "automatic" : "manual",
                isConverted ? "petrol" : "diesel",
                isConverted ? "unknown" : "50000-100000",
                isConverted ? "1" : "0",
                isConverted ? "3" : "1",
                isConverted ? "50000" : "0",
                isConverted ? "2000000" : "2500000",
                isConverted ? "high" : "low",
                isConverted ? "1" : "0"));
        }

        File.WriteAllLines(Path.Combine(dataDirectory, "car_leads_dataset.csv"), lines);
    }

    private const string CsvHeader =
        "LeadId,CreatedAt,SourceChannel,CarBrand,CarModel,CarCategory,PriceAtLead,IsNewCar,ClientCity,ContactCount,HadTestDrive,InterestedInCredit,InterestedInTradeIn,LeadStatusBeforeFinal,DaysToDecision,ManagerId,ClientAgeGroup,PreferredTransmission,PreferredFuelType,MileageRange,VisitToSalon,NumberOfViewedCars,DiscountOffered,InitialBudget,MonthlyIncomeRange,IsConverted";

    private sealed class TempContentRoot : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(
            Path.GetTempPath(),
            "conversion-prediction-service-tests",
            Guid.NewGuid().ToString());

        public TempContentRoot()
        {
            Directory.CreateDirectory(DirectoryPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, recursive: true);
            }
            catch
            {
                // Если ОС еще держит файл модели, очистка временной папки не должна валить тест.
            }
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
        }

        public string ApplicationName { get; set; } = "backend_spo.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; }
    }
}