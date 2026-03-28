using backend_spo.Data;
using Microsoft.EntityFrameworkCore;
using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Repositories;

public class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceOrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceOrder>> GetAllAsync()
    {
        return await _context.ServiceOrders
            .Include(so => so.Client)
            .Include(so => so.Car)
            .Include(so => so.Manager)
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceOrder?> GetByIdAsync(int id)
    {
        return await _context.ServiceOrders
            .Include(so => so.Client)
            .Include(so => so.Car)
            .Include(so => so.Manager)
            .FirstOrDefaultAsync(so => so.Id == id);
    }

    public async Task<IEnumerable<ServiceOrder>> GetByClientIdAsync(int clientId)
    {
        return await _context.ServiceOrders
            .Include(so => so.Client)
            .Include(so => so.Car)
            .Include(so => so.Manager)
            .Where(so => so.ClientId == clientId)
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceOrder>> GetByManagerIdAsync(int managerId)
    {
        return await _context.ServiceOrders
            .Include(so => so.Client)
            .Include(so => so.Car)
            .Include(so => so.Manager)
            .Where(so => so.ManagerId == managerId)
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(ServiceOrder serviceOrder)
    {
        _context.ServiceOrders.Add(serviceOrder);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ServiceOrder serviceOrder)
    {
        _context.ServiceOrders.Update(serviceOrder);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ServiceOrder serviceOrder)
    {
        _context.ServiceOrders.Remove(serviceOrder);
        await _context.SaveChangesAsync();
    }
}