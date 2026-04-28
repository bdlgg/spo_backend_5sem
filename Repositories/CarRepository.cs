using Microsoft.EntityFrameworkCore;
using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Repositories;

public class CarRepository : ICarRepository
{
    private readonly ApplicationDbContext _context;
    
    public CarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Car>> GetAllAsync()
    {
        return await _context.Cars
            .Include(c => c.Supplier).Include(c => c.Photos).ToListAsync();
    }

    public async Task<Car?> GetByIdAsync(int id)
    {
        return await _context.Cars.Include(c => c.Supplier).Include(c => c.Photos).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Car car)
    {
        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Car car)
    {
        _context.Cars.Update(car);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Car car)
    {
        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Car>> SearchAsync(string? brand, string? model, int? year)
    {
        var query = _context.Cars.Include(c => c.Supplier).Include(c => c.Photos).AsQueryable();
        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(c => c.Brand.Contains(brand));
        if (!string.IsNullOrWhiteSpace(model))
            query = query.Where(c => c.Model.Contains(model));
        if (year.HasValue)
            query = query.Where(c => c.Year == year.Value);

        return await query.ToListAsync();
    }
}