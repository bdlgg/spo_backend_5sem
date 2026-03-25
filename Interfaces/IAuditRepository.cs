using backend_spo.Entities;

namespace backend_spo.Interfaces
{
    public interface IAuditRepository
    {
        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<AuditLog?> GetByIdAsync(int id);
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
        Task AddAsync(AuditLog auditLog);
    }
}