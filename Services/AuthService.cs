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
    private readonly IAuditService _auditService;
    private readonly ITokenStoreService _tokenStoreService;
    public AuthService( ApplicationDbContext context,
    IConfiguration configuration,
    IAuditService auditService,
    ITokenStoreService tokenStoreService)
    {
        _context = context;
        _configuration = configuration;
        _auditService = auditService;
        _tokenStoreService = tokenStoreService;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
    {
        dto.FirstName = dto.FirstName?.Trim() ?? string.Empty;
        dto.LastName = dto.LastName?.Trim() ?? string.Empty;
        dto.Email = dto.Email?.Trim().ToLower() ?? string.Empty;
        dto.Password = dto.Password?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(dto.FirstName))
            return (false, "Имя не может быть пустым");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            return (false, "Фамилия не может быть пустой");
        if (string.IsNullOrWhiteSpace(dto.Email))
            return (false, "Email не может быть пустым");
        if (string.IsNullOrWhiteSpace(dto.Password))
            return (false, "Пароль не может быть пустым");
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
            return (false, "Пользователь с таким email уже существует");
        var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Клиент");
        if (clientRole == null)
            return (false, "Роль Клиент не найдена в базе данных");
        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = clientRole.Id,
            IsActive =  true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email);
        if (existingClient == null)
        {
            var client = new Client
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = string.Empty,
                UserId = user.Id
            };
            _context.Clients.Add(client);
        }
        else if (existingClient.UserId == null)
        {
            existingClient.UserId = user.Id;
        }
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            user.Id,
            "REGISTER",
            "User",
            user.Id,
            "Success",
            $"Регистрация пользователя {user.Email}"
        );

        return (true, "Пользователь успешно зарегистрирован");
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Response)> LoginAsync(LoginDto dto)
    {
       dto.Email = dto.Email?.Trim().ToLower() ?? string.Empty;
       dto.Password = dto.Password?.Trim() ?? string.Empty;

       if (string.IsNullOrWhiteSpace(dto.Email))
           return (false, "Email не может быть пустым", null);
       if (string.IsNullOrWhiteSpace(dto.Password))
           return (false, "Пароль не может быть пустым", null);

       var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            await _auditService.LogAsync(
                null,
                "LOGIN",
                "User",
                null,
                "Failed",
                $"Неудачная попытка входа. Пользователь с email {dto.Email} не найден"
            );
            return (false, "Пользователь не найден", null);
        }
        if (!user.IsActive)
        {
            await _auditService.LogAsync(
                user.Id,
                "LOGIN",
                "User",
                user.Id,
                "Failed",
                $"Попытка входа заблокированного пользователя {user.Email}"
            );
            return (false, "Пользователь заблокирован", null);
        }
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            await _auditService.LogAsync(
                user.Id,
                "LOGIN",
                "User",
                user.Id,
                "Failed",
                $"Неверный пароль для пользователя {user.Email}"
            );
            return (false, "Неверный пароль", null);
        }
        var token = GenerateJwtToken(user);
        var expireMinutes = int.Parse(_configuration["Jwt:ExpireInMinutes"]!);
        await _tokenStoreService.SaveTokenAsync(user.Id, token, TimeSpan.FromMinutes(expireMinutes));
        var response = new AuthResponseDto 
        { 
            Token = token, 
            Email = user.Email, 
            Role = user.Role?.Name ?? string.Empty 
        }; 
        await _auditService.LogAsync(
            user.Id, 
            "LOGIN", 
            "User", 
            user.Id, 
            "Success", 
            $"Успешный вход пользователя {user.Email}" 
            );
        return (true, "Успешный вход", response);
    }

    public async Task<(bool Success, string Message)> LogoutAsync(string token, int? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Токен не найден");
        await _tokenStoreService.RemoveTokenAsync(token);

        await _auditService.LogAsync(
            currentUserId,
            "LOGOUT",
            "User",
            currentUserId,
            "Success",
            "Пользователь вышел из системы"
        );
        return (true, "Выход выполнен успешно");
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