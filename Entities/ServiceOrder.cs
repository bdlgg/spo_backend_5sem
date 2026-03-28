namespace backend_spo.Entities;

public class ServiceOrder
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int ManagerId { get; set; }
    public int CarId { get; set; }
    public Car? Car { get; set; }
    public User? Manager { get; set; }
    public string WorkDescription { get; set; } = string.Empty;
    public decimal PlannedCost { get; set; }
    public decimal? FinalCost { get; set; }
    public string Status { get; set; } = "Accepted"; // Accepted / InProgress / Completed / Canceled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}