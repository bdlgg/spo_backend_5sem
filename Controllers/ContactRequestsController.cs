using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/contact-requests")]
public class ContactRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContactRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ContactRequestResponseDto>> Create(
        [FromBody] CreateContactRequestDto dto)
    {
        var fullName = (dto.FullName ?? string.Empty).Trim();
        var phone = (dto.Phone ?? string.Empty).Trim();
        var subject = (dto.Subject ?? string.Empty).Trim();
        var message = (dto.Message ?? string.Empty).Trim();

        if (fullName.Length < 3)
            return BadRequest(new { message = "Укажите ФИО" });
        if (phone.Length < 7)
            return BadRequest(new { message = "Укажите корректный телефон" });
        if (subject.Length == 0)
            return BadRequest(new { message = "Выберите тему" });
        if (message.Length < 3)
            return BadRequest(new { message = "Опишите запрос" });

        var entity = new ContactRequest
        {
            FullName = fullName,
            Phone = phone,
            Subject = subject,
            Message = message,
            Status = "New",
            CreatedAt = DateTime.UtcNow,
        };

        _context.ContactRequests.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(MapToDto(entity));
    }

    [HttpGet]
    [Authorize(Roles = "Менеджер,Админ,Руководитель")]
    public async Task<ActionResult<IEnumerable<ContactRequestResponseDto>>> List()
    {
        var items = await _context.ContactRequests
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .ToListAsync();
        return Ok(items.Select(MapToDto));
    }

    private static readonly HashSet<string> AllowedStatuses = new()
    {
        "New", "InProgress", "Done", "Cancelled",
    };

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Менеджер,Админ,Руководитель")]
    public async Task<ActionResult<ContactRequestResponseDto>> UpdateStatus(
        int id,
        [FromBody] UpdateContactRequestStatusDto dto)
    {
        var status = (dto.Status ?? string.Empty).Trim();
        if (!AllowedStatuses.Contains(status))
            return BadRequest(new { message = "Недопустимый статус" });

        var entity = await _context.ContactRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null)
            return NotFound(new { message = "Обращение не найдено" });

        entity.Status = status;
        await _context.SaveChangesAsync();
        return Ok(MapToDto(entity));
    }

    private static ContactRequestResponseDto MapToDto(ContactRequest r) => new()
    {
        Id = r.Id,
        FullName = r.FullName,
        Phone = r.Phone,
        Subject = r.Subject,
        Message = r.Message,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
    };
}
