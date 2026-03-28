using backend_spo.Entities;
namespace backend_spo.Interfaces;

public interface IServiceOrderRepository
{
    Task<IEnumerable<ServiceOrder>> GetAllAsync();
    Task<ServiceOrder?> GetByIdAsync(int id);
    Task<IEnumerable<ServiceOrder>> GetByClientIdAsync(int clientId);
    Task<IEnumerable<ServiceOrder>> GetByManagerIdAsync(int managerId);
    Task AddAsync(ServiceOrder serviceOrder);
    Task UpdateAsync(ServiceOrder serviceOrder);
    Task DeleteAsync(ServiceOrder serviceOrder);
}