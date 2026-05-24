using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using backend_spo.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Tests.Controllers;

public class AuditLogsControllerTests
{
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
    public async Task GetAuditLogs_WithoutAuth_Returns401()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/auditlogs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogs_AsAdmin_Returns200()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var response = await client.GetAsync("api/auditlogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditLogById_NotFound_Returns404()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var response = await client.GetAsync("api/auditlogs/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAuditLogsByUserId_AsAdmin_Returns200()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Админ"));

        var response = await client.GetAsync("api/auditlogs/user/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
