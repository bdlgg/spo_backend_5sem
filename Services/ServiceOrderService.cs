using backend_spo.Interfaces;
using backend_spo.Entities;

namespace backend_spo.Services;

public class ServiceOrderService : IServiceOrderService
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICarRepository _carRepository;
    private readonly IAuditService _auditService;

    public ServiceOrderService(IServiceOrderRepository serviceOrderRepository, IClientRepository clientRepository,
        ICarRepository carRepository, IAuditService auditService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _clientRepository = clientRepository;
        _carRepository = carRepository;
        _auditService = auditService;
    }

    public async Task<IEnumerable<ServiceOrder>> GetAllAsync()
    {
        return await _serviceOrderRepository.GetAllAsync();
    }

    public async Task<ServiceOrder?> GetByIdAsync(int id)
    {
        return await _serviceOrderRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ServiceOrder>> GetByClientIdAsync(int clientId)
    {
        return await _serviceOrderRepository.GetByClientIdAsync(clientId);
    }

    public async Task<IEnumerable<ServiceOrder>> GetByManagerIdAsync(int managerId)
    {
        return await _serviceOrderRepository.GetByManagerIdAsync(managerId);
    }

    public async Task<(bool Success, string Message, ServiceOrder? serviceOrder)> CreateAsync(ServiceOrder serviceOrder,
        int currentUserId)
    {
        
    }

    public async Task<(bool Success, string Message)> UpdateAsync(int id, string status, decimal? finalCost,
        int currentUserId)
    {
        
    }
}