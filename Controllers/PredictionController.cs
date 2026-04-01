using System.Security.Claims;
using backend_spo.DTOs;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Админ,Руководитель,Менеджер")]
public class PredictionController : ControllerBase
{
    private readonly IConversionPredictionService _predictionService;
    private readonly IAuditService _auditService;

    public PredictionController(IConversionPredictionService predictionService, IAuditService auditService)
    {
        _predictionService = predictionService;
        _auditService = auditService;
    }

    [HttpGet("model-status")]
    public IActionResult ModelStatus()
    {
        return Ok(new
        {
            isModelTrained = _predictionService.HasModel
        });
    }

    [HttpPost("train-conversion-model")]
    public async Task<ActionResult<TrainConversionModelResponseDto>> TrainConversionModel(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = null;
        if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
            currentUserId = parsedUserId;

        try
        {
            var result = await _predictionService.TrainAsync(cancellationToken);

            await _auditService.LogAsync(
                currentUserId,
                "TRAIN_CONVERSION_MODEL",
                "Prediction",
                null,
                "Success",
                $"Обучена модель прогнозирования конверсии. Строк датасета: {result.DataRows}, Accuracy={result.Accuracy:F4}, AUC={result.Auc:F4}"
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                currentUserId,
                "TRAIN_CONVERSION_MODEL",
                "Prediction",
                null,
                "Failed",
                $"Ошибка обучения модели прогнозирования конверсии: {ex.Message}"
            );

            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("predict-conversion")]
    public async Task<ActionResult<PredictConversionResponseDto>> PredictConversion([FromBody] PredictConversionRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = null;
        if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
            currentUserId = parsedUserId;

        var (success, message, result) = await _predictionService.PredictAsync(dto.ClientId, dto.CarId);

        await _auditService.LogAsync(
            currentUserId,
            "PREDICT_CONVERSION",
            "Prediction",
            null,
            success ? "Success" : "Failed",
            success
                ? $"Рассчитан прогноз конверсии для клиента Id={dto.ClientId}, автомобиля Id={dto.CarId}"
                : $"Ошибка прогноза конверсии для клиента Id={dto.ClientId}, автомобиля Id={dto.CarId}: {message}"
        );

        if (!success || result == null)
            return BadRequest(new { message });

        return Ok(result);
    }
}