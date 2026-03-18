using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICarRepository _carRepository;
    private readonly IClientRepository _clientRepository;

    public SaleService(ISaleRepository saleRepository, ICarRepository carRepository, IClientRepository clientRepository)
    {
        _saleRepository = saleRepository;
        _carRepository = carRepository;
        _clientRepository = clientRepository;
    }

    public async Task<IEnumerable<Sale>> GetAllAsync()
    {
        return await _saleRepository.GetAllAsync();
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        return await _saleRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message, Sale? sale)> CreateAsync(Sale sale)
    {
        var car = await _carRepository.GetByIdAsync(sale.CarId);
        if (car == null)
            return (false, "Автомобиль не найден", null);
        
        var client = await _clientRepository.GetByIdAsync(sale.ClientId);
        if (client == null)
            return (false, "Клиент не найден", null);

        if (sale.FinalPrice <= 0)
            return (false, "Итоговая цена должна быть больше нуля", null);

        if (string.IsNullOrWhiteSpace(sale.Status))
            sale.Status = "Completed";
        await _saleRepository.AddAsync(sale);
        return (true, "Сделка успешно создана", sale);
    }

    public async Task<bool> UpdateAsync(int id, Sale sale)
    {
        var existingSale = await _saleRepository.GetByIdAsync(id);
        if (existingSale == null)
            return false;
        existingSale.CarId = sale.CarId;
        existingSale.ClientId = sale.ClientId;
        existingSale.UserId = sale.UserId;
        existingSale.SaleDate = sale.SaleDate;
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