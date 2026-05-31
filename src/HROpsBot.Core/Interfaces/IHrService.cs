using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Interfaces;

public interface IHrService
{
    // --- Сотрудники ---
    Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId);
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee> CreateOrUpdateEmployeeAsync(long telegramId, string firstName, string? lastName, string? username);
    Task UpdateEmployeeProfileAsync(int employeeId, string department, string position);
    Task<List<Employee>> GetAllEmployeesAsync();

    // --- Отпуска ---
    Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId);
    Task<VacationRequest?> GetNextVacationAsync(int employeeId);
    Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end);
    Task<List<VacationRequest>> GetPendingVacationRequestsAsync();
    Task<bool> ApproveVacationAsync(int requestId);
    Task<bool> RejectVacationAsync(int requestId);
    Task<List<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId);

    // --- Справки ---
    Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod);
    Task<List<CertificateRequest>> GetPendingCertificateRequestsAsync();
    Task<bool> ApproveCertificateAsync(int requestId);
    Task<bool> RejectCertificateAsync(int requestId);

    // --- Запись к HR ---
    Task<List<DateTime>> GetAvailableSlotsAsync();
    Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot);

    // --- Онбординг ---
    Task<OnboardingProgress> GetOrCreateOnboardingAsync(int employeeId);
    Task<OnboardingProgress> UpdateOnboardingStepAsync(int employeeId, string step, bool value);

    // --- Аналитика ---
    Task<HrAnalyticsDto> GetAnalyticsAsync();
}

public class HrAnalyticsDto
{
    public int TotalEmployees { get; set; }
    public int NewEmployees30Days { get; set; }
    public int PendingRequests { get; set; }
    public double CsatScore { get; set; }
    public int CsatTotal { get; set; }
    public List<IntentStat> TopIntents { get; set; } = [];
    public int AvgResponseTimeMs { get; set; }
    public List<RequestBottleneck> Bottlenecks { get; set; } = [];
}

public class IntentStat
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class RequestBottleneck
{
    public string Category { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public string Status { get; set; } = string.Empty; // "ok", "warning", "critical"
}
