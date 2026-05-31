using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Interfaces;

public interface IHrService
{
    Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId);
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee> CreateOrUpdateEmployeeAsync(long telegramId, string firstName, string? lastName, string? username);
    
    Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId);
    Task<VacationRequest?> GetNextVacationAsync(int employeeId);
    Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end);
    
    Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod);
    
    Task<List<DateTime>> GetAvailableSlotsAsync();
    Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot);
}
