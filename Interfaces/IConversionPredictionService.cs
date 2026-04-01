using backend_spo.DTOs;

namespace backend_spo.Interfaces;

public interface IConversionPredictionService
{
    bool HasModel { get; }

    Task<TrainConversionModelResponseDto> TrainAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, PredictConversionResponseDto? Result)> PredictAsync(int clientId, int carId);
}