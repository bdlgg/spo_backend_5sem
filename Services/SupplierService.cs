using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _supplierRepository.GetAllAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _supplierRepository.GetByIdAsync(id);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        await _supplierRepository.AddAsync(supplier);
        return supplier;
    }

    public async Task<bool> UpdateAsync(int id, Supplier supplier)
    {
        var existingSupplier = await _supplierRepository.GetByIdAsync(id);
        if (existingSupplier == null)
            return false;

        existingSupplier.Name = supplier.Name;
        existingSupplier.ContactPerson = supplier.ContactPerson;
        existingSupplier.Phone = supplier.Phone;
        existingSupplier.Email = supplier.Email;
        existingSupplier.Address = supplier.Address;

        await _supplierRepository.UpdateAsync(existingSupplier);
        return true;
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var existingSupplier = await _supplierRepository.GetByIdAsync(id);
        if (existingSupplier == null)
            return (false, "Поставщик не найден");

        var hasCars = await _supplierRepository.HasCarsAsync(id);
        if (hasCars)
            return (false, "Нельзя удалить поставщика, так как к нему привязаны автомобили");

        await _supplierRepository.DeleteAsync(existingSupplier);
        return (true, "Поставщик удален");
    }

    public async Task<IEnumerable<Supplier>> SearchAsync(string? name, string? contactPerson, string? phone)
    {
        return await _supplierRepository.SearchAsync(name, contactPerson, phone);
    }
}