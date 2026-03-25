using backend_spo.Entities;

namespace backend_spo.Interfaces
{
    public interface IAuditService
    {
        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<AuditLog?> GetByIdAsync(int id);
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);

        Task LogAsync(
            int? userId,
            string actionType,
            string entityName,
            int? entityId,
            string result,
            string details
        );
    }
}