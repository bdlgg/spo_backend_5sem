using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_spo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Админ")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogResponseDto>>> GetAll()
        {
            var logs = await _auditService.GetAllAsync();
            return Ok(logs.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogResponseDto>> GetById(int id)
        {
            var log = await _auditService.GetByIdAsync(id);
            if (log == null)
                return NotFound(new { message = "Запись аудита не найдена" });

            return Ok(MapToDto(log));
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<AuditLogResponseDto>>> GetByUserId(int userId)
        {
            var logs = await _auditService.GetByUserIdAsync(userId);
            return Ok(logs.Select(MapToDto));
        }

        private static AuditLogResponseDto MapToDto(AuditLog log)
        {
            return new AuditLogResponseDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = log.User?.Email ?? string.Empty,
                CreatedAt = log.CreatedAt,
                ActionType = log.ActionType,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Result = log.Result,
                Details = log.Details
            };
        }
    }
}