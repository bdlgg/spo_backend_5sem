using backend_spo.Interfaces;

namespace backend_spo.Tests.Infrastructure;

public class FakeTokenStoreService : ITokenStoreService
{
    public Task SaveTokenAsync(int userId, string token, TimeSpan ttl)
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenActiveAsync(string token)
    {
        return Task.FromResult(true);
    }

    public Task RemoveTokenAsync(string token)
    {
        return Task.CompletedTask;
    }
}