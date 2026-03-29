using backend_spo.DTOs;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace backend_spo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Админ,Руководитель")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> GetRevenueReport([FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var report = await _reportService.GetRevenueReportAsync(from, to);
        return Ok(report);
    }

    [HttpGet("managers-performance")]
    public async Task<ActionResult<IEnumerable<ManagerPerformanceDto>>> GetManagersPerformance(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var report = await _reportService.GetManagersPerformanceAsync(from, to);
        return Ok(report);
    }
    
}