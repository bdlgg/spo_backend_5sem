using backend_spo.Entities;
namespace backend_spo.Interfaces;

public interface ISaleRepository
{
    Task<IEnumerable<Sale>> GetAllAsync();
    Task<Sale?> GetByIdAsync(int id);
    Task AddAsync(Sale sale);
    Task UpdateAsync(Sale sale);
    Task DeleteAsync(Sale sale);
    Task<IEnumerable<Sale>> GetByClientIdAsync(int clientId);
    Task<IEnumerable<Sale>> GetByUserIdAsync(int userId);
}