using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
    Task<IEnumerable<Supplier>> SearchAsync(string? name, string? contactPerson, string? phone);
    Task<bool> HasCarsAsync(int supplierId);
}