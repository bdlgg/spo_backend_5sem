using backend_spo.Interfaces;
using StackExchange.Redis;

namespace backend_spo.Services;

public class RedisTokenStoreService : ITokenStoreService
{
    private readonly IConnectionMultiplexer _redis;
    public RedisTokenStoreService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SaveTokenAsync(int userId, string token, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        var tokenKey = GetTokenKey(token);
        await db.StringSetAsync(tokenKey, userId.ToString(), ttl);
    }

    public async Task<bool> IsTokenActiveAsync(string token)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(GetTokenKey(token));
    }

    public async Task RemoveTokenAsync(string token)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(GetTokenKey(token));
    }

    private static string GetTokenKey(string token) => $"auth:token:{token}";
}