using backend_spo.Entities;

namespace backend_spo.Interfaces
{
    public interface ITestDriveRepository
    {
        Task<IEnumerable<TestDrive>> GetAllAsync();
        Task<TestDrive?> GetByIdAsync(int id);
        Task<IEnumerable<TestDrive>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<TestDrive>> GetByManagerIdAsync(int managerId);
        Task AddAsync(TestDrive testDrive);
        Task UpdateAsync(TestDrive testDrive);
        Task DeleteAsync(TestDrive testDrive);
        Task<bool> HasConflictAsync(int carId, DateTime scheduledAt);
    }
}