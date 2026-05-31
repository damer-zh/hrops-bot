using HROpsBot.Domain.Entities;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HROpsBot.Core.Interfaces;

namespace HROpsBot.Infrastructure.Services;

public class HrService(AppDbContext dbContext) : IHrService
{
    // --- Сотрудники ---

    public async Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId) =>
        await dbContext.Employees.FirstOrDefaultAsync(e => e.TelegramId == telegramId);

    public async Task<Employee?> GetEmployeeByIdAsync(int id) =>
        await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Employee> CreateOrUpdateEmployeeAsync(long telegramId, string firstName, string? lastName, string? username)
    {
        var emp = await dbContext.Employees.FirstOrDefaultAsync(e => e.TelegramId == telegramId);
        
        var fullName = string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}";

        if (emp == null)
        {
            emp = new Employee
            {
                TelegramId = telegramId,
                NameRu = fullName,
                NameKk = fullName,
                Department = "Новички",
                Position = "Стажер",
                Email = $"{username ?? telegramId.ToString()}@company.kz",
                HiredAt = DateTime.UtcNow,
                VacationDaysTotal = 28,
                VacationDaysUsed = 0,
                IsHrAdmin = false
            };
            dbContext.Employees.Add(emp);
        }
        else
        {
            // Обновляем имя, если оно изменилось в Telegram
            emp.NameRu = fullName;
            emp.NameKk = fullName;
        }

        await dbContext.SaveChangesAsync();
        return emp;
    }

    // --- Отпуска ---

    public async Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId)
    {
        var emp = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp == null) return (0, 0, 0);
        return (emp.VacationDaysTotal, emp.VacationDaysUsed, emp.VacationDaysRemaining);
    }

    public async Task<VacationRequest?> GetNextVacationAsync(int employeeId)
    {
        return await dbContext.VacationRequests
            .Where(v => v.EmployeeId == employeeId && v.StartDate > DateTime.UtcNow && v.Status != VacationStatus.Rejected)
            .OrderBy(v => v.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end)
    {
        var req = new VacationRequest
        {
            EmployeeId = employeeId,
            StartDate = start,
            EndDate = end,
            Status = VacationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.VacationRequests.Add(req);
        await dbContext.SaveChangesAsync();
        return req;
    }

    // --- Справки ---

    public async Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod)
    {
        var req = new CertificateRequest
        {
            EmployeeId = employeeId,
            Type = type,
            Status = RequestStatus.Pending,
            DeliveryMethod = deliveryMethod,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.CertificateRequests.Add(req);
        await dbContext.SaveChangesAsync();
        return req;
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

    public async Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot)
    {
        var appt = new HrAppointment
        {
            EmployeeId = employeeId,
            SlotDateTime = slot,
            HrManagerNameRu = "Динара Сейткали",
            HrManagerNameKk = "Динара Сейтқали",
            Status = AppointmentStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.HrAppointments.Add(appt);
        await dbContext.SaveChangesAsync();
        return appt;
    }
}
