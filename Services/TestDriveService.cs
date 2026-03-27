using backend_spo.Entities;
using backend_spo.Interfaces;

namespace backend_spo.Services
{
    public class TestDriveService : ITestDriveService
    {
        private readonly ITestDriveRepository _testDriveRepository;
        private readonly ICarRepository _carRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IAuditService _auditService;

        public TestDriveService(
            ITestDriveRepository testDriveRepository,
            ICarRepository carRepository,
            IClientRepository clientRepository,
            IAuditService auditService)
        {
            _testDriveRepository = testDriveRepository;
            _carRepository = carRepository;
            _clientRepository = clientRepository;
            _auditService = auditService;
        }

        public async Task<IEnumerable<TestDrive>> GetAllAsync()
        {
            return await _testDriveRepository.GetAllAsync();
        }

        public async Task<TestDrive?> GetByIdAsync(int id)
        {
            return await _testDriveRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TestDrive>> GetByClientIdAsync(int clientId)
        {
            return await _testDriveRepository.GetByClientIdAsync(clientId);
        }

        public async Task<IEnumerable<TestDrive>> GetByManagerIdAsync(int managerId)
        {
            return await _testDriveRepository.GetByManagerIdAsync(managerId);
        }

        public async Task<(bool Success, string Message, TestDrive? testDrive)> CreateAsync(TestDrive testDrive, int currentUserId)
        {
            var car = await _carRepository.GetByIdAsync(testDrive.CarId);
            if (car == null)
            {
                await _auditService.LogAsync(
                    currentUserId,
                    "TESTDRIVE_CREATE",
                    "TestDrive",
                    null,
                    "Failed",
                    $"Попытка создать тест-драйв для несуществующего автомобиля Id={testDrive.CarId}"
                );
                return (false, "Автомобиль не найден", null);
            }

            var client = await _clientRepository.GetByIdAsync(testDrive.ClientId);
            if (client == null)
            {
                await _auditService.LogAsync(
                    currentUserId,
                    "TESTDRIVE_CREATE",
                    "TestDrive",
                    null,
                    "Failed",
                    $"Попытка создать тест-драйв для несуществующего клиента Id={testDrive.ClientId}"
                );
                return (false, "Клиент не найден", null);
            }

            if (!car.IsActive)
                return (false, "Автомобиль неактивен", null);

            if (car.Status == "Sold")
                return (false, "Нельзя записать на тест-драйв проданный автомобиль", null);

            if (testDrive.ScheduledAt <= DateTime.UtcNow)
                return (false, "Дата тест-драйва должна быть в будущем", null);

            var hasConflict = await _testDriveRepository.HasConflictAsync(testDrive.CarId, testDrive.ScheduledAt);
            if (hasConflict)
            {
                await _auditService.LogAsync(
                    currentUserId,
                    "TESTDRIVE_CREATE",
                    "TestDrive",
                    null,
                    "Failed",
                    $"Конфликт расписания для автомобиля Id={testDrive.CarId} на {testDrive.ScheduledAt:u}"
                );
                return (false, "На это время уже запланирован тест-драйв для данного автомобиля", null);
            }

            testDrive.ManagerId = currentUserId;
            testDrive.Status = "Scheduled";
            testDrive.CreatedAt = DateTime.UtcNow;

            await _testDriveRepository.AddAsync(testDrive);

            await _auditService.LogAsync(
                currentUserId,
                "TESTDRIVE_CREATE",
                "TestDrive",
                testDrive.Id,
                "Success",
                $"Создан тест-драйв для автомобиля Id={testDrive.CarId} и клиента Id={testDrive.ClientId}"
            );

            return (true, "Тест-драйв успешно создан", testDrive);
        }

        public async Task<(bool Success, string Message)> CompleteAsync(int id, string result, string comment, int currentUserId)
        {
            var testDrive = await _testDriveRepository.GetByIdAsync(id);
            if (testDrive == null)
                return (false, "Тест-драйв не найден");

            if (testDrive.Status != "Scheduled")
                return (false, "Завершить можно только тест-драйв со статусом Scheduled");

            testDrive.Status = "Completed";
            testDrive.Result = result;
            testDrive.Comment = comment;

            await _testDriveRepository.UpdateAsync(testDrive);

            await _auditService.LogAsync(
                currentUserId,
                "TESTDRIVE_COMPLETE",
                "TestDrive",
                testDrive.Id,
                "Success",
                $"Завершен тест-драйв Id={testDrive.Id}, результат: {result}"
            );

            return (true, "Тест-драйв успешно завершен");
        }

        public async Task<(bool Success, string Message)> CancelAsync(int id, string comment, int currentUserId)
        {
            var testDrive = await _testDriveRepository.GetByIdAsync(id);
            if (testDrive == null)
                return (false, "Тест-драйв не найден");

            if (testDrive.Status != "Scheduled")
                return (false, "Отменить можно только тест-драйв со статусом Scheduled");

            testDrive.Status = "Cancelled";
            testDrive.Comment = comment;
            testDrive.Result = string.Empty;

            await _testDriveRepository.UpdateAsync(testDrive);

            await _auditService.LogAsync(
                currentUserId,
                "TESTDRIVE_CANCEL",
                "TestDrive",
                testDrive.Id,
                "Success",
                $"Отменен тест-драйв Id={testDrive.Id}"
            );

            return (true, "Тест-драйв успешно отменен");
        }
    }
}