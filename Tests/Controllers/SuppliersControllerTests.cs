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

public class SuppliersControllerTests
{
    private static async Task<int> SeedSupplierAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var supplier = new Supplier
        {
            Name = "Toyota Center",
            ContactPerson = "Иван Петров",
            Phone = "+79990000001",
            Email = $"sup{Guid.NewGuid():N}@test.com",
            Address = "Москва"
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        return supplier.Id;
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

    private static CreateSupplierDto NewSupplierDto() => new()
    {
        Name = "Honda Motors",
        ContactPerson = "Анна Иванова",
        Phone = "+79991111111",
        Email = $"honda{Guid.NewGuid():N}@test.com",
        Address = "СПб"
    };

    [Fact]
    public async Task GetSuppliers_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedSupplierAsync(factory);
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.GetAsync("api/suppliers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var suppliers = await response.Content.ReadFromJsonAsync<List<SupplierResponseDto>>();
        suppliers.Should().NotBeNull();
        suppliers!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSupplierById_NotFound_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.GetAsync("api/suppliers/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostSupplier_WithoutAuth_Returns401()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/suppliers", NewSupplierDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostSupplier_AsManager_CreatesSupplier()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.PostAsJsonAsync("api/suppliers", NewSupplierDto());

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeleteSupplier_AsAdmin_ReturnsSuccess()
    {
        await using var factory = new CustomWebApplicationFactory();
        var id = await SeedSupplierAsync(factory);
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var response = await client.DeleteAsync($"api/suppliers/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }
}
