using Microsoft.EntityFrameworkCore;
using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _context;

    public SupplierRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers.ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Supplier supplier)
    {
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Supplier>> SearchAsync(string? name, string? contactPerson, string? phone)
    {
        var query = _context.Suppliers.AsQueryable();
        
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        if (!string.IsNullOrWhiteSpace(name))
            query = isInMemory 
                ? query.Where(s => s.Name.ToLower().Contains(name.ToLower()))
                : query.Where(s => EF.Functions.ILike(s.Name, $"%{name}%"));

        if (!string.IsNullOrWhiteSpace(contactPerson))
            query = isInMemory
                ? query.Where(s => (s.ContactPerson ?? "").ToLower().Contains(contactPerson.ToLower()))
                : query.Where(s => EF.Functions.ILike(s.ContactPerson, $"%{contactPerson}%"));

        if (!string.IsNullOrWhiteSpace(phone))
            query = isInMemory
                ? query.Where(s => (s.Phone ?? "").ToLower().Contains(phone.ToLower()))
                : query.Where(s => EF.Functions.ILike(s.Phone, $"%{phone}%"));

        return await query.ToListAsync();
    }

    public async Task<bool> HasCarsAsync(int supplierId)
    {
        return await _context.Cars.AnyAsync(c => c.SupplierId == supplierId);
    }
}