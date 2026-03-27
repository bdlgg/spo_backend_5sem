namespace backend_spo.DTOs
{
    public class TestDriveResponseDto
    {
        public int Id { get; set; }

        public int CarId { get; set; }
        public string CarTitle { get; set; } = string.Empty;

        public int ClientId { get; set; }
        public string ClientFullName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public DateTime ScheduledAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}