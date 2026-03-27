using backend_spo.Data;
using backend_spo.Entities;
using backend_spo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_spo.Repositories
{
    public class TestDriveRepository : ITestDriveRepository
    {
        private readonly ApplicationDbContext _context;

        public TestDriveRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TestDrive>> GetAllAsync()
        {
            return await _context.TestDrives
                .Include(td => td.Car)
                .Include(td => td.Client)
                .Include(td => td.Manager)
                .OrderByDescending(td => td.CreatedAt)
                .ToListAsync();
        }

        public async Task<TestDrive?> GetByIdAsync(int id)
        {
            return await _context.TestDrives
                .Include(td => td.Car)
                .Include(td => td.Client)
                .Include(td => td.Manager)
                .FirstOrDefaultAsync(td => td.Id == id);
        }

        public async Task<IEnumerable<TestDrive>> GetByClientIdAsync(int clientId)
        {
            return await _context.TestDrives
                .Include(td => td.Car)
                .Include(td => td.Client)
                .Include(td => td.Manager)
                .Where(td => td.ClientId == clientId)
                .OrderByDescending(td => td.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TestDrive>> GetByManagerIdAsync(int managerId)
        {
            return await _context.TestDrives
                .Include(td => td.Car)
                .Include(td => td.Client)
                .Include(td => td.Manager)
                .Where(td => td.ManagerId == managerId)
                .OrderByDescending(td => td.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(TestDrive testDrive)
        {
            _context.TestDrives.Add(testDrive);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TestDrive testDrive)
        {
            _context.TestDrives.Update(testDrive);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TestDrive testDrive)
        {
            _context.TestDrives.Remove(testDrive);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasConflictAsync(int carId, DateTime scheduledAt)
        {
            var start = scheduledAt.AddHours(-1);
            var end = scheduledAt.AddHours(1);

            return await _context.TestDrives.AnyAsync(td =>
                td.CarId == carId &&
                td.Status == "Scheduled" &&
                td.ScheduledAt >= start &&
                td.ScheduledAt <= end);
        }
    }
}