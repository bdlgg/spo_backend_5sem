using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class AuditServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static AuditService CreateService(ApplicationDbContext context)
    {
        var repository = new AuditRepository(context);
        return new AuditService(repository);
    }

    [Fact]
    public async Task LogAsync_WithValidData_SavesAuditLog()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.LogAsync(
            userId: 5,
            actionType: "LOGIN",
            entityName: "User",
            entityId: 5,
            result: "Success",
            details: "Юзер вошёл в систему"
        );

        var saved = await context.AuditLogs.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(5);
        saved.ActionType.Should().Be("LOGIN");
        saved.EntityName.Should().Be("User");
        saved.EntityId.Should().Be(5);
        saved.Result.Should().Be("Success");
        saved.Details.Should().Be("Юзер вошёл в систему");
    }

    [Fact]
    public async Task LogAsync_SetsCreatedAtToUtcNow()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var before = DateTime.UtcNow;

        await service.LogAsync(1, "TEST", "Test", null, "Success", "");

        var after = DateTime.UtcNow;
        var saved = await context.AuditLogs.FirstAsync();
        saved.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task LogAsync_WithNullUserId_StoresNull()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.LogAsync(null, "SYSTEM_SEED", "Role", null, "Success", "Сидинг ролей");

        var saved = await context.AuditLogs.FirstAsync();
        saved.UserId.Should().BeNull();
        saved.EntityId.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmpty()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsLogsOrderedByCreatedAtDescending()
    {
        await using var context = CreateContext();
        context.AuditLogs.AddRange(
            new AuditLog { ActionType = "OLD", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new AuditLog { ActionType = "NEW", CreatedAt = DateTime.UtcNow },
            new AuditLog { ActionType = "MID", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = (await service.GetAllAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].ActionType.Should().Be("NEW");
        result[1].ActionType.Should().Be("MID");
        result[2].ActionType.Should().Be("OLD");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsLog()
    {
        await using var context = CreateContext();
        var log = new AuditLog { ActionType = "TEST", EntityName = "User" };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetByIdAsync(log.Id);

        result.Should().NotBeNull();
        result!.ActionType.Should().Be("TEST");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyLogsOfThatUser()
    {
        await using var context = CreateContext();
        context.AuditLogs.AddRange(
            new AuditLog { UserId = 1, ActionType = "A" },
            new AuditLog { UserId = 2, ActionType = "B" },
            new AuditLog { UserId = 1, ActionType = "C" }
        );
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = (await service.GetByUserIdAsync(1)).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.ActionType).Should().BeEquivalentTo(new[] { "A", "C" });
    }

    [Fact]
    public async Task GetByUserIdAsync_NoLogsForUser_ReturnsEmpty()
    {
        await using var context = CreateContext();
        context.AuditLogs.Add(new AuditLog { UserId = 1, ActionType = "A" });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetByUserIdAsync(999);

        result.Should().BeEmpty();
    }
}
