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

    public async Task<IEnumerable<ServiceOrder>> GetByCarIdAsync(int carId)
    {
        return await _serviceOrderRepository.GetByCarIdAsync(carId);
    }

    public async Task<(bool Success, string Message, ServiceOrder? serviceOrder)> CreateAsync(ServiceOrder serviceOrder, int currentUserId)
    {
        var client = await _clientRepository.GetByIdAsync(serviceOrder.ClientId);
        if (client == null)
        {
            await _auditService.LogAsync(
                currentUserId,
                "SERVICE_ORDER_CREATE",
                "ServiceOrder",
                null,
                "Failed",
                $"Попытка создать заказ-наряд для несуществующего клиента Id={serviceOrder.ClientId}"
            );
            return (false, "Клиент не найден", null);
        }
        var car = await _carRepository.GetByIdAsync(serviceOrder.CarId);
        if (car == null)
        {
            await _auditService.LogAsync(
                currentUserId,
                "SERVICE_ORDER_CREATE",
                "ServiceOrder",
                null,
                "Failed",
                $"Попытка создать заказ-наряд для несуществующего автомобиля Id={serviceOrder.CarId}"
            );
            return (false, "Автомобиль не найден", null);
        }

        if (!car.IsActive)
            return (false, "Автомобиль неактивен", null);
        if (string.IsNullOrWhiteSpace(serviceOrder.WorkDescription))
            return (false, "Описание работ обязательно", null);
        if (serviceOrder.PlannedCost <= 0)
            return (false, "Плановая стоимость должна быть больше нуля", null);
        serviceOrder.ManagerId = currentUserId;
        serviceOrder.Status = "Accepted";
        serviceOrder.CreatedAt = DateTime.UtcNow;
        serviceOrder.FinalCost = null;
        serviceOrder.CompletedAt = null;
        await _serviceOrderRepository.AddAsync(serviceOrder);
        await _auditService.LogAsync(
            currentUserId,
            "SERVICE_ORDER_CREATE",
            "ServiceOrder",
            serviceOrder.Id,
            "Success",
            $"Создан заказ-наряд Id={serviceOrder.Id} для клиента Id={serviceOrder.ClientId}, автомобиля Id={serviceOrder.CarId}"
        );
        return (true, "Заказ-наряд успешно создан", serviceOrder);
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(int id, string status, decimal? finalCost, int currentUserId)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(id);
        if (serviceOrder == null)
            return (false, "Заказ-наряд не найден");
        var normalizedStatus = status?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedStatus))
            return (false, "Статус обязателен");
        var allowedStatuses = new[] { "Accepted", "InProgress", "Completed", "Cancelled" };
        if (!allowedStatuses.Contains(normalizedStatus))
            return (false, "Недопустимый статус, возможные статусы: Completed, InProgress, Cancelled, Accepted(возврат невозможен)");
        if (serviceOrder.Status == "Accepted")
            return (false, "Завершенный заказ-наряд нельзя изменить");
        if (serviceOrder.Status == "Cancelled")
            return (false, "Отмененный заказ-наряд нельзя изменить");
        switch (normalizedStatus)
        {
            case "Completed":
                if (serviceOrder.Status == "Accepted")
                    return (false, "Нельзя завершить заказ-наряд без статуса InProgress");
                if (!finalCost.HasValue || finalCost.Value <= 0)
                    return (false, "Для завершения заказ-наряда нужно указать итоговую стоимость больше нуля");
                serviceOrder.Status = "Completed";
                serviceOrder.FinalCost = finalCost.Value;
                serviceOrder.CompletedAt =  DateTime.UtcNow;
                break;
            case "InProgress":
                if (serviceOrder.Status != "Accepted")
                    return (false, "в InProgress можно перевести только заказ-наряд со статусом Accepted");
                serviceOrder.Status = "InProgress";
                break;
            case "Cancelled":
                serviceOrder.Status = "Cancelled";
                break;
            case "Accepted":
                return (false, "Возврат в статус Accepted невозможен");
            default:
                return (false,
                    "Недопустимый статус, возможные статусы: Completed, InProgress, Cancelled, Accepted(возврат невозможен)");
        }

        await _serviceOrderRepository.UpdateAsync(serviceOrder);
        await _auditService.LogAsync(
            currentUserId,
            "SERVICE_ORDER_STATUS_UPDATE",
            "ServiceOrder",
            serviceOrder.Id,
            "Success",
            $"Заказ-наряд Id={serviceOrder.Id} переведен в статус {serviceOrder.Status}"
        );
        return (true, "Статус заказ-наряда успешно обновлен");
    }
}