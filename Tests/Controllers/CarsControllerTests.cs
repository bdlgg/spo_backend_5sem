using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Security.Claims;
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

public class CarsControllerTests
{
    private static async Task<int> SeedCarAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.CarPhotos.RemoveRange(db.CarPhotos);
        db.Sales.RemoveRange(db.Sales);
        db.TestDrives.RemoveRange(db.TestDrives);
        db.ServiceOrders.RemoveRange(db.ServiceOrders);
        db.Cars.RemoveRange(db.Cars);

        await db.SaveChangesAsync();

        var supplier = await db.Suppliers.FirstOrDefaultAsync();

        if (supplier == null)
        {
            supplier = new Supplier
            {
                Name = "Test Supplier",
                ContactPerson = "Test Contact",
                Phone = "+79990000000",
                Email = "supplier@test.com",
                Address = "Test Address"
            };

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
        }

        var car = new Car
        {
            Brand = "Toyota",
            Model = "Camry",
            Year = 2020,
            Price = 2500000,
            Vin = $"VIN{Guid.NewGuid():N}"[..17],
            Color = "Black",
            Type = "Новый",
            Status = "Available",
            SupplierId = supplier.Id,
            IsActive = true,
            Photos = new List<CarPhoto>
            {
                new CarPhoto
                {
                    Url = "https://example.com/car.jpg",
                    SortOrder = 1
                }
            }
        };

        db.Cars.Add(car);
        await db.SaveChangesAsync();

        return car.Id;
    }

    private static string CreateJwtToken(CustomWebApplicationFactory factory, string role)
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@test.com"),
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

    private static CreateCarDto CreateCarDto()
    {
        return new CreateCarDto
        {
            Brand = "BMW",
            Model = "X5",
            Year = 2022,
            Price = 5500000,
            Vin = "VIN00000000000002",
            Color = "White",
            Type = "Новый",
            Status = "Available",
            SupplierId = 1,
            PhotoUrls = new List<string>
            {
                "https://example.com/bmw.jpg"
            }
        };
    }

    [Fact]
    public async Task GetCars_ReturnsOkAndList()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedCarAsync(factory);
        
        var client = factory.CreateClient();
        var response = await client.GetAsync("api/cars");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cars = await response.Content.ReadFromJsonAsync<List<CarResponseDto>>();
        cars.Should().NotBeNull();
        cars!.Should().NotBeEmpty();
        cars.Should().Contain(c => c.Brand == "Toyota" && c.Model == "Camry");
    }

    [Fact]
    public async Task GetCarById_ExistingCar_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var carId = await SeedCarAsync(factory);

        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/cars/{carId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var car = await response.Content.ReadFromJsonAsync<CarResponseDto>();
        car.Should().NotBeNull();
        car!.Id.Should().Be(carId);
        car.Brand.Should().Be("Toyota");
    }
    
    [Fact]
    public async Task GetCarById_NotExistingCar_ReturnsNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedCarAsync(factory);

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cars/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task PostCar_WithoutAuthorization_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedCarAsync(factory);

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cars", CreateCarDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task PostCar_WithManagerRole_ReturnsCreatedAndCreatesCar()
    {
        await using var factory = new CustomWebApplicationFactory();
        await SeedCarAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.PostAsJsonAsync("/api/cars", CreateCarDto());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdCar = await response.Content.ReadFromJsonAsync<CarResponseDto>();
        createdCar.Should().NotBeNull();
        createdCar!.Brand.Should().Be("BMW");
        createdCar.Model.Should().Be("X5");

        var getResponse = await client.GetAsync($"/api/cars/{createdCar.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task PutCar_WithManagerRole_ReturnsNoContentAndUpdatesCar()
    {
        await using var factory = new CustomWebApplicationFactory();
        var carId = await SeedCarAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory, "Менеджер"));

        var dto = CreateCarDto();
        dto.Brand = "Audi";
        dto.Model = "A6";
        dto.Vin = "VIN00000000000003";

        var response = await client.PutAsJsonAsync($"/api/cars/{carId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/cars/{carId}");
        var updatedCar = await getResponse.Content.ReadFromJsonAsync<CarResponseDto>();

        updatedCar.Should().NotBeNull();
        updatedCar!.Brand.Should().Be("Audi");
        updatedCar.Model.Should().Be("A6");
    }
    
    [Fact]
    public async Task DeleteCar_WithAdminRole_ReturnsNoContent()
    {
        await using var factory = new CustomWebApplicationFactory();
        var carId = await SeedCarAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory, "Админ"));

        var response = await client.DeleteAsync($"/api/cars/{carId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/cars/{carId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}