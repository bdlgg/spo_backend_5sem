using System.Reflection;
using backend_spo.Services;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace backend_spo.Tests.Services;

public class RedisTokenStoreServiceTests
{
    private static RedisTokenStoreService CreateService(
        InMemoryRedisState? state = null,
        bool throwOnGetDatabase = false)
    {
        var database = CreateDatabase(state ?? new InMemoryRedisState());
        var redis = CreateConnection(database, throwOnGetDatabase);

        return new RedisTokenStoreService(redis);
    }

    private static RedisTokenStoreService CreateService(
        InMemoryRedisState state,
        IDatabase database)
    {
        var redis = CreateConnection(database);
        return new RedisTokenStoreService(redis);
    }

    private static IDatabase CreateDatabase(InMemoryRedisState state)
    {
        var proxy = DispatchProxy.Create<IDatabase, FakeRedisDatabaseProxy>();
        ((FakeRedisDatabaseProxy)(object)proxy).State = state;

        return proxy;
    }

    private static IConnectionMultiplexer CreateConnection(
        IDatabase database,
        bool throwOnGetDatabase = false)
    {
        var proxy = DispatchProxy.Create<IConnectionMultiplexer, FakeRedisConnectionProxy>();
        var fakeProxy = (FakeRedisConnectionProxy)(object)proxy;

        fakeProxy.Database = database;
        fakeProxy.ThrowOnGetDatabase = throwOnGetDatabase;

        return proxy;
    }

    [Fact]
    public async Task SaveTokenAsync_WithValidToken_SavesTokenWithExpectedKeyValue()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        var userId = 10;
        var token = "access-token";
        var ttl = TimeSpan.FromMinutes(30);

        // Act
        await service.SaveTokenAsync(userId, token, ttl);

        // Assert
        state.Values.Should().ContainKey("auth:token:access-token");
        state.Values["auth:token:access-token"].Should().Be("10");
    }

    [Fact]
    public async Task SaveTokenAsync_WithEmptyToken_SavesTokenWithEmptyTokenKey()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        // Act
        await service.SaveTokenAsync(5, string.Empty, TimeSpan.FromMinutes(10));

        // Assert
        state.Values.Should().ContainKey("auth:token:");
        state.Values["auth:token:"].Should().Be("5");
    }

    [Fact]
    public async Task SaveTokenAsync_WithNullToken_SavesTokenWithEmptyTokenKey()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        // Act
        await service.SaveTokenAsync(7, null!, TimeSpan.FromMinutes(10));

        // Assert
        state.Values.Should().ContainKey("auth:token:");
        state.Values["auth:token:"].Should().Be("7");
    }

    [Fact]
    public async Task SaveTokenAsync_WithZeroTtl_SavesToken()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        // Act
        var act = async () => await service.SaveTokenAsync(1, "token", TimeSpan.Zero);

        // Assert
        await act.Should().NotThrowAsync();

        state.Values.Should().ContainKey("auth:token:token");
        state.Values["auth:token:token"].Should().Be("1");
    }

    [Fact]
    public async Task SaveTokenAsync_WhenRedisGetDatabaseThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(throwOnGetDatabase: true);

        // Act
        var act = async () => await service.SaveTokenAsync(1, "token", TimeSpan.FromMinutes(10));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis database is unavailable");
    }

    [Fact]
    public async Task SaveTokenAsync_WhenRedisStringSetThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var state = new InMemoryRedisState
        {
            StringSetException = new InvalidOperationException("Redis StringSet failed")
        };

        var service = CreateService(state);

        // Act
        var act = async () => await service.SaveTokenAsync(1, "token", TimeSpan.FromMinutes(10));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis StringSet failed");
    }

    [Fact]
    public async Task IsTokenActiveAsync_WhenTokenExists_ReturnsTrue()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        await service.SaveTokenAsync(1, "active-token", TimeSpan.FromMinutes(10));

        // Act
        var result = await service.IsTokenActiveAsync("active-token");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenActiveAsync_WhenTokenDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        // Act
        var result = await service.IsTokenActiveAsync("missing-token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenActiveAsync_WithEmptyToken_WhenTokenExists_ReturnsTrue()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        await service.SaveTokenAsync(1, string.Empty, TimeSpan.FromMinutes(10));

        // Act
        var result = await service.IsTokenActiveAsync(string.Empty);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenActiveAsync_WithNullToken_WhenTokenDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        // Act
        var result = await service.IsTokenActiveAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenActiveAsync_WithNullToken_WhenTokenExists_ReturnsTrue()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        await service.SaveTokenAsync(1, null!, TimeSpan.FromMinutes(10));

        // Act
        var result = await service.IsTokenActiveAsync(null!);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenActiveAsync_WhenRedisGetDatabaseThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(throwOnGetDatabase: true);

        // Act
        var act = async () => await service.IsTokenActiveAsync("token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis database is unavailable");
    }

    [Fact]
    public async Task IsTokenActiveAsync_WhenRedisKeyExistsThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var state = new InMemoryRedisState
        {
            KeyExistsException = new InvalidOperationException("Redis KeyExists failed")
        };

        var service = CreateService(state);

        // Act
        var act = async () => await service.IsTokenActiveAsync("token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis KeyExists failed");
    }

    [Fact]
    public async Task RemoveTokenAsync_WhenTokenExists_RemovesToken()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        await service.SaveTokenAsync(1, "token-to-remove", TimeSpan.FromMinutes(10));

        // Act
        await service.RemoveTokenAsync("token-to-remove");

        // Assert
        state.Values.Should().NotContainKey("auth:token:token-to-remove");

        var isActive = await service.IsTokenActiveAsync("token-to-remove");
        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveTokenAsync_WhenTokenDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        // Act
        var act = async () => await service.RemoveTokenAsync("missing-token");

        // Assert
        await act.Should().NotThrowAsync();
        state.Values.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveTokenAsync_WithEmptyToken_RemovesEmptyTokenKey()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        await service.SaveTokenAsync(1, string.Empty, TimeSpan.FromMinutes(10));

        // Act
        await service.RemoveTokenAsync(string.Empty);

        // Assert
        state.Values.Should().NotContainKey("auth:token:");

        var isActive = await service.IsTokenActiveAsync(string.Empty);
        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveTokenAsync_WithNullToken_RemovesEmptyTokenKey()
    {
        // Arrange
        var state = new InMemoryRedisState();
        var service = CreateService(state);

        await service.SaveTokenAsync(1, null!, TimeSpan.FromMinutes(10));

        // Act
        await service.RemoveTokenAsync(null!);

        // Assert
        state.Values.Should().NotContainKey("auth:token:");

        var isActive = await service.IsTokenActiveAsync(null!);
        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveTokenAsync_WhenRedisGetDatabaseThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(throwOnGetDatabase: true);

        // Act
        var act = async () => await service.RemoveTokenAsync("token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis database is unavailable");
    }

    [Fact]
    public async Task RemoveTokenAsync_WhenRedisKeyDeleteThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var state = new InMemoryRedisState
        {
            KeyDeleteException = new InvalidOperationException("Redis KeyDelete failed")
        };

        var service = CreateService(state);

        // Act
        var act = async () => await service.RemoveTokenAsync("token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis KeyDelete failed");
    }

    [Fact]
    public async Task ServiceMethod_WithNullRedis_ThrowsNullReferenceException()
    {
        // Arrange
        var service = new RedisTokenStoreService(null!);

        // Act
        var act = async () => await service.IsTokenActiveAsync("token");

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    public class InMemoryRedisState
    {
        public Dictionary<string, string> Values { get; } = new();
        public Dictionary<string, TimeSpan?> Expiries { get; } = new();

        public Exception? StringSetException { get; init; }
        public Exception? KeyExistsException { get; init; }
        public Exception? KeyDeleteException { get; init; }
    }

    public class FakeRedisConnectionProxy : DispatchProxy
    {
        public IDatabase Database { get; set; } = null!;
        public bool ThrowOnGetDatabase { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IConnectionMultiplexer.GetDatabase))
            {
                if (ThrowOnGetDatabase)
                {
                    throw new InvalidOperationException("Redis database is unavailable");
                }

                return Database;
            }

            if (targetMethod?.Name == nameof(ToString))
            {
                return nameof(FakeRedisConnectionProxy);
            }

            throw new NotSupportedException($"Method {targetMethod?.Name} is not supported by fake Redis connection.");
        }
    }

    public class FakeRedisDatabaseProxy : DispatchProxy
    {
        public InMemoryRedisState State { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                nameof(IDatabase.StringSetAsync) => StringSetAsync(args),
                nameof(IDatabase.KeyExistsAsync) => KeyExistsAsync(args),
                nameof(IDatabase.KeyDeleteAsync) => KeyDeleteAsync(args),
                nameof(ToString) => nameof(FakeRedisDatabaseProxy),
                _ => throw new NotSupportedException($"Method {targetMethod?.Name} is not supported by fake Redis database.")
            };
        }

        private Task<bool> StringSetAsync(object?[]? args)
        {
            if (State.StringSetException is not null)
            {
                throw State.StringSetException;
            }

            var key = GetRedisKey(args);
            var value = GetRedisValue(args);
            var expiry = GetExpiry(args);

            State.Values[key] = value;
            State.Expiries[key] = expiry;

            return Task.FromResult(true);
        }

        private Task<bool> KeyExistsAsync(object?[]? args)
        {
            if (State.KeyExistsException is not null)
            {
                throw State.KeyExistsException;
            }

            var key = GetRedisKey(args);

            return Task.FromResult(State.Values.ContainsKey(key));
        }

        private Task<bool> KeyDeleteAsync(object?[]? args)
        {
            if (State.KeyDeleteException is not null)
            {
                throw State.KeyDeleteException;
            }

            var key = GetRedisKey(args);
            var removed = State.Values.Remove(key);

            State.Expiries.Remove(key);

            return Task.FromResult(removed);
        }

        private static string GetRedisKey(object?[]? args)
        {
            return args?[0] is RedisKey redisKey
                ? redisKey.ToString()
                : args?[0]?.ToString() ?? string.Empty;
        }

        private static string GetRedisValue(object?[]? args)
        {
            return args?[1] is RedisValue redisValue
                ? redisValue.ToString()
                : args?[1]?.ToString() ?? string.Empty;
        }

        private static TimeSpan? GetExpiry(object?[]? args)
        {
            if (args is null)
            {
                return null;
            }

            foreach (var arg in args)
            {
                if (arg is TimeSpan expiry)
                {
                    return expiry;
                }
            }

            return null;
        }
    }
}