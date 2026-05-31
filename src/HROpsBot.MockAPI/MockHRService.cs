using HROpsBot.Domain.Entities;

namespace HROpsBot.MockAPI;

/// <summary>Mock HR-системы (имитация 1С/SAP HR)</summary>
public class MockHRService
{
    private static readonly List<Employee> _employees =
    [
        new() {
            Id = 1, TelegramId = 479526836,
            NameRu = "Алия Бекова", NameKk = "Әлия Бекова",
            Department = "Разработка", Position = "Старший разработчик",
            Email = "a.bekova@company.kz", HiredAt = new DateTime(2021, 3, 15),
            VacationDaysTotal = 28, VacationDaysUsed = 10, IsHrAdmin = false
        },
        new() {
            Id = 2, TelegramId = 100000002,
            NameRu = "Максим Иванов", NameKk = "Максим Иванов",
            Department = "Маркетинг", Position = "Менеджер по маркетингу",
            Email = "m.ivanov@company.kz", HiredAt = new DateTime(2022, 6, 1),
            VacationDaysTotal = 28, VacationDaysUsed = 5, IsHrAdmin = false
        },
        new() {
            Id = 3, TelegramId = 100000003,
            NameRu = "Динара Сейткали", NameKk = "Динара Сейтқали",
            Department = "HR", Position = "HR-менеджер",
            Email = "d.seitkali@company.kz", HiredAt = new DateTime(2020, 1, 10),
            VacationDaysTotal = 28, VacationDaysUsed = 14, IsHrAdmin = true
        }
    ];

    private static readonly List<VacationRequest> _vacations =
    [
        new() {
            Id = 1, EmployeeId = 1,
            StartDate = new DateTime(2026, 7, 15),
            EndDate = new DateTime(2026, 7, 29),
            Status = VacationStatus.Approved
        },
        new() {
            Id = 2, EmployeeId = 2,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 14),
            Status = VacationStatus.Pending
        }
    ];

    private static readonly List<HrAppointment> _appointments = [];
    private static int _nextAppointmentId = 1;

    // --- Сотрудники ---

    public Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId) =>
        Task.FromResult(_employees.FirstOrDefault(e => e.TelegramId == telegramId));

    public Task<Employee?> GetEmployeeByIdAsync(int id) =>
        Task.FromResult(_employees.FirstOrDefault(e => e.Id == id));

    // --- Отпуска ---

    public Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId)
    {
        var emp = _employees.FirstOrDefault(e => e.Id == employeeId);
        if (emp == null) return Task.FromResult((0, 0, 0));
        return Task.FromResult((emp.VacationDaysTotal, emp.VacationDaysUsed, emp.VacationDaysRemaining));
    }

    public Task<VacationRequest?> GetNextVacationAsync(int employeeId)
    {
        var next = _vacations
            .Where(v => v.EmployeeId == employeeId && v.StartDate > DateTime.UtcNow)
            .OrderBy(v => v.StartDate)
            .FirstOrDefault();
        return Task.FromResult(next);
    }

    // --- Справки ---

    public Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod)
    {
        var req = new CertificateRequest
        {
            Id = Random.Shared.Next(1000, 9999),
            EmployeeId = employeeId,
            Type = type,
            Status = RequestStatus.Pending,
            DeliveryMethod = deliveryMethod,
            CreatedAt = DateTime.UtcNow
        };
        return Task.FromResult(req);
    }

    // --- Запись к HR ---

    public Task<List<DateTime>> GetAvailableSlotsAsync()
    {
        var slots = new List<DateTime>();
        var now = DateTime.UtcNow.Date.AddHours(9);
        for (int d = 1; d <= 3; d++)
        {
            var day = now.AddDays(d);
            if (day.DayOfWeek == DayOfWeek.Saturday) day = day.AddDays(2);
            if (day.DayOfWeek == DayOfWeek.Sunday) day = day.AddDays(1);
            slots.Add(day.AddHours(0));
            slots.Add(day.AddHours(2));
            slots.Add(day.AddHours(4));
        }
        return Task.FromResult(slots);
    }

    public Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot)
    {
        var appt = new HrAppointment
        {
            Id = _nextAppointmentId++,
            EmployeeId = employeeId,
            SlotDateTime = slot,
            HrManagerNameRu = "Динара Сейткали",
            HrManagerNameKk = "Динара Сейтқали",
            Status = AppointmentStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        _appointments.Add(appt);
        return Task.FromResult(appt);
    }
}
