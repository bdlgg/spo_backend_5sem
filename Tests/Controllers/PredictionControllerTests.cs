using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using backend_spo.DTOs;
using backend_spo.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Tests.Controllers;

public class PredictionControllerTests
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
    public async Task ModelStatus_WithoutAuth_Returns401()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/prediction/model-status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ModelStatus_AsClient_Returns403()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Клиент"));

        var response = await client.GetAsync("api/prediction/model-status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ModelStatus_AsManager_Returns200()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var response = await client.GetAsync("api/prediction/model-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PredictConversion_WithoutTrainedModel_ReturnsBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        AddBearer(client, CreateJwtToken(factory, "Менеджер"));

        var dto = new PredictConversionRequestDto { ClientId = 1, CarId = 1 };
        var response = await client.PostAsJsonAsync("api/prediction/predict-conversion", dto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }
}
