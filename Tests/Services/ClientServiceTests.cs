using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Repositories;
using backend_spo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Services;

public class ClientServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        context.Roles.AddRange(
            new Role
            {
                Id = 1,
                Name = "Клиент",
                Description = "Client role"
            },
            new Role
            {
                Id = 2,
                Name = "Менеджер",
                Description = "Manager role"
            }
        );

        context.Users.AddRange(
            new User
            {
                Id = 1,
                FirstName = "Иван",
                LastName = "Клиентов",
                Email = "client-user@test.com",
                PasswordHash = "hash",
                RoleId = 1,
                IsActive = true
            },
            new User
            {
                Id = 2,
                FirstName = "Петр",
                LastName = "Менеджеров",
                Email = "manager-user@test.com",
                PasswordHash = "hash",
                RoleId = 2,
                IsActive = true
            }
        );

        context.SaveChanges();

        return context;
    }

    private static ClientService CreateService(ApplicationDbContext context)
    {
        return new ClientService(new ClientRepository(context));
    }

    private static Client CreateClient(
        int id = 0,
        string firstName = "Алексей",
        string lastName = "Скворцов",
        string phone = "+79991112233",
        string email = "client@test.com",
        int? userId = null)
    {
        return new Client
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
            UserId = userId
        };
    }

    [Fact]
    public async Task GetAllAsync_WhenClientsExist_ReturnsClientsWithoutUserOrWithClientRole()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", email: "client1@test.com"),
            CreateClient(id: 2, firstName: "Иван", lastName: "Иванов", email: "client2@test.com", userId: 1),
            CreateClient(id: 3, firstName: "Петр", lastName: "Петров", email: "manager-client@test.com", userId: 2)
        );

        await context.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(client => client.FirstName == "Алексей" && client.UserId == null);
        result.Should().Contain(client => client.FirstName == "Иван" && client.User != null && client.User.Role!.Name == "Клиент");
        result.Should().NotContain(client => client.FirstName == "Петр");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoClientsExist_ReturnsEmptyList()
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
    public async Task GetAllAsync_WhenOnlyManagerLinkedClientsExist_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        context.Clients.Add(CreateClient(
            id: 1,
            firstName: "Петр",
            lastName: "Петров",
            email: "manager-client@test.com",
            userId: 2));

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenClientExists_ReturnsClientWithUserAndRole()
    {
        // Arrange
        await using var context = CreateContext();

        var client = CreateClient(
            id: 1,
            firstName: "Иван",
            lastName: "Иванов",
            email: "client@test.com",
            userId: 1);

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(client.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(client.Id);
        result.FirstName.Should().Be("Иван");
        result.User.Should().NotBeNull();
        result.User!.Role.Should().NotBeNull();
        result.User.Role!.Name.Should().Be("Клиент");
    }

    [Fact]
    public async Task GetByIdAsync_WhenClientDoesNotExist_ReturnsNull()
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
    public async Task GetByIdAsync_WhenClientIsLinkedToManagerUser_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();

        context.Clients.Add(CreateClient(
            id: 1,
            firstName: "Петр",
            lastName: "Петров",
            email: "manager-client@test.com",
            userId: 2));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidClient_SavesAndReturnsClient()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var client = CreateClient(
            firstName: "Мария",
            lastName: "Сидорова",
            phone: "+79990001122",
            email: "maria@test.com");

        // Act
        var result = await service.CreateAsync(client);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.FirstName.Should().Be("Мария");
        result.LastName.Should().Be("Сидорова");

        var clientFromDb = await context.Clients.FirstOrDefaultAsync(c => c.Id == result.Id);

        clientFromDb.Should().NotBeNull();
        clientFromDb!.Phone.Should().Be("+79990001122");
        clientFromDb.Email.Should().Be("maria@test.com");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFields_SavesClientBecauseServiceDoesNotValidateClient()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var client = CreateClient(
            firstName: "",
            lastName: "",
            phone: "",
            email: "");

        // Act
        var result = await service.CreateAsync(client);

        // Assert
        result.Id.Should().BeGreaterThan(0);

        var clientFromDb = await context.Clients.FirstOrDefaultAsync(c => c.Id == result.Id);

        clientFromDb.Should().NotBeNull();
        clientFromDb!.FirstName.Should().BeEmpty();
        clientFromDb.LastName.Should().BeEmpty();
        clientFromDb.Phone.Should().BeEmpty();
        clientFromDb.Email.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithNullClient_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var act = async () => await service.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenClientExists_UpdatesClientAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();

        var client = CreateClient(
            id: 1,
            firstName: "Алексей",
            lastName: "Скворцов",
            phone: "+79991112233",
            email: "old@test.com");

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedClient = CreateClient(
            firstName: "Иван",
            lastName: "Иванов",
            phone: "+79994445566",
            email: "new@test.com");

        // Act
        var result = await service.UpdateAsync(client.Id, updatedClient);

        // Assert
        result.Should().BeTrue();

        var clientFromDb = await context.Clients.FirstAsync(c => c.Id == client.Id);

        clientFromDb.FirstName.Should().Be("Иван");
        clientFromDb.LastName.Should().Be("Иванов");
        clientFromDb.Phone.Should().Be("+79994445566");
        clientFromDb.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdateAsync_WhenClientDoesNotExist_ReturnsFalseAndDoesNotCreateClient()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        var updatedClient = CreateClient(
            firstName: "Иван",
            lastName: "Иванов",
            phone: "+79994445566",
            email: "new@test.com");

        // Act
        var result = await service.UpdateAsync(999, updatedClient);

        // Assert
        result.Should().BeFalse();

        var clients = await context.Clients.ToListAsync();
        clients.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WhenClientIsLinkedToManagerUser_ReturnsFalseAndDoesNotUpdateClient()
    {
        // Arrange
        await using var context = CreateContext();

        var managerLinkedClient = CreateClient(
            id: 1,
            firstName: "Петр",
            lastName: "Петров",
            phone: "+79990000000",
            email: "manager-client@test.com",
            userId: 2);

        context.Clients.Add(managerLinkedClient);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updatedClient = CreateClient(
            firstName: "Новое",
            lastName: "Имя",
            phone: "+79994445566",
            email: "new@test.com");

        // Act
        var result = await service.UpdateAsync(managerLinkedClient.Id, updatedClient);

        // Assert
        result.Should().BeFalse();

        var clientFromDb = await context.Clients.FirstAsync(c => c.Id == managerLinkedClient.Id);
        clientFromDb.FirstName.Should().Be("Петр");
        clientFromDb.LastName.Should().Be("Петров");
        clientFromDb.Email.Should().Be("manager-client@test.com");
    }

    [Fact]
    public async Task UpdateAsync_WithNullClient_WhenExistingClientFound_ThrowsNullReferenceException()
    {
        // Arrange
        await using var context = CreateContext();

        var client = CreateClient(id: 1);
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var act = async () => await service.UpdateAsync(client.Id, null!);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();

        var clientFromDb = await context.Clients.FirstAsync(c => c.Id == client.Id);
        clientFromDb.FirstName.Should().Be("Алексей");
    }

    [Fact]
    public async Task UpdateAsync_WithNullClient_WhenClientDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateAsync(999, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenClientExists_RemovesClientAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();

        var client = CreateClient(id: 1);
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(client.Id);

        // Assert
        result.Should().BeTrue();

        var clientFromDb = await context.Clients.FindAsync(client.Id);
        clientFromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenClientDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenClientIsLinkedToManagerUser_ReturnsFalseAndDoesNotDeleteClient()
    {
        // Arrange
        await using var context = CreateContext();

        var client = CreateClient(
            id: 1,
            firstName: "Петр",
            lastName: "Петров",
            email: "manager-client@test.com",
            userId: 2);

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(client.Id);

        // Assert
        result.Should().BeFalse();

        var clientFromDb = await context.Clients.FindAsync(client.Id);
        clientFromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_WithFirstNameLastNameAndPhone_ReturnsMatchingClients()
    {
        // Arrange
        await using var context = CreateContext();

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", phone: "+79991112233", email: "client1@test.com"),
            CreateClient(id: 2, firstName: "Алексей", lastName: "Петров", phone: "+79994445566", email: "client2@test.com"),
            CreateClient(id: 3, firstName: "Иван", lastName: "Скворцов", phone: "+79991112233", email: "client3@test.com")
        );

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = (await service.SearchAsync("Алексей", "Скворцов", "111")).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].FirstName.Should().Be("Алексей");
        result[0].LastName.Should().Be("Скворцов");
        result[0].Phone.Should().Be("+79991112233");
    }

    [Fact]
    public async Task SearchAsync_WithNullFilters_ReturnsAllClients()
    {
        // Arrange
        await using var context = CreateContext();

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", email: "client1@test.com"),
            CreateClient(id: 2, firstName: "Иван", lastName: "Иванов", email: "client2@test.com")
        );

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SearchAsync(null, null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyAndWhitespaceFilters_ReturnsAllClients()
    {
        // Arrange
        await using var context = CreateContext();

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", email: "client1@test.com"),
            CreateClient(id: 2, firstName: "Иван", lastName: "Иванов", email: "client2@test.com")
        );

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SearchAsync("", " ", null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WhenNoClientsMatch_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();

        context.Clients.Add(CreateClient(
            id: 1,
            firstName: "Алексей",
            lastName: "Скворцов",
            phone: "+79991112233"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SearchAsync("Мария", null, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_DoesNotFilterByUserRoleAndReturnsManagerLinkedClientToo()
    {
        // Arrange
        await using var context = CreateContext();

        context.Clients.AddRange(
            CreateClient(id: 1, firstName: "Алексей", lastName: "Скворцов", email: "client@test.com", userId: 1),
            CreateClient(id: 2, firstName: "Петр", lastName: "Петров", email: "manager-client@test.com", userId: 2)
        );

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = (await service.SearchAsync(null, null, null)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(client => client.Email == "client@test.com");
        result.Should().Contain(client => client.Email == "manager-client@test.com");
    }

    [Fact]
    public async Task GetAllAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.GetAllAsync();

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SearchAsync_WhenContextIsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var context = CreateContext();
        var service = CreateService(context);
        await context.DisposeAsync();

        // Act
        var act = async () => await service.SearchAsync(null, null, null);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}