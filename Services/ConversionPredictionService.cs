using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_spo.Services;

public class ConversionPredictionService : IConversionPredictionService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    public ConversionPredictionService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(bool Success, string Message, PredictConversionResponseDto? Result)> PredictAsync(int clientId,
        int carId)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
        if (client == null)
            return (false, "Клиент не найден", null);
        var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == carId);
        if (car == null)
            return (false, "Автомобиль не найден", null);
        if (!car.IsActive)
            return (false, "Автомобиль не активен",  null);
        if (car.Status == "Sold")
            return (false, "Для проданного автомобиля прогноз недоступен", null);

        float score = 0.35f;
        var explanationParts = new List<string>();
        var clientTestDrives = await _context.TestDrives
            .Where(td => td.ClientId == clientId)
            .ToListAsync();
        var testDriveForThisCar = clientTestDrives.Any(td => td.CarId == carId && td.Status == "Completed");
        var anyCompletedTestDrive = clientTestDrives.Any(td => td.Status == "Completed");
        if (testDriveForThisCar)
        {
            score += 0.30f;
            explanationParts.Add("был завершенный тест-драйв по этому автомобилю");
        }
        else if (anyCompletedTestDrive)
        {
            score += 0.15f;
            explanationParts.Add("У клиента уже был завершенный тест-драйв");
        }

        var clientSalesCount = await _context.Sales
            .CountAsync(s => s.ClientId == clientId && s.Status == "Completed");
        if (clientSalesCount > 0)
        {
            score += 0.10f;
            explanationParts.Add("У клиента уже есть завершенные сделки");
        }

        if (car.Price <= 2000000)
        {
            score += 0.10f;
            explanationParts.Add("Стоимость автомобиля находится в более доступном диапазоне");
        }
        else if (car.Price >= 5000000)
        {
            score -= 0.10f;
            explanationParts.Add("Стоимость автомобиля находится в менее доступном диапазоне");
        }

        if (car.Status == "Available")
        {
            score += 0.05f;
            explanationParts.Add("Автомобиль доступен к продаже");
        }

        score = Math.Clamp(score, 0.05f, 0.95f);
        var level = score switch
        {
            >= 0.75f => "High",
            >= 0.45f => "Medium",
            _ => "Low"
        };
        var explanation = explanationParts.Count > 0
            ? string.Join(", ", explanationParts)
            : "Прогноз построен на базовых признаках клиента и автомобиля";
        var result = new PredictConversionResponseDto
        {
            Probability = score,
            Level = level,
            Explanation = explanation
        };
        return (true, "Прогноз успешно рассчитан", result);
    }
}