namespace backend_spo.DTOs
{
    public class AuditLogResponseDto
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string Result { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;
    }
}