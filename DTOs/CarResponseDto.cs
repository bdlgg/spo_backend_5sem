namespace backend_spo.DTOs;

public class CarResponseDto
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
}