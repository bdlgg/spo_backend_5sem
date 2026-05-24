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

public class ClientsControllerTests
{
    private static async Task ClearClientRelatedDataAsync(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.ServiceOrders.RemoveRange(db.ServiceOrders);
        db.TestDrives.RemoveRange(db.TestDrives);
        db.Sales.RemoveRange(db.Sales);
        db.Clients.RemoveRange(db.Clients);

        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedClientAsync(
        CustomWebApplicationFactory factory,
        string firstName = "Алексей",
        string lastName = "Скворцов",
        string phone = "+79990000000",
        string? email = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var client = new Client
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email ?? CreateUniqueEmail()
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        return client.Id;
    }

    private static string CreateJwtToken(CustomWebApplicationFactory factory, string role = "Менеджер")
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var jwtSettings = configuration.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "manager@test.com"),
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

    private static CreateClientDto CreateClientDto(
        string firstName = "Мария",
        string lastName = "Иванова",
        string phone = "+79991112233",
        string? email = null)
    {
        return new CreateClientDto
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email ?? CreateUniqueEmail()
        };
    }

    private static CreateClientDto CreateInvalidClientDto()
    {
        return new CreateClientDto
        {
            FirstName = "",
            LastName = "",
            Phone = "",
            Email = "invalid-email"
        };
    }

    private static string CreateUniqueEmail()
    {
        return $"client_{Guid.NewGuid():N}@test.com";
    }

    [Fact]
    public async Task GetClients_WithManagerRole_ReturnsOkAndList()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        await SeedClientAsync(factory, firstName: "Алексей", lastName: "Скворцов");
        await SeedClientAsync(factory, firstName: "Мария", lastName: "Иванова");

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.GetAsync("/api/Clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var clients = await response.Content.ReadFromJsonAsync<List<ClientResponseDto>>();

        clients.Should().NotBeNull();
        clients!.Should().HaveCount(2);
        clients.Should().Contain(c => c.FirstName == "Алексей" && c.LastName == "Скворцов");
        clients.Should().Contain(c => c.FirstName == "Мария" && c.LastName == "Иванова");
    }

    [Fact]
    public async Task GetClients_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Clients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientById_WithManagerRoleAndExistingClient_ReturnsOk()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(
            factory,
            firstName: "Алексей",
            lastName: "Скворцов",
            phone: "+79990001122",
            email: "alex@test.com");

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.GetAsync($"/api/Clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponseDto>();

        result.Should().NotBeNull();
        result!.Id.Should().Be(clientId);
        result.FirstName.Should().Be("Алексей");
        result.LastName.Should().Be("Скворцов");
        result.Phone.Should().Be("+79990001122");
        result.Email.Should().Be("alex@test.com");
    }

    [Fact]
    public async Task GetClientById_WhenClientDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.GetAsync("/api/Clients/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClientById_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(factory);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostClient_WithManagerRoleAndValidData_ReturnsCreatedAndCreatesClient()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        var dto = CreateClientDto(
            firstName: "Никита",
            lastName: "Петров",
            phone: "+79995554433",
            email: "nikita@test.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/Clients", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdClient = await response.Content.ReadFromJsonAsync<ClientResponseDto>();

        createdClient.Should().NotBeNull();
        createdClient!.Id.Should().BeGreaterThan(0);
        createdClient.FirstName.Should().Be("Никита");
        createdClient.LastName.Should().Be("Петров");
        createdClient.Phone.Should().Be("+79995554433");
        createdClient.Email.Should().Be("nikita@test.com");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var clientFromDb = await db.Clients.FirstOrDefaultAsync(c => c.Id == createdClient.Id);

        clientFromDb.Should().NotBeNull();
        clientFromDb!.Email.Should().Be("nikita@test.com");
    }

    [Fact]
    public async Task PostClient_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.PostAsJsonAsync("/api/Clients", CreateInvalidClientDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostClient_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/Clients", CreateClientDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutClient_WithManagerRoleAndValidData_ReturnsOkAndUpdatesClient()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        var dto = CreateClientDto(
            firstName: "Обновленный",
            lastName: "Клиент",
            phone: "+79998887766",
            email: "updated@test.com");

        // Act
        var response = await client.PutAsJsonAsync($"/api/Clients/{clientId}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedClient = await response.Content.ReadFromJsonAsync<ClientResponseDto>();

        updatedClient.Should().NotBeNull();
        updatedClient!.Id.Should().Be(clientId);
        updatedClient.FirstName.Should().Be("Обновленный");
        updatedClient.LastName.Should().Be("Клиент");
        updatedClient.Phone.Should().Be("+79998887766");
        updatedClient.Email.Should().Be("updated@test.com");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var clientFromDb = await db.Clients.FirstAsync(c => c.Id == clientId);

        clientFromDb.FirstName.Should().Be("Обновленный");
        clientFromDb.LastName.Should().Be("Клиент");
        clientFromDb.Phone.Should().Be("+79998887766");
        clientFromDb.Email.Should().Be("updated@test.com");
    }

    [Fact]
    public async Task PutClient_WhenClientDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.PutAsJsonAsync("/api/Clients/999999", CreateClientDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutClient_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.PutAsJsonAsync($"/api/Clients/{clientId}", CreateInvalidClientDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutClient_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(factory);
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/Clients/{clientId}", CreateClientDto());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteClient_WithManagerRoleAndExistingClient_ReturnsNoContentAndDeletesClient()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.DeleteAsync($"/api/Clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/Clients/{clientId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteClient_WhenClientDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.DeleteAsync("/api/Clients/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteClient_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        var clientId = await SeedClientAsync(factory);
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/Clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchClients_WithQuery_ReturnsOkAndFilteredClients()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        await SeedClientAsync(
            factory,
            firstName: "Алексей",
            lastName: "Скворцов",
            phone: "+79990000001",
            email: "alexey@test.com");

        await SeedClientAsync(
            factory,
            firstName: "Мария",
            lastName: "Иванова",
            phone: "+79990000002",
            email: "maria@test.com");

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.GetAsync("/api/Clients/search?query=Алексей");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var clients = await response.Content.ReadFromJsonAsync<List<ClientResponseDto>>();

        clients.Should().NotBeNull();
        clients!.Should().ContainSingle();
        clients[0].FirstName.Should().Be("Алексей");
        clients[0].Email.Should().Be("alexey@test.com");
    }

    [Fact]
    public async Task SearchClients_WithQueryAndNoMatches_ReturnsOkAndEmptyList()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        await ClearClientRelatedDataAsync(factory);

        await SeedClientAsync(
            factory,
            firstName: "Алексей",
            lastName: "Скворцов",
            phone: "+79990000001",
            email: "alexey@test.com");

        var client = factory.CreateClient();
        AddBearerToken(client, CreateJwtToken(factory));

        // Act
        var response = await client.GetAsync("/api/Clients/search?query=НесуществующийКлиент");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var clients = await response.Content.ReadFromJsonAsync<List<ClientResponseDto>>();

        clients.Should().NotBeNull();
        clients!.Should().BeEmpty();
    }
}