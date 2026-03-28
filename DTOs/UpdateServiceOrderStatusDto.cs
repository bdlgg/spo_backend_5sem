namespace backend_spo.DTOs;

public class UpdateServiceOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public decimal? FinalCost { get; set; }
}