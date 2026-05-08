using backend_spo.DTOs;

namespace backend_spo.Interfaces;

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime? from, DateTime? to);
    Task<IEnumerable<ManagerPerformanceDto>> GetManagersPerformanceAsync(DateTime? from, DateTime? to);
}