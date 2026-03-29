namespace backend_spo.DTOs;

public class ManagerPerformanceDto
{
    public int ManagerId { get; set; }
    public string ManagerFullName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public decimal TotalRevenue { get; set; }
}