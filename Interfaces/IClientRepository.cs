using backend_spo.Entities;
namespace backend_spo.Interfaces;

public interface IClientRepository
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(int id);
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Client client);
    Task<IEnumerable<Client>> SearchAsync(string? firstName, string? lastName, string? phone);
}