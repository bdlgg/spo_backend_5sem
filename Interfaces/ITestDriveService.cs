using backend_spo.Entities;

namespace backend_spo.Interfaces
{
    public interface ITestDriveService
    {
        Task<IEnumerable<TestDrive>> GetAllAsync();
        Task<TestDrive?> GetByIdAsync(int id);
        Task<IEnumerable<TestDrive>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<TestDrive>> GetByManagerIdAsync(int managerId);

        Task<(bool Success, string Message, TestDrive? testDrive)> CreateAsync(TestDrive testDrive, int currentUserId);

        Task<(bool Success, string Message)> CompleteAsync(int id, string result, string comment, int currentUserId);

        Task<(bool Success, string Message)> CancelAsync(int id, string comment, int currentUserId);
    }
}