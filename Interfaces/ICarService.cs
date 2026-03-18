using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface ICarService
{
    Task<IEnumerable<Car>> GetAllAsync();
    Task<Car?> GetByIdAsync(int id);
    Task<Car> CreateAsync(Car car);
    Task<bool> UpdateAsync(int id, Car car);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Car>> SearchAsync(string? brand, string? model, int? year);
}