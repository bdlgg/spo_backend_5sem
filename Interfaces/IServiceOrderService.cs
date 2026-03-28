using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface IServiceOrderService
{
    Task<IEnumerable<ServiceOrder>> GetAllAsync();
    Task<ServiceOrder?> GetByIdAsync(int id);
    Task<IEnumerable<ServiceOrder>> GetByClientIdAsync(int clientId);
    Task<IEnumerable<ServiceOrder>> GetByManagerIdAsync(int managerId);
    Task<IEnumerable<ServiceOrder>> GetByCarIdAsync(int carId);
    Task<(bool Success, string Message, ServiceOrder? serviceOrder)> CreateAsync(ServiceOrder serviceOrder, int currentUserId);
    Task<(bool Success, string Message)> UpdateStatusAsync(int id, string status, decimal? finalCost, int currentUserId);
}