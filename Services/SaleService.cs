using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICarRepository _carRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IAuditService _auditService;

    public SaleService(
    ISaleRepository saleRepository,
    ICarRepository carRepository,
    IClientRepository clientRepository,
    IAuditService auditService)
    {
        _saleRepository = saleRepository;
        _carRepository = carRepository;
        _clientRepository = clientRepository;
        _auditService = auditService;
    }

    public async Task<IEnumerable<Sale>> GetAllAsync()
    {
        return await _saleRepository.GetAllAsync();
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        return await _saleRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, Sale? sale)> CreateAsync(Sale sale, int currentUserId)
    {
        var car = await _carRepository.GetByIdAsync(sale.CarId);
        if (car == null)
            return (false, "Автомобиль не найден", null);

        var client = await _clientRepository.GetByIdAsync(sale.ClientId);
        if (client == null)
            return (false, "Клиент не найден", null);

        if (sale.FinalPrice <= 0)
            return (false, "Итоговая цена должна быть больше нуля", null);

        if (car.Status == "Sold")
        {
            await _auditService.LogAsync(
                currentUserId,
                "SALE_CREATE",
                "Sale",
                null,
                "Failed",
                $"Попытка повторной продажи автомобиля Id={sale.CarId}"
            );
            return (false, "Этот автомобиль уже продан", null);
        }

        if (!car.IsActive)
            return (false, "Автомобиль неактивен", null);

        sale.UserId = currentUserId;

        if (string.IsNullOrWhiteSpace(sale.Status))
            sale.Status = "Completed";

        sale.SaleDate = DateTime.UtcNow;

        await _saleRepository.AddAsync(sale);

        car.Status = "Sold";
        await _carRepository.UpdateAsync(car);

        await _auditService.LogAsync(
    currentUserId,
    "SALE_CREATE",
    "Sale",
    sale.Id,
    "Success",
    $"Создана продажа автомобиля Id={sale.CarId} клиенту Id={sale.ClientId}. Итоговая цена: {sale.FinalPrice}"
);

        return (true, "Сделка успешно создана", sale);
    }

    public async Task<bool> UpdateAsync(int id, Sale sale)
    {
        var existingSale = await _saleRepository.GetByIdAsync(id);
        if (existingSale == null)
            return false;

        existingSale.CarId = sale.CarId;
        existingSale.ClientId = sale.ClientId;
        existingSale.FinalPrice = sale.FinalPrice;
        existingSale.Status = sale.Status;

        await _saleRepository.UpdateAsync(existingSale);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existingSale = await _saleRepository.GetByIdAsync(id);
        if (existingSale == null)
            return false;

        await _saleRepository.DeleteAsync(existingSale);
        return true;
    }

    public async Task<IEnumerable<Sale>> GetByClientIdAsync(int clientId)
    {
        return await _saleRepository.GetByClientIdAsync(clientId);
    }

    public async Task<IEnumerable<Sale>> GetByUserIdAsync(int userId)
    {
        return await _saleRepository.GetByUserIdAsync(userId);
    }
}