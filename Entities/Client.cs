namespace backend_spo.Entities
{
    public class Client
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        
        public int? UserId { get; set; }
        
        public User? User { get; set; }
        
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}