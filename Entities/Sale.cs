namespace backend_spo.Entities;

public class Sale
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Car? Car { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal FinalPrice { get; set; }
    public string Status { get; set; } = string.Empty;

}