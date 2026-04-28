namespace backend_spo.DTOs
{
    public class CarResponseDto
    {
        public int Id { get; set; }

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int Year { get; set; }

        public decimal Price { get; set; }
        
        public string Vin { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int SupplierId { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public List<CarPhotoResponseDto> Photos { get; set; } = new();
    }
}