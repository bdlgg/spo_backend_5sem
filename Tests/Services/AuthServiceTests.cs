using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace backend_spo.Tests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(
        ApplicationDbContext context,
        SpyTokenStoreService? tokenStore = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-super-secret-test-key-123456",
                ["Jwt:Issuer"] = "backend_spo",
                ["Jwt:Audience"] = "backend_spo_users",
                ["Jwt:ExpireInMinutes"] = "120"
            })
            .Build();

        return new AuthService(
            context,
            configuration,
            new FakeAuditService(),
            tokenStore ?? new SpyTokenStoreService()
        );
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        context.Roles.AddRange(
            new Role { Id = 1, Name = "Админ" },
            new Role { Id = 2, Name = "Клиент" }
        );

        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUser()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var dto = new RegisterDto
        {
            FirstName = "Иван",
            LastName = "Иванов",
            Email = " Ivan@Test.Com ",
            Password = "123456"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Пользователь успешно зарегистрирован");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "ivan@test.com");

        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Иван");
        user.LastName.Should().Be("Иванов");
        user.IsActive.Should().BeTrue();
        user.RoleId.Should().Be(2);
        BCrypt.Net.BCrypt.Verify("123456", user.PasswordHash).Should().BeTrue();

        var client = await context.Clients.FirstOrDefaultAsync(c => c.Email == "ivan@test.com");

        client.Should().NotBeNull();
        client!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsError()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            FirstName = "Иван",
            LastName = "Иванов",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RoleId = 2,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var dto = new RegisterDto
        {
            FirstName = "Петр",
            LastName = "Петров",
            Email = " TEST@Test.Com ",
            Password = "123456"
        };

        var result = await service.RegisterAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Пользователь с таким email уже существует");

        var usersCount = await context.Users.CountAsync(u => u.Email == "test@test.com");
        usersCount.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsJwt()
    {
        await using var context = CreateContext();
        var tokenStore = new SpyTokenStoreService();

        var role = await context.Roles.FirstAsync(r => r.Name == "Клиент");

        context.Users.Add(new User
        {
            FirstName = "Иван",
            LastName = "Иванов",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RoleId = role.Id,
            Role = role,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = CreateService(context, tokenStore);

        var dto = new LoginDto
        {
            Email = " TEST@Test.Com ",
            Password = "123456"
        };

        var result = await service.LoginAsync(dto);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Успешный вход");
        result.Response.Should().NotBeNull();
        result.Response!.Token.Should().NotBeNullOrWhiteSpace();
        result.Response.Email.Should().Be("test@test.com");
        result.Response.Role.Should().Be("Клиент");

        tokenStore.SavedToken.Should().NotBeNullOrWhiteSpace();
        tokenStore.SavedUserId.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsError()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            FirstName = "Иван",
            LastName = "Иванов",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            RoleId = 2,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var dto = new LoginDto
        {
            Email = "test@test.com",
            Password = "wrong-password"
        };

        var result = await service.LoginAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Неверный пароль");
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithNotExistingUser_ReturnsError()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var dto = new LoginDto
        {
            Email = "missing@test.com",
            Password = "123456"
        };

        var result = await service.LoginAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Пользователь не найден");
        result.Response.Should().BeNull();
    }

    private class FakeAuditService : IAuditService
    {
        public Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<AuditLog>>(new List<AuditLog>());
        }

        public Task<AuditLog?> GetByIdAsync(int id)
        {
            return Task.FromResult<AuditLog?>(null);
        }

        public Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
        {
            return Task.FromResult<IEnumerable<AuditLog>>(new List<AuditLog>());
        }

        public Task LogAsync(
            int? userId,
            string actionType,
            string entityName,
            int? entityId,
            string result,
            string details)
        {
            return Task.CompletedTask;
        }
    }

    private class SpyTokenStoreService : ITokenStoreService
    {
        public int? SavedUserId { get; private set; }
        public string? SavedToken { get; private set; }

        public Task SaveTokenAsync(int userId, string token, TimeSpan ttl)
        {
            SavedUserId = userId;
            SavedToken = token;
            return Task.CompletedTask;
        }

        public Task<bool> IsTokenActiveAsync(string token)
        {
            return Task.FromResult(true);
        }

        public Task RemoveTokenAsync(string token)
        {
            return Task.CompletedTask;
        }
    }
}