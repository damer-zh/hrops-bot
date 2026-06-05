using HROpsBot.Domain.Entities;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HROpsBot.Core.Interfaces;

namespace HROpsBot.Infrastructure.Services;

public class HrService(AppDbContext dbContext) : IHrService
{
    // ==================== СОТРУДНИКИ ====================

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
                TelegramId      = telegramId,
                NameRu          = fullName,
                NameKk          = fullName,
                Department      = "",
                Position        = "",
                Email           = $"{username ?? telegramId.ToString()}@company.kz",
                HiredAt         = DateTime.UtcNow,
                VacationDaysTotal = 28,
                VacationDaysUsed  = 0,
                IsHrAdmin       = false
            };
            dbContext.Employees.Add(emp);
        }
        else
        {
            emp.NameRu = fullName;
            emp.NameKk = fullName;
        }

        await dbContext.SaveChangesAsync();
        return emp;
    }

    public async Task UpdateEmployeeProfileAsync(int employeeId, string department, string position)
    {
        var emp = await dbContext.Employees.FindAsync(employeeId);
        if (emp != null)
        {
            emp.Department = department;
            emp.Position   = position;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<Employee>> GetAllEmployeesAsync() =>
        await dbContext.Employees
            .OrderByDescending(e => e.HiredAt)
            .ToListAsync();

    // ==================== ОТПУСКА ====================

    public async Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId)
    {
        var emp = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp == null) return (0, 0, 0);
        return (emp.VacationDaysTotal, emp.VacationDaysUsed, emp.VacationDaysRemaining);
    }

    public async Task<VacationRequest?> GetNextVacationAsync(int employeeId) =>
        await dbContext.VacationRequests
            .Where(v => v.EmployeeId == employeeId && v.StartDate > DateTime.UtcNow && v.Status != VacationStatus.Rejected)
            .OrderBy(v => v.StartDate)
            .FirstOrDefaultAsync();

    public async Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end)
    {
        var req = new VacationRequest
        {
            EmployeeId = employeeId,
            StartDate  = start,
            EndDate    = end,
            Status     = VacationStatus.Pending,
            CreatedAt  = DateTime.UtcNow
        };
        dbContext.VacationRequests.Add(req);
        await dbContext.SaveChangesAsync();
        return req;
    }

    public async Task<List<VacationRequest>> GetPendingVacationRequestsAsync() =>
        await dbContext.VacationRequests
            .Include(v => v.Employee)
            .Where(v => v.Status == VacationStatus.Pending)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

    public async Task<List<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId) =>
        await dbContext.VacationRequests
            .Where(v => v.EmployeeId == employeeId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

    public async Task<bool> ApproveVacationAsync(int requestId)
    {
        var req = await dbContext.VacationRequests.Include(v => v.Employee).FirstOrDefaultAsync(v => v.Id == requestId);
        if (req == null) return false;
        req.Status = VacationStatus.Approved;
        // Спишем дни
        if (req.Employee != null)
            req.Employee.VacationDaysUsed += req.DaysCount;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectVacationAsync(int requestId)
    {
        var req = await dbContext.VacationRequests.FindAsync(requestId);
        if (req == null) return false;
        req.Status = VacationStatus.Rejected;
        await dbContext.SaveChangesAsync();
        return true;
    }

    // ==================== СПРАВКИ ====================

    public async Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod)
    {
        var req = new CertificateRequest
        {
            EmployeeId     = employeeId,
            Type           = type,
            Status         = RequestStatus.Pending,
            DeliveryMethod = deliveryMethod,
            CreatedAt      = DateTime.UtcNow
        };
        dbContext.CertificateRequests.Add(req);
        await dbContext.SaveChangesAsync();
        return req;
    }

    public async Task<List<CertificateRequest>> GetPendingCertificateRequestsAsync() =>
        await dbContext.CertificateRequests
            .Include(c => c.Employee)
            .Where(c => c.Status == RequestStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<bool> ApproveCertificateAsync(int requestId)
    {
        var req = await dbContext.CertificateRequests.FindAsync(requestId);
        if (req == null) return false;
        req.Status = RequestStatus.Approved;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectCertificateAsync(int requestId)
    {
        var req = await dbContext.CertificateRequests.FindAsync(requestId);
        if (req == null) return false;
        req.Status = RequestStatus.Rejected;
        await dbContext.SaveChangesAsync();
        return true;
    }

    // ==================== ЗАПИСЬ К HR ====================

    public Task<List<DateTime>> GetAvailableSlotsAsync()
    {
        var slots = new List<DateTime>();
        var now = DateTime.UtcNow.Date.AddHours(9);
        for (int d = 1; d <= 5; d++)
        {
            var day = now.AddDays(d);
            if (day.DayOfWeek == DayOfWeek.Saturday) day = day.AddDays(2);
            if (day.DayOfWeek == DayOfWeek.Sunday) day = day.AddDays(1);
            slots.Add(day);
            slots.Add(day.AddHours(2));
            slots.Add(day.AddHours(4));
        }
        return Task.FromResult(slots);
    }

    public async Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot)
    {
        var appt = new HrAppointment
        {
            EmployeeId        = employeeId,
            SlotDateTime      = slot,
            HrManagerNameRu   = "Динара Сейткали",
            HrManagerNameKk   = "Динара Сейтқали",
            Status            = AppointmentStatus.Scheduled,
            CreatedAt         = DateTime.UtcNow
        };
        dbContext.HrAppointments.Add(appt);
        await dbContext.SaveChangesAsync();
        return appt;
    }

    // ==================== ОНБОРДИНГ ====================

    public async Task<OnboardingProgress> GetOrCreateOnboardingAsync(int employeeId)
    {
        var progress = await dbContext.OnboardingProgresses
            .FirstOrDefaultAsync(o => o.EmployeeId == employeeId);

        if (progress == null)
        {
            progress = new OnboardingProgress
            {
                EmployeeId = employeeId,
                StartedAt  = DateTime.UtcNow
            };
            dbContext.OnboardingProgresses.Add(progress);
            await dbContext.SaveChangesAsync();
        }
        return progress;
    }

    public async Task<OnboardingProgress> UpdateOnboardingStepAsync(int employeeId, string step, bool value)
    {
        var progress = await GetOrCreateOnboardingAsync(employeeId);

        switch (step.ToLower())
        {
            case "fire_safety":    progress.FireSafetyDone          = value; break;
            case "general_safety": progress.GeneralSafetyDone       = value; break;
            case "cyber_safety":   progress.CyberSafetyDone         = value; break;
            case "pass":           progress.PassReceived            = value; break;
            case "face_id":        progress.FaceIdDone              = value; break;
            case "workplace":      progress.WorkplaceSetupRequested = value; break;
        }

        await dbContext.SaveChangesAsync();
        return progress;
    }

    public async Task<List<OnboardingProgress>> GetAllOnboardingProgressesAsync()
    {
        return await dbContext.OnboardingProgresses
            .Include(o => o.Employee)
            .OrderByDescending(o => o.StartedAt)
            .ToListAsync();
    }

    // ==================== АНАЛИТИКА ====================

    public async Task<HrAnalyticsDto> GetAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var totalEmployees   = await dbContext.Employees.CountAsync();
        var newEmployees30   = await dbContext.Employees.CountAsync(e => e.HiredAt >= now.AddDays(-30));
        var pendingVacations = await dbContext.VacationRequests.CountAsync(v => v.Status == VacationStatus.Pending);
        var pendingCerts     = await dbContext.CertificateRequests.CountAsync(c => c.Status == RequestStatus.Pending);
        var pendingEquip     = await dbContext.EquipmentRequests.CountAsync(e => e.Status == RequestStatus.Pending);
        var pendingTotal     = pendingVacations + pendingCerts + pendingEquip;

        var csatScores = await dbContext.CsatScores.ToListAsync();
        var csatScore  = csatScores.Count > 0 ? csatScores.Average(s => s.Score) : 0.0;

        // Топ интентов из логов
        var intentCounts = await dbContext.RequestLogs
            .GroupBy(r => r.ScenarioType)
            .Select(g => new IntentStat { Name = g.Key ?? "Прочее", Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .Take(6)
            .ToListAsync();

        if (intentCounts.Count == 0)
        {
            intentCounts =
            [
                new IntentStat { Name = "Справки",     Value = 45 },
                new IntentStat { Name = "Отпуск",      Value = 38 },
                new IntentStat { Name = "Оборудование",Value = 22 },
                new IntentStat { Name = "IT-доступ",   Value = 18 },
                new IntentStat { Name = "Онбординг",   Value = 12 },
            ];
        }

        var bottlenecks = new List<RequestBottleneck>
        {
            new() { Category = "Отпуска",      PendingCount = pendingVacations, Status = pendingVacations > 5 ? "critical" : pendingVacations > 2 ? "warning" : "ok" },
            new() { Category = "Справки",      PendingCount = pendingCerts,     Status = pendingCerts > 10 ? "critical" : pendingCerts > 5 ? "warning" : "ok" },
            new() { Category = "Оборудование", PendingCount = pendingEquip,     Status = pendingEquip > 3 ? "warning" : "ok" },
        };

        return new HrAnalyticsDto
        {
            TotalEmployees     = totalEmployees,
            NewEmployees30Days = newEmployees30,
            PendingRequests    = pendingTotal,
            CsatScore          = Math.Round(csatScore, 1),
            CsatTotal          = csatScores.Count,
            TopIntents         = intentCounts,
            AvgResponseTimeMs  = 750,
            Bottlenecks        = bottlenecks
        };
    }
}
