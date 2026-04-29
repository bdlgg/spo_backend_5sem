namespace backend_spo.DTOs;

public class CreateMyTestDriveDto
{
    public int CarId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Comment { get; set; } = string.Empty;
}
