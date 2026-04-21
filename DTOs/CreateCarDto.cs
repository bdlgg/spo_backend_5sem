namespace backend_spo.DTOs
{
    public class CreateCarDto
    {
        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int Year { get; set; }

        public decimal Price { get; set; }
        
        public string ImageUrl { get; set; } = string.Empty;

        public string Vin { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Type { get; set; } = "Новый";

        public string Status { get; set; } = "Available";

        public int SupplierId { get; set; }
    }
}