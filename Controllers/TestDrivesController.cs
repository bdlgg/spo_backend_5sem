using System.Security.Claims;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_spo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TestDrivesController : ControllerBase
    {
        private readonly ITestDriveService _testDriveService;

        public TestDrivesController(ITestDriveService testDriveService)
        {
            _testDriveService = testDriveService;
        }

        [Authorize(Roles = "Админ,Менеджер,Руководитель")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestDriveResponseDto>>> GetAll()
        {
            var testDrives = await _testDriveService.GetAllAsync();
            return Ok(testDrives.Select(MapToDto));
        }

        [Authorize(Roles = "Админ,Менеджер,Руководитель")]
        [HttpGet("{id}")]
        public async Task<ActionResult<TestDriveResponseDto>> GetById(int id)
        {
            var testDrive = await _testDriveService.GetByIdAsync(id);
            if (testDrive == null)
                return NotFound(new { message = "Тест-драйв не найден" });

            return Ok(MapToDto(testDrive));
        }

        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPost]
        public async Task<ActionResult<TestDriveResponseDto>> Create([FromBody] CreateTestDriveDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = "Не удалось определить текущего пользователя" });

            var testDrive = new TestDrive
            {
                CarId = dto.CarId,
                ClientId = dto.ClientId,
                ScheduledAt = dto.ScheduledAt
            };

            var (success, message, createdTestDrive) = await _testDriveService.CreateAsync(testDrive, currentUserId);

            if (!success || createdTestDrive == null)
                return BadRequest(new { message });

            return Ok(MapToDto(createdTestDrive));
        }

        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> Complete(int id, [FromBody] UpdateTestDriveStatusDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = "Не удалось определить текущего пользователя" });

            var (success, message) = await _testDriveService.CompleteAsync(id, dto.Result, dto.Comment, currentUserId);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [Authorize(Roles = "Админ,Менеджер")]
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] UpdateTestDriveStatusDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized(new { message = "Не удалось определить текущего пользователя" });

            var (success, message) = await _testDriveService.CancelAsync(id, dto.Comment, currentUserId);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [Authorize(Roles = "Админ,Менеджер,Руководитель")]
        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<IEnumerable<TestDriveResponseDto>>> GetByClientId(int clientId)
        {
            var testDrives = await _testDriveService.GetByClientIdAsync(clientId);
            return Ok(testDrives.Select(MapToDto));
        }

        [Authorize(Roles = "Админ,Менеджер,Руководитель")]
        [HttpGet("manager/{managerId}")]
        public async Task<ActionResult<IEnumerable<TestDriveResponseDto>>> GetByManagerId(int managerId)
        {
            var testDrives = await _testDriveService.GetByManagerIdAsync(managerId);
            return Ok(testDrives.Select(MapToDto));
        }

        private static TestDriveResponseDto MapToDto(TestDrive testDrive)
        {
            return new TestDriveResponseDto
            {
                Id = testDrive.Id,
                CarId = testDrive.CarId,
                CarTitle = testDrive.Car == null
                    ? string.Empty
                    : $"{testDrive.Car.Brand} {testDrive.Car.Model} ({testDrive.Car.Year})",
                ClientId = testDrive.ClientId,
                ClientFullName = testDrive.Client == null
                    ? string.Empty
                    : $"{testDrive.Client.FirstName} {testDrive.Client.LastName}",
                ManagerId = testDrive.ManagerId,
                ManagerName = testDrive.Manager == null
    ? string.Empty
    : $"{testDrive.Manager.FirstName} {testDrive.Manager.LastName}",
                ScheduledAt = testDrive.ScheduledAt,
                Status = testDrive.Status,
                Result = testDrive.Result,
                Comment = testDrive.Comment,
                CreatedAt = testDrive.CreatedAt
            };
        }
    }
}