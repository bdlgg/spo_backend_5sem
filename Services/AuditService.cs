using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;

        public AuditService(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return await _auditRepository.GetAllAsync();
        }

        public async Task<AuditLog?> GetByIdAsync(int id)
        {
            return await _auditRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
        {
            return await _auditRepository.GetByUserIdAsync(userId);
        }

        public async Task LogAsync(
            int? userId,
            string actionType,
            string entityName,
            int? entityId,
            string result,
            string details)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                Result = result,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(auditLog);
        }
    }
}