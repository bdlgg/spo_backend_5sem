using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Админ")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    public  AdminController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllAsync();
        var result = users.Select(MapToDto);
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if  (user == null) return NotFound(new { message = "Пользователь не найден" });
        return Ok(MapToDto(user));
    }

    [HttpPatch("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
    {
        var result = await _userService.UpdateUserRoleAsync(id, dto.RoleName);
        if (!result.Success)
        {
            if (result.Message == "Пользователь не найден")
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        return Ok(new  { message = result.Message });
    }

    [HttpPatch("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto dto)
    {
        var result = await _userService.UpdateUserStatusAsync(id, dto.IsActive);
        if (!result.Success)
        {
            if (result.Message == "Пользователь не найден")
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }
        return Ok(new { message = result.Message });
    }
    
    private static UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty
        };
    }
}