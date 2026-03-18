using Microsoft.EntityFrameworkCore;
using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly ApplicationDbContext _context;
    public SaleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sale>> GetAllAsync()
    {
        return await _context.Sales
            .Include(s => s.Car)
            .Include(s => s.Client)
            .Include(s => s.User).ToListAsync();
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        return await _context.Sales.Include(s => s.Car)
            .Include(s => s.Client)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(Sale sale)
    {
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Sale sale)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Sale sale)
    {
        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Sale>> GetByClientIdAsync(int clientId)
    {
        return await _context.Sales
            .Include(s => s.Car)
            .Include(s => s.Client)
            .Include(s => s.User)
            .Where(s => s.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Sale>> GetByUserIdAsync(int userId)
    {
        return await _context.Sales
            .Include(s => s.Car)
            .Include(s => s.Client)
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }
}