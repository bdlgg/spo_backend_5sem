using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface ISaleService
{
    Task<IEnumerable<Sale>> GetAllAsync();
    Task<Sale?> GetByIdAsync(int id);
    Task<(bool Success, string Message, Sale? sale)> CreateAsync(Sale sale);
    Task<bool> UpdateAsync(int id, Sale sale);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Sale>> GetByClientIdAsync(int clientId);
    Task<IEnumerable<Sale>> GetByUserIdAsync(int userId);
}