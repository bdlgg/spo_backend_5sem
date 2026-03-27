namespace backend_spo.Entities
{
    public class TestDrive
    {
        public int Id { get; set; }

        public int CarId { get; set; }
        public Car? Car { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public int ManagerId { get; set; }
        public User? Manager { get; set; }

        public DateTime ScheduledAt { get; set; }

        public string Status { get; set; } = "Scheduled";
        // Scheduled / Completed / Cancelled

        public string Result { get; set; } = string.Empty;
        // Interested / Rejected / NeedConsultation

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}