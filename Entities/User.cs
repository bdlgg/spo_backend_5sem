namespace backend_spo.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}