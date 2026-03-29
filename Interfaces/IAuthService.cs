using backend_spo.DTOs;

namespace backend_spo.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto);
    Task<(bool Success, string Message, AuthResponseDto? Response)> LoginAsync(LoginDto dto);
    Task<(bool Success, string Message)> LogoutAsync(string token, int? currentUserId);
}