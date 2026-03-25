namespace backend_spo.DTOs
{
    public class CreateSaleDto
    {
        public int CarId { get; set; }

        public int ClientId { get; set; }

        public decimal FinalPrice { get; set; }

        public string Status { get; set; } = "Completed";
    }
}