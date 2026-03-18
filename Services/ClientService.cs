using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;

    public ClientService(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await _clientRepository.GetAllAsync();
    }
    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _clientRepository.GetByIdAsync(id);
    }

    public async Task<Client> CreateAsync(Client client)
    {
        await _clientRepository.AddAsync(client);
        return client;
    }

    public async Task<bool> UpdateAsync(int id, Client client)
    {
        var existingClient = await _clientRepository.GetByIdAsync(id);
        if (existingClient == null)
            return false;
        existingClient.FirstName = client.FirstName;
        existingClient.LastName = client.LastName;
        existingClient.Phone = client.Phone;
        existingClient.Email = client.Email;
        await _clientRepository.UpdateAsync(existingClient);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existingClient = await _clientRepository.GetByIdAsync(id);
        if (existingClient == null)
            return false;
        await _clientRepository.DeleteAsync(existingClient);
        return true;
    }

    public async Task<IEnumerable<Client>> SearchAsync(string? firstName, string? lastName, string? phone)
    {
        return await _clientRepository.SearchAsync(firstName, lastName, phone);
    }
}