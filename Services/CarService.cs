using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _carRepository;
    
    public CarService(ICarRepository carRepository)
    {
        _carRepository = carRepository;
    }

    public async Task<IEnumerable<Car>> GetAllAsync()
    {
        return await _carRepository.GetAllAsync();
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await _carRepository.GetByIdAsync(id);
    }

    public async Task<Car> CreateAsync(Car car)
    {
        await _carRepository.AddAsync(car);
        return car;
    }

    public async Task<bool> UpdateAsync(int id, Car car)
    {
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
            return false;
        existingCar.Brand = car.Brand;
        existingCar.Model = car.Model;
        existingCar.Year = car.Year;
        existingCar.Price = car.Price;
        existingCar.SupplierId = car.SupplierId;
        await _carRepository.UpdateAsync(existingCar);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
            return false;
        await _carRepository.DeleteAsync(existingCar);
        return true;
    }

    public async Task<IEnumerable<Car>> SearchAsync(string? brand, string? model, int? year)
    {
        return await _carRepository.SearchAsync(brand, model, year);
    }
}