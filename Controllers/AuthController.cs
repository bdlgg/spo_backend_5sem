using System.Security.Claims;
using backend_spo.DTOs;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
        {
            if (result.Message == "Пользователь не найден")
                return NotFound(new { message = result.Message });

            if (result.Message == "Неверный пароль")
                return Unauthorized(new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        return Ok(result.Response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            return BadRequest(new { message = "Токен не найден в заголовке Authorization" });

        var token = authHeader["Bearer ".Length..].Trim();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = null;

        if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
            currentUserId = parsedUserId;

        var result = await _authService.LogoutAsync(token, currentUserId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return NoContent();
    }
}