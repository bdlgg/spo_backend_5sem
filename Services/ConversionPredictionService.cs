using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using backend_spo.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace backend_spo.Services;

public class ConversionPredictionService : IConversionPredictionService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly MLContext _mlContext;

    private ITransformer? _model;

    private static readonly string[] CategoricalColumns =
    [
        nameof(ConversionModelInput.SourceChannel),
        nameof(ConversionModelInput.CarBrand),
        nameof(ConversionModelInput.CarModel),
        nameof(ConversionModelInput.CarCategory),
        nameof(ConversionModelInput.ClientCity),
        nameof(ConversionModelInput.ClientAgeGroup),
        nameof(ConversionModelInput.PreferredTransmission),
        nameof(ConversionModelInput.PreferredFuelType),
        nameof(ConversionModelInput.MileageRange),
        nameof(ConversionModelInput.MonthlyIncomeRange)
    ];

    private static readonly string[] NumericColumns =
    [
        nameof(ConversionModelInput.PriceAtLead),
        nameof(ConversionModelInput.IsNewCar),
        nameof(ConversionModelInput.ContactCount),
        nameof(ConversionModelInput.HadTestDrive),
        nameof(ConversionModelInput.InterestedInCredit),
        nameof(ConversionModelInput.InterestedInTradeIn),
        nameof(ConversionModelInput.VisitToSalon),
        nameof(ConversionModelInput.NumberOfViewedCars),
        nameof(ConversionModelInput.DiscountOffered),
        nameof(ConversionModelInput.InitialBudget),
        nameof(ConversionModelInput.LeadMonth)
    ];

    private string DatasetPath => Path.Combine(_environment.ContentRootPath, "Data", "car_leads_dataset.csv");
    private string ModelDirectory => Path.Combine(_environment.ContentRootPath, "MLModels");
    private string ModelPath => Path.Combine(ModelDirectory, "conversion-model.zip");

    public bool HasModel => _model is not null || File.Exists(ModelPath);

    public ConversionPredictionService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
        _mlContext = new MLContext(seed: 42);

        TryLoadExistingModel();
    }

    public async Task<TrainConversionModelResponseDto> TrainAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(DatasetPath))
                throw new FileNotFoundException($"Не найден датасет: {DatasetPath}");

            var rawData = _mlContext.Data.LoadFromTextFile<CarLeadCsvRow>(
                DatasetPath,
                hasHeader: true,
                separatorChar: ',');

            var rows = _mlContext.Data
                .CreateEnumerable<CarLeadCsvRow>(rawData, reuseRowObject: false)
                .Select(MapCsvRowToModelInput)
                .ToList();

            if (rows.Count == 0)
                throw new InvalidOperationException("Датасет пустой.");

            var data = _mlContext.Data.LoadFromEnumerable(rows);
            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 42);

            var encodedColumns = CategoricalColumns
                .Select(x => new InputOutputColumnPair($"{x}Encoded", x))
                .ToArray();

            var featureColumns = NumericColumns
                .Concat(encodedColumns.Select(x => x.OutputColumnName))
                .ToArray();

            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(encodedColumns)
                .Append(_mlContext.Transforms.Concatenate("Features", featureColumns))
                .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: nameof(ConversionModelInput.Label),
                    featureColumnName: "Features"));

            var model = pipeline.Fit(split.TrainSet);

            var predictions = model.Transform(split.TestSet);
            var metrics = _mlContext.BinaryClassification.Evaluate(
                predictions,
                labelColumnName: nameof(ConversionModelInput.Label));

            Directory.CreateDirectory(ModelDirectory);
            _mlContext.Model.Save(model, split.TrainSet.Schema, ModelPath);

            _model = model;

            return new TrainConversionModelResponseDto
            {
                DataRows = rows.Count,
                Accuracy = metrics.Accuracy,
                Auc = metrics.AreaUnderRocCurve,
                F1Score = metrics.F1Score,
                PositivePrecision = metrics.PositivePrecision,
                PositiveRecall = metrics.PositiveRecall,
                ModelFile = "MLModels/conversion-model.zip"
            };
        }, cancellationToken);
    }

    public async Task<(bool Success, string Message, PredictConversionResponseDto? Result)> PredictAsync(int clientId, int carId)
    {
        try
        {
            EnsureModelLoaded();
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
        if (client == null)
            return (false, "Клиент не найден", null);

        var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == carId);
        if (car == null)
            return (false, "Автомобиль не найден", null);

        if (!car.IsActive)
            return (false, "Автомобиль не активен", null);

        if (car.Status == "Sold")
            return (false, "Для проданного автомобиля прогноз недоступен", null);

        var clientTestDrives = await _context.TestDrives
            .Where(td => td.ClientId == clientId)
            .ToListAsync();

        var clientSalesCount = await _context.Sales
            .CountAsync(s => s.ClientId == clientId && s.Status == "Completed");

        var input = BuildInputFromDb(client, car, clientTestDrives, clientSalesCount);

        var engine = _mlContext.Model.CreatePredictionEngine<ConversionModelInput, ConversionPredictionOutput>(_model!);
        var prediction = engine.Predict(input);

        var probability = Math.Clamp(prediction.Probability, 0.05f, 0.95f);

        var level = probability switch
        {
            >= 0.75f => "High",
            >= 0.45f => "Medium",
            _ => "Low"
        };

        var explanation = BuildExplanation(probability, input, clientSalesCount);

        var result = new PredictConversionResponseDto
        {
            Probability = probability,
            Level = level,
            Explanation = explanation
        };

        return (true, "Прогноз успешно рассчитан", result);
    }

    private void TryLoadExistingModel()
    {
        if (!File.Exists(ModelPath))
            return;

        _model = _mlContext.Model.Load(ModelPath, out _);
    }

    private void EnsureModelLoaded()
    {
        if (_model is not null)
            return;

        if (!File.Exists(ModelPath))
            throw new InvalidOperationException("Модель еще не обучена. Сначала вызови POST /api/Prediction/train-conversion-model");

        _model = _mlContext.Model.Load(ModelPath, out _);
    }

    private static ConversionModelInput MapCsvRowToModelInput(CarLeadCsvRow row)
    {
        var leadMonth = 1f;
        if (DateTime.TryParse(row.CreatedAt, out var createdAt))
            leadMonth = createdAt.Month;

        return new ConversionModelInput
        {
            SourceChannel = Normalize(row.SourceChannel),
            CarBrand = Normalize(row.CarBrand),
            CarModel = Normalize(row.CarModel),
            CarCategory = Normalize(row.CarCategory),
            PriceAtLead = row.PriceAtLead,
            IsNewCar = row.IsNewCar,
            ClientCity = Normalize(row.ClientCity),
            ContactCount = row.ContactCount,
            HadTestDrive = row.HadTestDrive,
            InterestedInCredit = row.InterestedInCredit,
            InterestedInTradeIn = row.InterestedInTradeIn,
            ClientAgeGroup = Normalize(row.ClientAgeGroup),
            PreferredTransmission = Normalize(row.PreferredTransmission),
            PreferredFuelType = Normalize(row.PreferredFuelType),
            MileageRange = Normalize(string.IsNullOrWhiteSpace(row.MileageRange) ? "unknown" : row.MileageRange),
            VisitToSalon = row.VisitToSalon,
            NumberOfViewedCars = row.NumberOfViewedCars,
            DiscountOffered = row.DiscountOffered,
            InitialBudget = row.InitialBudget,
            MonthlyIncomeRange = Normalize(row.MonthlyIncomeRange),
            LeadMonth = leadMonth,
            Label = row.IsConverted >= 0.5f
        };
    }

    private static ConversionModelInput BuildInputFromDb(
        Client client,
        Car car,
        List<TestDrive> testDrives,
        int completedSalesCount)
    {
        var testDrivesForThisCar = testDrives.Where(x => x.CarId == car.Id).ToList();
        var anyCompletedTestDrive = testDrives.Any(x => x.Status == "Completed");
        var firstInteractionDate = testDrives
            .OrderBy(x => x.CreatedAt)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefault() ?? DateTime.UtcNow;

        var isNewCar = car.Type.Contains("нов", StringComparison.OrdinalIgnoreCase) ? 1f : 0f;
        var viewedCarsCount = testDrives.Select(x => x.CarId).Distinct().Count();

        return new ConversionModelInput
        {
            SourceChannel = "unknown",
            CarBrand = Normalize(car.Brand),
            CarModel = Normalize(car.Model),
            CarCategory = "unknown",
            PriceAtLead = (float)car.Price,
            IsNewCar = isNewCar,
            ClientCity = "unknown",
            ContactCount = Math.Max(testDrives.Count, 1),
            HadTestDrive = anyCompletedTestDrive ? 1f : 0f,
            InterestedInCredit = 0f,
            InterestedInTradeIn = 0f,
            ClientAgeGroup = "unknown",
            PreferredTransmission = "unknown",
            PreferredFuelType = "unknown",
            MileageRange = isNewCar > 0.5f ? "unknown" : "0-50000",
            VisitToSalon = testDrivesForThisCar.Count > 0 ? 1f : 0f,
            NumberOfViewedCars = Math.Max(viewedCarsCount, 1),
            DiscountOffered = 0f,
            InitialBudget = (float)car.Price,
            MonthlyIncomeRange = "unknown",
            LeadMonth = firstInteractionDate.Month,
            Label = false
        };
    }

    private static string BuildExplanation(float probability, ConversionModelInput input, int completedSalesCount)
    {
        var parts = new List<string>();

        if (input.HadTestDrive > 0.5f)
            parts.Add("у клиента был завершенный тест-драйв");

        if (input.VisitToSalon > 0.5f)
            parts.Add("клиент уже взаимодействовал с салоном");

        if (completedSalesCount > 0)
            parts.Add("у клиента уже есть история завершенных сделок");

        if (input.PriceAtLead <= 2_000_000)
            parts.Add("стоимость автомобиля находится в более доступном диапазоне");
        else if (input.PriceAtLead >= 5_000_000)
            parts.Add("стоимость автомобиля находится в более высоком ценовом диапазоне");

        if (probability >= 0.75f)
            parts.Add("лид стоит приоритизировать");
        else if (probability < 0.45f)
            parts.Add("нужна дополнительная работа с клиентом");

        return parts.Count > 0
            ? string.Join(", ", parts)
            : "Прогноз сформирован по доступным данным клиента, автомобиля и истории взаимодействия";
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}