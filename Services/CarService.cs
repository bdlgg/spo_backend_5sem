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
        ValidateCar(car);

        if (string.IsNullOrWhiteSpace(car.Status))
            car.Status = "Available";

        await _carRepository.AddAsync(car);
        return car;
    }

    public async Task<bool> UpdateAsync(int id, Car car)
    {
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
            return false;

        ValidateCar(car);

        existingCar.Brand = car.Brand;
        existingCar.Model = car.Model;
        existingCar.Year = car.Year;
        existingCar.Price = car.Price;
        existingCar.Vin = car.Vin;
        existingCar.Color = car.Color;
        existingCar.Type = car.Type;
        existingCar.Status = car.Status;
        existingCar.IsActive = car.IsActive;
        existingCar.SupplierId = car.SupplierId;
        existingCar.Photos.Clear();

        foreach (var photo in car.Photos
                     .Where(p => !string.IsNullOrWhiteSpace(p.Url))
                     .OrderBy(p => p.SortOrder))
        {
            existingCar.Photos.Add(new CarPhoto
            {
                Url = photo.Url.Trim(),
                SortOrder = photo.SortOrder
            });
        }

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

    private static void ValidateCar(Car car)
    {
        if (string.IsNullOrWhiteSpace(car.Brand))
            throw new ArgumentException("Марка автомобиля обязательна");

        if (string.IsNullOrWhiteSpace(car.Model))
            throw new ArgumentException("Модель автомобиля обязательна");

        if (string.IsNullOrWhiteSpace(car.Vin))
            throw new ArgumentException("VIN обязателен");

        if (car.Price <= 0)
            throw new ArgumentException("Цена должна быть больше нуля");

        if (car.Year < 1900 || car.Year > DateTime.UtcNow.Year + 1)
            throw new ArgumentException("Некорректный год выпуска");

        if (string.IsNullOrWhiteSpace(car.Status))
            throw new ArgumentException("Статус автомобиля обязателен");
    }
}