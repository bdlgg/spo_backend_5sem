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

    [HttpPost("predict-conversion")]
    public async Task<ActionResult<PredictConversionResponseDto>> PredictConversion(
        [FromBody] PredictConversionRequestDto dto)
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