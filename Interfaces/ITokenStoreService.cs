namespace backend_spo.Interfaces;

public interface ITokenStoreService
{
    Task SaveTokenAsync(int userId, string token, TimeSpan ttl);
    Task<bool> IsTokenActiveAsync(string token);
    Task RemoveTokenAsync(string token);
}