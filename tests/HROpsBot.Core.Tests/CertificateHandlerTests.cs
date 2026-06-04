using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Services;
using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Tests;

public class CertificateHandlerTests
{
    [Fact]
    public async Task HandleTypeSelectedAsync_SetsStateForDeliveryStep()
    {
        var state = new ConversationState { EmployeeId = 1 };
        var handler = new CertificateHandler(new FakeHrService(), new I18nService());

        var response = await handler.HandleTypeSelectedAsync(state, "cert_salary");

        Assert.Equal(DialogStep.WaitingCertificateDelivery, state.CurrentStep);
        Assert.Equal(nameof(CertificateType.SalaryStatement), state.Get("cert_type"));
        Assert.Contains("Справка о зарплате", response.Text);
        Assert.NotNull(response.Keyboard);
    }

    [Fact]
    public async Task HandleDeliverySelectedAsync_WhenEmployeeMissing_ReturnsFallback()
    {
        var i18n = new I18nService();
        var state = new ConversationState { EmployeeId = null };
        var handler = new CertificateHandler(new FakeHrService(), i18n);

        var response = await handler.HandleDeliverySelectedAsync(state, "delivery_email");

        Assert.Equal(i18n.Get("fallback"), response.Text);
    }

    private sealed class FakeHrService : IHrService
    {
        public Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod)
            => Task.FromResult(new CertificateRequest
            {
                Id = 1,
                EmployeeId = employeeId,
                Type = type,
                DeliveryMethod = deliveryMethod,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

        public Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId) => throw new NotImplementedException();
        public Task<Employee?> GetEmployeeByIdAsync(int id) => throw new NotImplementedException();
        public Task<Employee> CreateOrUpdateEmployeeAsync(long telegramId, string firstName, string? lastName, string? username) => throw new NotImplementedException();
        public Task UpdateEmployeeProfileAsync(int employeeId, string department, string position) => throw new NotImplementedException();
        public Task<List<Employee>> GetAllEmployeesAsync() => throw new NotImplementedException();
        public Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId) => throw new NotImplementedException();
        public Task<VacationRequest?> GetNextVacationAsync(int employeeId) => throw new NotImplementedException();
        public Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end) => throw new NotImplementedException();
        public Task<List<VacationRequest>> GetPendingVacationRequestsAsync() => throw new NotImplementedException();
        public Task<bool> ApproveVacationAsync(int requestId) => throw new NotImplementedException();
        public Task<bool> RejectVacationAsync(int requestId) => throw new NotImplementedException();
        public Task<List<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId) => throw new NotImplementedException();
        public Task<List<CertificateRequest>> GetPendingCertificateRequestsAsync() => throw new NotImplementedException();
        public Task<bool> ApproveCertificateAsync(int requestId) => throw new NotImplementedException();
        public Task<bool> RejectCertificateAsync(int requestId) => throw new NotImplementedException();
        public Task<List<DateTime>> GetAvailableSlotsAsync() => throw new NotImplementedException();
        public Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot) => throw new NotImplementedException();
        public Task<OnboardingProgress> GetOrCreateOnboardingAsync(int employeeId) => throw new NotImplementedException();
        public Task<OnboardingProgress> UpdateOnboardingStepAsync(int employeeId, string step, bool value) => throw new NotImplementedException();
        public Task<HrAnalyticsDto> GetAnalyticsAsync() => throw new NotImplementedException();
    }
}
