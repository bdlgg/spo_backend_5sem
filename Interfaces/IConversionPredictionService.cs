using backend_spo.DTOs;

namespace backend_spo.Interfaces;

public interface IConversionPredictionService
{
    Task<(bool Success, string Message, PredictConversionResponseDto? Result)> PredictAsync(int clientId, int carId);
}