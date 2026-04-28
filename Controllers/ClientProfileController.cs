using System.Security.Claims;
using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/client-profile")]
[Authorize(Roles = "Клиент")]
public class ClientProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public  ClientProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ClientDashboardSummaryDto>> GetSummary()
    {
        var client = await GetCurrentClientAsync();
        if (client == null)
            return NotFound(new { message = "Профиль клиента не найден" });
        
        var testDrives = await _context.TestDrives
            .Where(t => t.ClientId == client.Id)
            .ToListAsync();
        
        var serviceOrders = await _context.ServiceOrders
            .Where(s => s.ClientId == client.Id)
            .ToListAsync();
        
        var sales = await  _context.Sales
            .Where(s => s.ClientId == client.Id)
            .ToListAsync();

        var summary = new ClientDashboardSummaryDto
        {
            ClientId = client.Id,
            ClientFullName = $"{client.FirstName} {client.LastName}",
            TestDrivesCount = testDrives.Count,
            ActiveTestDrivesCount = testDrives.Count(td => td.Status == "Scheduled"),
            ServiceOrdersCount = serviceOrders.Count,
            ActiveServiceOrdersCount = serviceOrders.Count(so => so.Status is "Accepted" or "InProgress"),
            SalesCount = sales.Count,
            TotalPurchasesAmount = sales.Sum(s => s.FinalPrice)
        };
        return Ok(summary);
    }

    [HttpGet("test-drives")]
    public async Task<ActionResult<IEnumerable<TestDriveResponseDto>>> GetMyTestDrives()
    {
        var client = await GetCurrentClientAsync();
        if (client == null)
            return NotFound(new { message = "Профиль клиента не найден" });

        var testDrives = await _context.TestDrives
            .Include(td => td.Car)
            .Include(td => td.Client)
            .Include(td => td.Manager)
            .Where(td => td.ClientId == client.Id)
            .OrderByDescending(td => td.CreatedAt)
            .ToListAsync();
        
        return Ok(testDrives.Select(MapTestDriveToDto));
    }

    [HttpGet("service-orders")]
    public async Task<ActionResult<IEnumerable<ServiceOrderResponseDto>>> GetMyServiceOrders()
    {
        var client = await GetCurrentClientAsync();
        if (client == null)
            return NotFound(new { message = "Профиль клиента не найден" });
        
        var serviceOrders = await _context.ServiceOrders
            .Include(so => so.Client)
            .Include(so => so.Car)
            .Include(so => so.Manager)
            .Where(so => so.ClientId == client.Id)
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();
        
        return Ok(serviceOrders.Select(MapServiceOrderToDto));
    }

    [HttpGet("sales")]
    public async Task<ActionResult<IEnumerable<SaleResponseDto>>> GetMySales()
    {
        var client = await GetCurrentClientAsync();
        if (client == null)
            return NotFound(new { message = "Профиль клиента не найден" });
        
        var sales = await _context.Sales
            .Include(s => s.Car)
            .Include(s => s.Client)
            .Include(s => s.User)
            .Where(s => s.ClientId == client.Id)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
        
        return Ok(sales.Select(MapSaleToDto));
    }

    private async Task<Client?> GetCurrentClientAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return await _context.Clients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    private static TestDriveResponseDto MapTestDriveToDto(TestDrive testDrive)
    {
        return new TestDriveResponseDto
        {
            Id = testDrive.Id,
            CarId = testDrive.CarId,
            CarTitle = testDrive.Car == null
                ? string.Empty
                : $"{testDrive.Car.Brand} {testDrive.Car.Model} ({testDrive.Car.Year})",
            ClientId = testDrive.ClientId,
            ClientFullName = testDrive.Client == null
                ? string.Empty
                : $"{testDrive.Client.FirstName} {testDrive.Client.LastName}",
            ManagerId = testDrive.ManagerId,
            ManagerName = testDrive.Manager == null
                ? string.Empty
                : $"{testDrive.Manager.FirstName} {testDrive.Manager.LastName}",
            ScheduledAt = testDrive.ScheduledAt,
            Status = testDrive.Status,
            Result = testDrive.Result,
            Comment = testDrive.Comment,
            CreatedAt = testDrive.CreatedAt
        };
    }

    private static ServiceOrderResponseDto MapServiceOrderToDto(ServiceOrder serviceOrder)
    {
        return new ServiceOrderResponseDto
        {
            Id = serviceOrder.Id,
            ClientId = serviceOrder.ClientId,
            ClientFullName = serviceOrder.Client == null
                ? string.Empty
                : $"{serviceOrder.Client.FirstName} {serviceOrder.Client.LastName}",
            CarId = serviceOrder.CarId,
            CarTitle = serviceOrder.Car == null
                ? string.Empty
                : $"{serviceOrder.Car.Brand} {serviceOrder.Car.Model} ({serviceOrder.Car.Year})",
            ManagerId = serviceOrder.ManagerId,
            ManagerFullName = serviceOrder.Manager == null
                ? string.Empty
                : $"{serviceOrder.Manager.FirstName} {serviceOrder.Manager.LastName}",
            WorkDescription = serviceOrder.WorkDescription,
            PlannedCost = serviceOrder.PlannedCost,
            FinalCost = serviceOrder.FinalCost,
            Status = serviceOrder.Status,
            CreatedAt = serviceOrder.CreatedAt,
            CompletedAt = serviceOrder.CompletedAt
        };
    }

    private static SaleResponseDto MapSaleToDto(Sale sale)
    {
        return new SaleResponseDto
        {
            Id = sale.Id,
            CarId = sale.CarId,
            ClientId = sale.ClientId,
            UserId = sale.UserId,
            SaleDate = sale.SaleDate,
            FinalPrice = sale.FinalPrice,
            Status = sale.Status,
        };
    }
}