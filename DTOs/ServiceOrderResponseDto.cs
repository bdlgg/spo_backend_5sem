namespace backend_spo.DTOs;

public class ServiceOrderResponseDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientFullName { get; set; } = string.Empty;
    public int CarId { get; set; }
    public string CarTitle { get; set; } = string.Empty;
    public int ManagerId { get; set; }
    public string ManagerFullName { get; set; } = string.Empty;
    public string WorkDescription { get; set; } = string.Empty;
    public decimal PlannedCost { get; set; }
    public decimal? FinalCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}