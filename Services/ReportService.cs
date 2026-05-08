using backend_spo.Data;
using backend_spo.DTOs;
using backend_spo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_spo.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime? from, DateTime? to)
    {
        var salesQuery = _context.Sales
            .Include(s => s.User)
            .Where(s => s.Status == "Completed")
            .AsQueryable();
        if (from.HasValue)
            salesQuery = salesQuery.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue)
            salesQuery = salesQuery.Where(s => s.SaleDate <= to.Value);
        var sales = await salesQuery.ToListAsync();
        var totalRevenue = sales.Sum(s => s.FinalPrice);
        var completedSalesCount = sales.Count;
        var averageSaleAmount = completedSalesCount > 0 ? totalRevenue / completedSalesCount : 0;
        return new RevenueReportDto
        {
            From = from,
            To = to,
            TotalRevenue = totalRevenue,
            CompletedSalesCount = completedSalesCount,
            AverageSalesAmount = averageSaleAmount
        };
    }

    public async Task<IEnumerable<ManagerPerformanceDto>> GetManagersPerformanceAsync(DateTime? from, DateTime? to)
    {
        var salesQuery = _context.Sales
            .Include(s => s.User)
            .Where(s => s.Status == "Completed")
            .AsQueryable();
        if (from.HasValue)
            salesQuery = salesQuery.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue)
            salesQuery = salesQuery.Where(s => s.SaleDate <= to.Value);
        var sales = await salesQuery.ToListAsync();
        var result = sales
            .Where(s => s.User != null)
            .GroupBy(s => new
            {
                s.UserId,
                s.User!.FirstName,
                s.User.LastName
            })
            .Select(g => new ManagerPerformanceDto
            {
                ManagerId = g.Key.UserId,
                ManagerFullName = $"{g.Key.FirstName} {g.Key.LastName}",
                SalesCount = g.Count(),
                TotalRevenue = g.Sum(x => x.FinalPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();
        return result;
    }
}