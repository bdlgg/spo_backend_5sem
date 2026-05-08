namespace backend_spo.DTOs;

public class ClientDashboardSummaryDto
{
    public int ClientId { get; set; }
    public string ClientFullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TestDrivesCount { get; set; }
    public int ActiveTestDrivesCount { get; set; }
    public int ServiceOrdersCount { get; set; }
    public int ActiveServiceOrdersCount { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalPurchasesAmount { get; set; }
}