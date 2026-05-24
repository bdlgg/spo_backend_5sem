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

public class TestDrivesControllerTests
{
    private record SeedResult(int CarId, int ClientId);

    private static async Task<SeedResult> SeedAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var supplier = await db.Suppliers.FirstOrDefaultAsync() ?? db.Suppliers.Add(new Supplier
        {
            Name = "Test", ContactPerson = "Test", Phone = "+7", Email = "s@t.ru", Address = "M"
        }).Entity;
        await db.SaveChangesAsync();

        var car = new Car
        {
            Brand = "Honda",
            Model = "Civic",
            Year = 2021,
            Price = 1_500_000,
            Vin = $"VIN{Guid.NewGuid():N}"[..17],
            Color = "Black",
            Type = "Седан",
            Status = "Available",
            IsActive = true,
            SupplierId = supplier.Id
        };
        db.Cars.Add(car);

        var client = new Client
        {
            FirstName = "Анна",
            LastName = "Иванова",
            Phone = "+79991111111",
            Email = $"c{Guid.NewGuid():N}@t.ru"
        };
        db.Clients.Add(client);

        await db.SaveChangesAsync();
        return new SeedResult(car.Id, client.Id);
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
    public async Task GetTestDrives_WithoutAuth_Returns401()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/testdrives");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTestDrives_AsClient_Returns403()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Клиент"));

        var response = await client.GetAsync("api/testdrives");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTestDrives_AsManager_Returns200()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.GetAsync("api/testdrives");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTestDriveById_NotFound_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.GetAsync("api/testdrives/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostTestDrive_AsManager_CreatesScheduled()
    {
        await using var factory = new CustomWebApplicationFactory();
        var seed = await SeedAsync(factory);
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var dto = new CreateTestDriveDto
        {
            CarId = seed.CarId,
            ClientId = seed.ClientId,
            ScheduledAt = DateTime.UtcNow.AddDays(3)
        };
        var response = await client.PostAsJsonAsync("api/testdrives", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
