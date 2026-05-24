using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using backend_spo.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Tests.Controllers;

public class AuthControllerTests
{
    private static WebApplicationFactory<Program> CreateFactoryWithFakeTokenStore(
        TrackingFakeTokenStoreService tokenStore)
    {
        return new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ITokenStoreService>();
                    services.AddSingleton<ITokenStoreService>(tokenStore);
                });
            });
    }

    private static RegisterDto CreateRegisterDto(string? email = null)
    {
        return new RegisterDto
        {
            FirstName = "Алексей",
            LastName = "Скворцов",
            Email = email ?? CreateUniqueEmail(),
            Password = "Password123!"
        };
    }

    private static LoginDto CreateLoginDto(string email, string password = "Password123!")
    {
        return new LoginDto
        {
            Email = email,
            Password = password
        };
    }

    private static string CreateUniqueEmail()
    {
        return $"auth_{Guid.NewGuid():N}@test.com";
    }

    private static async Task<User> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password = "Password123!",
        string roleName = "Клиент",
        bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = await db.Roles.FirstAsync(r => r.Name == roleName);

        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = role.Id,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private static string CreateJwtToken(
        WebApplicationFactory<Program> factory,
        int userId,
        string email,
        string role = "Клиент")
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var jwtSettings = configuration.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndCreatesUser()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var dto = CreateRegisterDto();

        // Act
        var response = await client.PostAsJsonAsync("/api/Auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        user.Should().NotBeNull();
        user!.FirstName.Should().Be(dto.FirstName);
        user.LastName.Should().Be(dto.LastName);
        user.Email.Should().Be(dto.Email.ToLower());
        user.IsActive.Should().BeTrue();
        user.Role.Should().NotBeNull();
        user.Role!.Name.Should().Be("Клиент");

        var clientEntity = await db.Clients.FirstOrDefaultAsync(c => c.Email == dto.Email.ToLower());
        clientEntity.Should().NotBeNull();
        clientEntity!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var email = CreateUniqueEmail();
        await SeedUserAsync(factory, email);

        var dto = CreateRegisterDto(email);

        // Act
        var response = await client.PostAsJsonAsync("/api/Auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var dto = CreateRegisterDto("invalid-email-format");

        // Act
        var response = await client.PostAsJsonAsync("/api/Auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndJwt()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var email = CreateUniqueEmail();
        await SeedUserAsync(factory, email);

        var dto = CreateLoginDto(email);

        // Act
        var response = await client.PostAsJsonAsync("/api/Auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrWhiteSpace();
        authResponse.Email.Should().Be(email.ToLower());
        authResponse.Role.Should().Be("Клиент");

        tokenStore.ActiveTokens.Should().Contain(authResponse.Token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(authResponse.Token);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email.ToLower());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Клиент");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var email = CreateUniqueEmail();
        await SeedUserAsync(factory, email, password: "CorrectPassword123!");

        var dto = CreateLoginDto(email, password: "WrongPassword123!");

        // Act
        var response = await client.PostAsJsonAsync("/api/Auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        tokenStore.ActiveTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var dto = CreateLoginDto(CreateUniqueEmail());

        // Act
        var response = await client.PostAsJsonAsync("/api/Auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        tokenStore.ActiveTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContentAndBlacklistsToken()
    {
        // Arrange
        var tokenStore = new TrackingFakeTokenStoreService();
        await using var factory = CreateFactoryWithFakeTokenStore(tokenStore);
        var client = factory.CreateClient();

        var email = CreateUniqueEmail();
        var user = await SeedUserAsync(factory, email);

        var token = CreateJwtToken(factory, user.Id, email);
        await tokenStore.SaveTokenAsync(user.Id, token, TimeSpan.FromHours(1));

        AddBearerToken(client, token);

        // Act
        var response = await client.PostAsync("/api/Auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        tokenStore.BlacklistedTokens.Should().Contain(token);
        tokenStore.ActiveTokens.Should().NotContain(token);

        var isTokenActive = await tokenStore.IsTokenActiveAsync(token);
        isTokenActive.Should().BeFalse();
    }

    private sealed class TrackingFakeTokenStoreService : ITokenStoreService
    {
        public HashSet<string> ActiveTokens { get; } = new();
        public HashSet<string> BlacklistedTokens { get; } = new();

        public Task SaveTokenAsync(int userId, string token, TimeSpan ttl)
        {
            ActiveTokens.Add(token);
            BlacklistedTokens.Remove(token);

            return Task.CompletedTask;
        }

        public Task<bool> IsTokenActiveAsync(string token)
        {
            var isActive = ActiveTokens.Contains(token) && !BlacklistedTokens.Contains(token);

            return Task.FromResult(isActive);
        }

        public Task RemoveTokenAsync(string token)
        {
            ActiveTokens.Remove(token);
            BlacklistedTokens.Add(token);

            return Task.CompletedTask;
        }
    }
}