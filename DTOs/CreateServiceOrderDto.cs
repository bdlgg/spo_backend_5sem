namespace backend_spo.DTOs;

public class CreateServiceOrderDto
{
    public int ClientId { get; set; }
    public int CarId { get; set; }
    public string WorkDescription { get; set; } = string.Empty;
    public decimal PlannedCost { get; set; }
}