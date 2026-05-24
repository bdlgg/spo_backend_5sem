using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Tests.Controllers;

public class AdminControllerTests
{
    private static async Task<int> SeedUserAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Клиент")
            ?? db.Roles.Add(new Role { Name = "Клиент" }).Entity;
        await db.SaveChangesAsync();

        var user = new User
        {
            FirstName = "Тест",
            LastName = "Юзер",
            Email = $"u{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private static string CreateJwtToken(CustomWebApplicationFactory factory, string role)
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Email, "test@test.com"),
            new(ClaimTypes.Role, role)
        };
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_AsManager_Returns403()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_Returns200()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var response = await client.GetAsync("api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_NotFound_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var response = await client.GetAsync("api/admin/users/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchUserStatus_AsAdmin_BlocksUser()
    {
        await using var factory = new CustomWebApplicationFactory();
        var userId = await SeedUserAsync(factory);
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var dto = new UpdateUserStatusDto { IsActive = false };
        var response = await client.PatchAsJsonAsync($"api/admin/users/{userId}/status", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
