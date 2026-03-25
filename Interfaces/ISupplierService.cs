using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<bool> UpdateAsync(int id, Supplier supplier);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<IEnumerable<Supplier>> SearchAsync(string? name, string? contactPerson, string? phone);
}