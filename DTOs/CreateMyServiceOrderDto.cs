namespace backend_spo.DTOs;

public class CreateMyServiceOrderDto
{
    public int CarId { get; set; }
    public string WorkDescription { get; set; } = string.Empty;
}
