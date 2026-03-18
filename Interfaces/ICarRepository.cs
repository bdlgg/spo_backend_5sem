using backend_spo.Entities;
namespace backend_spo.Interfaces;

public interface ICarRepository
{
    Task<IEnumerable<Car>> GetAllAsync();
    Task<Car?> GetByIdAsync(int id);
    Task AddAsync(Car car);
    Task UpdateAsync(Car car);
    Task DeleteAsync(Car car);
    Task<IEnumerable<Car>> SearchAsync(string? brand, string? model, int? year);
}