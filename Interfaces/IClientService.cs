using backend_spo.Entities;

namespace backend_spo.Interfaces;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(int id);
    Task<Client> CreateAsync(Client client);
    Task<bool> UpdateAsync(int id, Client client);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Client>> SearchAsync(string? firstName, string? lastName, string? phone);
}