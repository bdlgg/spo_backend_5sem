namespace backend_spo.Entities
{
	public class AuditLog
	{
		public int Id { get; set; }

		public int? UserId { get; set; }
		public User? User { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public string ActionType { get; set; } = string.Empty;
		// LOGIN, REGISTER, SALE_CREATE, USER_ROLE_UPDATE, USER_STATUS_UPDATE ...

		public string EntityName { get; set; } = string.Empty;
		// User, Sale, Car, Client, Supplier ...

		public int? EntityId { get; set; }

		public string Result { get; set; } = string.Empty;
		// Success / Failed

		public string Details { get; set; } = string.Empty;
	}
}