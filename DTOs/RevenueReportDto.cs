namespace backend_spo.DTOs;

public class RevenueReportDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedSalesCount { get; set; }
    public decimal AverageSalesAmount { get; set; }
}