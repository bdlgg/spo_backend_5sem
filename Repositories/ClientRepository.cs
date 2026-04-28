using Microsoft.EntityFrameworkCore;
using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;
    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await _context.Clients
            .Include(c => c.User)
            .ThenInclude(u => u.Role)
            .Where(c => c.UserId == null || c.User!.Role!.Name == "Клиент")
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _context.Clients
            .Include(c => c.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == null || c.User!.Role!.Name == "Клиент"));
    }

    public async Task AddAsync(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Client client)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Client client)
    {
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Client>> SearchAsync(string? firstName, string? lastName, string? phone)
    {
        var query = _context.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(c => c.FirstName.Contains(firstName));
        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(c => c.LastName.Contains(lastName));
        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(c => c.Phone.Contains(phone));
        return await query.ToListAsync();
    }
}