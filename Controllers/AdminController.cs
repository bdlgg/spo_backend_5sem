using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend_spo.DTOs;
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

    [HttpPatch("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
    {
        var result = await _userService.UpdateUserRoleAsync(id, dto.RoleName);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new  { message = result.Message });
    }
}