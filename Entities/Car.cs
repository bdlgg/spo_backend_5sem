namespace backend_spo.Entities
{
    public class Car
    {
        public int Id { get; set; }

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int Year { get; set; }

        public decimal Price { get; set; }
        
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}