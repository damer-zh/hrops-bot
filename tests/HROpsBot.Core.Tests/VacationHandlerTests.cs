using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Services;
using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Tests;

public class VacationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenEmployeeIdMissing_ReturnsFallback()
    {
        var i18n = new I18nService();
        var handler = new VacationHandler(new FakeHrService(), i18n);
        var state = new ConversationState { EmployeeId = null };

        var response = await handler.HandleAsync(state, "отпуск");

        Assert.Equal(i18n.Get("fallback"), response.Text);
    }

    [Fact]
    public async Task HandleAsync_WithNoUpcomingVacation_ResetsState()
    {
        var i18n = new I18nService();
        var handler = new VacationHandler(new FakeHrService(), i18n);
        var state = new ConversationState
        {
            EmployeeId = 42,
            CurrentStep = DialogStep.WaitingRegulationQuery,
            PendingIntent = "vacation.status"
        };
        state.Set("tmp", "1");

        var response = await handler.HandleAsync(state, "хочу в отпуск");

        Assert.Contains(i18n.Get("vacation.no_upcoming"), response.Text);
        Assert.Equal(DialogStep.Idle, state.CurrentStep);
        Assert.Null(state.PendingIntent);
        Assert.Empty(state.Context);
        Assert.NotNull(response.Keyboard);
    }

    private sealed class FakeHrService : IHrService
    {
        public Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId)
            => Task.FromResult((24, 10, 14));

        public Task<VacationRequest?> GetNextVacationAsync(int employeeId)
            => Task.FromResult<VacationRequest?>(null);

        public Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId) => throw new NotImplementedException();
        public Task<Employee?> GetEmployeeByIdAsync(int id) => throw new NotImplementedException();
        public Task<Employee> CreateOrUpdateEmployeeAsync(long telegramId, string firstName, string? lastName, string? username) => throw new NotImplementedException();
        public Task UpdateEmployeeProfileAsync(int employeeId, string department, string position) => throw new NotImplementedException();
        public Task<List<Employee>> GetAllEmployeesAsync() => throw new NotImplementedException();
        public Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end) => throw new NotImplementedException();
        public Task<List<VacationRequest>> GetPendingVacationRequestsAsync() => throw new NotImplementedException();
        public Task<bool> ApproveVacationAsync(int requestId) => throw new NotImplementedException();
        public Task<bool> RejectVacationAsync(int requestId) => throw new NotImplementedException();
        public Task<List<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId) => throw new NotImplementedException();
        public Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod) => throw new NotImplementedException();
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
