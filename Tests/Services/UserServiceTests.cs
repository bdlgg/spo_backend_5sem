using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Interfaces;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend_spo.Tests.Services;

public class UserServiceTests
{
    private static ApplicationDbContext CreateContext(bool seedRoles = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        if (seedRoles)
        {
            context.Roles.AddRange(
                new Role
                {
                    Id = 1,
                    Name = "Админ",
                    Description = "Администратор"
                },
                new Role
                {
                    Id = 2,
                    Name = "Клиент",
                    Description = "Клиент"
                },
                new Role
                {
                    Id = 3,
                    Name = "Менеджер",
                    Description = "Менеджер"
                }
            );

            context.SaveChanges();
        }

        return context;
    }

    private static UserService CreateService(
        ApplicationDbContext context,
        IAuditService? auditService = null)
    {
        return new UserService(
            new UserRepository(context),
            auditService ?? new AuditService(new AuditRepository(context)));
    }

    private static User CreateUser(
        int id = 0,
        string firstName = "Иван",
        string lastName = "Иванов",
        string email = "user@test.com",
        int roleId = 2,
        bool isActive = true,
        DateTime? createdAt = null)
    {
        return new User
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = "hash",
            RoleId = roleId,
            IsActive = isActive,
            CreatedAt = createdAt ?? new DateTime(2026, 5, 1)
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenUsersExist_ReturnsAllUsersWithRoles()
    {
        // Arrange
        await using var context = CreateContext();

        context.Users.AddRange(
            CreateUser(id: 1, email: "admin@test.com", roleId: 1),
            CreateUser(id: 2, email: "client@test.com", roleId: 2),
            CreateUser(id: 3, email: "manager@test.com", roleId: 3)
        );

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(user => user.Role != null);

        result.Select(user => user.Email).Should().Contain("admin@test.com");
        result.Select(user => user.Email).Should().Contain("client@test.com");
        result.Select(user => user.Email).Should().Contain("manager@test.com");

        result.First(user => user.Email == "admin@test.com").Role!.Name.Should().Be("Админ");
        result.First(user => user.Email == "client@test.com").Role!.Name.Should().Be("Клиент");
        result.First(user => user.Email == "manager@test.com").Role!.Name.Should().Be("Менеджер");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoUsersExist_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUserWithRole()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            firstName: "Алексей",
            lastName: "Скворцов",
            email: "alex@test.com",
            roleId: 2);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FirstName.Should().Be("Алексей");
        result.LastName.Should().Be("Скворцов");
        result.Email.Should().Be("alex@test.com");
        result.Role.Should().NotBeNull();
        result.Role!.Name.Should().Be("Клиент");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var zeroIdResult = await service.GetByIdAsync(0);
        var negativeIdResult = await service.GetByIdAsync(-1);

        // Assert
        zeroIdResult.Should().BeNull();
        negativeIdResult.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserAndRoleExist_UpdatesRoleAndCreatesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "client@test.com",
            roleId: 2);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserRoleAsync(user.Id, "Менеджер");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Роль пользователя успешно обновлена");

        var userFromDb = await context.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.Id == user.Id);

        userFromDb.RoleId.Should().Be(3);
        userFromDb.Role.Should().NotBeNull();
        userFromDb.Role!.Name.Should().Be("Менеджер");

        var auditLog = await context.AuditLogs.SingleAsync();

        auditLog.UserId.Should().BeNull();
        auditLog.ActionType.Should().Be("USER_ROLE_UPDATE");
        auditLog.EntityName.Should().Be("User");
        auditLog.EntityId.Should().Be(user.Id);
        auditLog.Result.Should().Be("Success");
        auditLog.Details.Should().Contain("client@test.com");
        auditLog.Details.Should().Contain("Менеджер");
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserDoesNotExist_ReturnsFalseAndDoesNotCreateAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserRoleAsync(999, "Менеджер");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Пользователь не найден");

        var auditLogsCount = await context.AuditLogs.CountAsync();
        auditLogsCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenRoleDoesNotExist_ReturnsFalseAndDoesNotChangeUser()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "client@test.com",
            roleId: 2);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserRoleAsync(user.Id, "Несуществующая роль");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Роль не найдена");

        var userFromDb = await context.Users.FirstAsync(u => u.Id == user.Id);
        userFromDb.RoleId.Should().Be(2);

        var auditLogsCount = await context.AuditLogs.CountAsync();
        auditLogsCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WithNullRoleName_ReturnsFalseAndRoleNotFoundMessage()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "client@test.com",
            roleId: 2);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserRoleAsync(user.Id, null!);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Роль не найдена");

        var userFromDb = await context.Users.FirstAsync(u => u.Id == user.Id);
        userFromDb.RoleId.Should().Be(2);

        var auditLogsCount = await context.AuditLogs.CountAsync();
        auditLogsCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WithEmptyRoleName_ReturnsFalseAndRoleNotFoundMessage()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "client@test.com",
            roleId: 2);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserRoleAsync(user.Id, string.Empty);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Роль не найдена");

        var userFromDb = await context.Users.FirstAsync(u => u.Id == user.Id);
        userFromDb.RoleId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WhenUserExistsAndIsBlocked_ActivatesUserAndCreatesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "blocked@test.com",
            roleId: 2,
            isActive: false);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserStatusAsync(user.Id, true);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Пользователь успешно разблокирован");

        var userFromDb = await context.Users.FirstAsync(u => u.Id == user.Id);
        userFromDb.IsActive.Should().BeTrue();

        var auditLog = await context.AuditLogs.SingleAsync();

        auditLog.UserId.Should().BeNull();
        auditLog.ActionType.Should().Be("USER_STATUS_UPDATE");
        auditLog.EntityName.Should().Be("User");
        auditLog.EntityId.Should().Be(user.Id);
        auditLog.Result.Should().Be("Success");
        auditLog.Details.Should().Contain("blocked@test.com");
        auditLog.Details.Should().Contain("разблокирован");
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WhenUserExistsAndIsActive_BlocksUserAndCreatesAuditLog()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "active@test.com",
            roleId: 2,
            isActive: true);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserStatusAsync(user.Id, false);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Пользователь успешно заблокирован");

        var userFromDb = await context.Users.FirstAsync(u => u.Id == user.Id);
        userFromDb.IsActive.Should().BeFalse();

        var auditLog = await context.AuditLogs.SingleAsync();

        auditLog.UserId.Should().BeNull();
        auditLog.ActionType.Should().Be("USER_STATUS_UPDATE");
        auditLog.EntityName.Should().Be("User");
        auditLog.EntityId.Should().Be(user.Id);
        auditLog.Result.Should().Be("Success");
        auditLog.Details.Should().Contain("active@test.com");
        auditLog.Details.Should().Contain("заблокирован");
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WhenUserDoesNotExist_ReturnsFalseAndDoesNotCreateAuditLog()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateUserStatusAsync(999, false);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Пользователь не найден");

        var auditLogsCount = await context.AuditLogs.CountAsync();
        auditLogsCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WhenAuditServiceThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "user@test.com",
            roleId: 2,
            isActive: true);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context, new ThrowingAuditService());

        // Act
        var act = async () => await service.UpdateUserStatusAsync(user.Id, false);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit failed");
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenAuditServiceThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = CreateContext();

        var user = CreateUser(
            id: 1,
            email: "user@test.com",
            roleId: 2);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context, new ThrowingAuditService());

        // Act
        var act = async () => await service.UpdateUserRoleAsync(user.Id, "Менеджер");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit failed");
    }

    private class ThrowingAuditService : IAuditService
    {
        public Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<AuditLog>>(new List<AuditLog>());
        }

        public Task<AuditLog?> GetByIdAsync(int id)
        {
            return Task.FromResult<AuditLog?>(null);
        }

        public Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
        {
            return Task.FromResult<IEnumerable<AuditLog>>(new List<AuditLog>());
        }

        public Task LogAsync(
            int? userId,
            string actionType,
            string entityName,
            int? entityId,
            string result,
            string details)
        {
            throw new InvalidOperationException("Audit failed");
        }
    }
}