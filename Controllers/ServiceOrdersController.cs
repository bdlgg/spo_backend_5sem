using System.Security.Claims;
using backend_spo.DTOs;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceOrdersController : ControllerBase
{
    private readonly IServiceOrderService _serviceOrderService;
    public ServiceOrdersController(IServiceOrderService serviceOrderService)
    {
        _serviceOrderService = serviceOrderService;
    }

    [Authorize(Roles = "Админ,Руководитель,Менеджер")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceOrderResponseDto>>> GetAll()
    {
        var serviceOrders = await _serviceOrderService.GetAllAsync();
        return Ok(serviceOrders.Select(MapToDto));
    }

    [Authorize(Roles = "Админ,Руководитель,Менеджер")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceOrderResponseDto>> GetById(int id)
    {
        var serviceOrder = await _serviceOrderService.GetByIdAsync(id);
        if (serviceOrder == null)
            return NotFound(new { message = "Заказ-наряд не найден"});
        return Ok(MapToDto(serviceOrder));
    }

    [Authorize(Roles = "Админ,Руководитель,Менеджер")]
    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IEnumerable<ServiceOrderResponseDto>>> GetByClientId(int clientId)
    {
        var serviceOrders = await _serviceOrderService.GetByClientIdAsync(clientId);
        return Ok(serviceOrders.Select(MapToDto));
    }

    [Authorize(Roles = "Админ,Руководитель,Менеджер")]
    [HttpGet("manager/{managerId}")]
    public async Task<ActionResult<IEnumerable<ServiceOrderResponseDto>>> GetByManagerId(int managerId)
    {
        var serviceOrders = await _serviceOrderService.GetByManagerIdAsync(managerId);
        return Ok(serviceOrders.Select(MapToDto));
    }

    [Authorize(Roles = "Админ,Руководитель,Менеджер")]
    [HttpGet("car/{carId}")]
    public async Task<ActionResult<IEnumerable<ServiceOrderResponseDto>>> GetByCarId(int carId)
    {
        var serviceOrders = await _serviceOrderService.GetByCarIdAsync(carId);
        return Ok(serviceOrders.Select(MapToDto));
    }

    [Authorize(Roles = "Админ,Менеджер")]
    [HttpPost]
    public async Task<ActionResult<ServiceOrderResponseDto>> Create([FromBody] CreateServiceOrderDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { message = "Не удалось определить текущего пользователя" });
        var serviceOrder = new ServiceOrder
        {
            ClientId = dto.ClientId,
            CarId = dto.CarId,
            WorkDescription = dto.WorkDescription,
            PlannedCost = dto.PlannedCost
        };
        var (success, message, createdServiceOrder) = await _serviceOrderService.CreateAsync(serviceOrder, currentUserId);
        if (!success || createdServiceOrder == null)
            return BadRequest(new { message });
        return Ok(MapToDto(createdServiceOrder));
    }

    [Authorize(Roles = "Админ,Менеджер")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateServiceOrderStatusDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { message = "Не удалось определить текущего пользователя" });
        var (success, message) = await _serviceOrderService.UpdateStatusAsync(id, dto.Status, dto.FinalCost, currentUserId);
        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    private static ServiceOrderResponseDto MapToDto(ServiceOrder serviceOrder)
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
}