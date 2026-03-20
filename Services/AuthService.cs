using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend_spo.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
            return (false, "Пользователь с таким email уже существует");
        var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Клиент");
        if (clientRole == null)
            return (false, "Роль Клиент не найдена в базе данных");

        var user = new User
        {
            FirstName =  dto.FirstName,
            LastName =  dto.LastName,
            Email =   dto.Email,
            PasswordHash =  BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId =  clientRole.Id,
            IsActive = true,
            CreatedAt =  DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return (true, "Пользователь успешно зарегистрирован");
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Response)> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return (false, "Пользователь не найден", null);
        if (!user.IsActive)
            return (false, "Пользователь заблокирован", null);
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
            return (false, "Неверный пароль", null);
        var token = GenerateJwtToken(user);
        var response = new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty
        };
        return (true, "Успешный вход", response);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        );
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty)
        };
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(jwtSettings["ExpireInMinutes"]!)
            ),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}