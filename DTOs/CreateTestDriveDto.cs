namespace backend_spo.DTOs
{
    public class CreateTestDriveDto
    {
        public int CarId { get; set; }

        public int ClientId { get; set; }

        public DateTime ScheduledAt { get; set; }
    }
}