using System.Net;
using System.Text;
using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.NLU;
using HROpsBot.Core.Services;
using HROpsBot.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HROpsBot.Core.Tests;

/// <summary>
/// Unit-тесты для DialogManager.
/// Все внешние зависимости (handlers, NLU, HR-сервис) заменены на стабы,
/// поэтому тесты проверяют ровно логику роутинга самого DialogManager.
/// </summary>
public class DialogManagerTests
{
    // ─────────────────────────── Routing: message ───────────────────────────

    [Fact]
    public async Task HandleMessageAsync_WhenIntentVacation_CallsVacationHandler()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.VacationStatus);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "хочу в отпуск");

        Assert.Equal("vacation", response.Text);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenIntentCertificate_CallsCertificateHandler()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.CertificateRequest);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "справка");

        Assert.Equal("certificate", response.Text);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenIntentEquipment_CallsEquipmentHandler()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.EquipmentRequest);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "нужен ноутбук");

        Assert.Equal("equipment", response.Text);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenIntentTaskList_CallsTaskHandler()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.TaskList);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "мои задачи");

        Assert.Equal("task_list", response.Text);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenIntentGreeting_ReturnsWelcome()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Greeting);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "привет");

        // welcome использует i18n.Get("welcome") — при отсутствии JSON файла ключ возвращается как есть
        Assert.False(string.IsNullOrWhiteSpace(response.Text));
        Assert.NotNull(response.Keyboard);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenIntentFallback_ReturnsFallbackWithMenu()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "агхпджвр");

        Assert.False(string.IsNullOrWhiteSpace(response.Text));
        Assert.NotNull(response.Keyboard);
    }

    // ─────────────────────────── Routing: step (waiting) ───────────────────

    [Fact]
    public async Task HandleMessageAsync_WhenWaitingRegulationQuery_CallsRegulationQueryHandler()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);
        state.CurrentStep = DialogStep.WaitingRegulationQuery;

        var response = await sut.HandleMessageAsync(state, "командировки");

        Assert.Equal("regulation_query", response.Text);
    }

    [Fact]
    public async Task HandleMessageAsync_WhenWaitingOnboardingDepartment_CallsOnboardingHandler()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);
        state.CurrentStep = DialogStep.WaitingOnboardingDepartment;

        var response = await sut.HandleMessageAsync(state, "IT");

        Assert.Equal("onboarding_department", response.Text);
    }

    // ─────────────────────────── Routing: callbacks ─────────────────────────

    [Theory]
    [InlineData("vacation.status",    "vacation")]
    [InlineData("certificate.request","certificate")]
    [InlineData("equipment.request",  "equipment")]
    [InlineData("task.list",          "task_list")]
    [InlineData("task.overdue",       "task_overdue")]
    [InlineData("hr.appointment",     "appointment")]
    [InlineData("faq.general",        "faq")]
    public async Task HandleCallbackAsync_KnownCallback_RoutesToCorrectHandler(string callbackData, string expectedText)
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleCallbackAsync(state, callbackData);

        Assert.Equal(expectedText, response.Text);
    }

    [Fact]
    public async Task HandleCallbackAsync_MainMenu_ResetsStateAndReturnsMenu()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);
        state.CurrentStep = DialogStep.WaitingCertificateDelivery;
        state.PendingIntent = "cert_salary";
        state.Set("x", "1");

        var response = await sut.HandleCallbackAsync(state, "main_menu");

        Assert.Equal(DialogStep.Idle, state.CurrentStep);
        Assert.Empty(state.Context);
        Assert.NotNull(response.Keyboard);
    }

    [Theory]
    [InlineData("cert_salary")]
    [InlineData("cert_employment")]
    public async Task HandleCallbackAsync_CertPrefix_RoutesToCertificateTypeSelected(string cb)
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleCallbackAsync(state, cb);

        Assert.Equal("cert_type_selected", response.Text);
    }

    [Fact]
    public async Task HandleCallbackAsync_DeliveryPrefix_RoutesToCertificateDeliverySelected()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleCallbackAsync(state, "delivery_email");

        Assert.Equal("cert_delivery_selected", response.Text);
    }

    [Fact]
    public async Task HandleCallbackAsync_EquipPrefix_RoutesToEquipmentTypeSelected()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleCallbackAsync(state, "equip_laptop");

        Assert.Equal("equip_type_selected", response.Text);
    }

    [Fact]
    public async Task HandleCallbackAsync_SlotPrefix_RoutesToAppointmentSlotSelected()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleCallbackAsync(state, "slot_202607151000");

        Assert.Equal("slot_selected", response.Text);
    }

    [Fact]
    public async Task HandleCallbackAsync_UnknownCallback_ReturnsFallback()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.Fallback);
        var state = StateWithEmployee(42);

        var response = await sut.HandleCallbackAsync(state, "totally_unknown_cb");

        Assert.False(string.IsNullOrWhiteSpace(response.Text));
        Assert.NotNull(response.Keyboard);
    }

    // ─────────────────────────── Onboarding intercept ───────────────────────

    [Fact]
    public async Task HandleMessageAsync_WhenEmployeeHasNoDepartment_InterceptsWithOnboarding()
    {
        var sut = BuildManager(nluIntent: NluResult.Intents.TaskList, employeeHasDepartment: false);
        var state = StateWithEmployee(42);

        var response = await sut.HandleMessageAsync(state, "мои задачи");

        // Должен вернуть start-onboarding, а не task_list
        Assert.Equal("onboarding_start", response.Text);
    }

    // ─────────────────────────── Helpers ────────────────────────────────────

    private static ConversationState StateWithEmployee(int id) =>
        new() { ChatId = id, EmployeeId = id };

    private static DialogManager BuildManager(
        string nluIntent,
        bool employeeHasDepartment = true)
    {
        var i18n = new I18nService();
        var csatService = new CsatService(i18n);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:WebAppUrl"] = "https://test.app"
            })
            .Build();

        var fakeHr = new FakeHrService(employeeHasDepartment);

        return new DialogManager(
            nluClient:           BuildNluClient(nluIntent),
            vacationHandler:     new StubVacationHandler(i18n),
            certificateHandler:  new StubCertificateHandler(fakeHr, i18n),
            regulationHandler:   new StubRegulationHandler(null!, i18n),
            equipmentHandler:    new StubEquipmentHandler(null!, i18n),
            taskHandler:         new StubTaskHandler(null!, i18n),
            appointmentHandler:  new StubAppointmentHandler(fakeHr, i18n),
            faqHandler:          new StubFaqHandler(i18n),
            onboardingHandler:   new StubOnboardingHandler(fakeHr, i18n),
            csatService:         csatService,
            hrService:           fakeHr,
            i18n:                i18n,
            config:              config,
            logger:              NullLogger<DialogManager>.Instance
        );
    }

    private static GeminiNluClient BuildNluClient(string intent)
    {
        var payload = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{\\\"intent\\\":\\\"" + intent + "\\\",\\\"confidence\\\":0.99,\\\"detectedLanguage\\\":\\\"ru\\\",\\\"entities\\\":{}}\"}]}}]}";
        var handler = new StubHttpMessageHandler(_ =>
            new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        var http = new System.Net.Http.HttpClient(handler);
        var options = Options.Create(new GeminiOptions { ApiKey = "x", Model = "m", BaseUrl = "https://x.test" });
        return new GeminiNluClient(http, options, NullLogger<GeminiNluClient>.Instance);
    }

    // ──────────────────────── Stub handlers ─────────────────────────────────

    private sealed class StubVacationHandler(I18nService i18n) : VacationHandler(new FakeHrService(true), i18n)
    {
        public override Task<BotResponse> HandleAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("vacation"));
    }

    private sealed class StubCertificateHandler(IHrService hr, I18nService i18n) : CertificateHandler(hr, i18n)
    {
        public override Task<BotResponse> HandleAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("certificate"));
        public override Task<BotResponse> HandleTypeSelectedAsync(ConversationState state, string callbackData)
            => Task.FromResult(BotResponse.Create("cert_type_selected"));
        public override Task<BotResponse> HandleDeliverySelectedAsync(ConversationState state, string callbackData)
            => Task.FromResult(BotResponse.Create("cert_delivery_selected"));
    }

    private sealed class StubRegulationHandler(IDocService docService, I18nService i18n) : RegulationSearchHandler(docService, i18n)
    {
        public override Task<BotResponse> HandleAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("regulation"));
        public override Task<BotResponse> HandleQueryAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("regulation_query"));
        public override Task<BotResponse> HandleOpenDocAsync(ConversationState state, int docId)
            => Task.FromResult(BotResponse.Create("reg_open_doc"));
    }

    private sealed class StubEquipmentHandler(IEquipmentService equipmentService, I18nService i18n) : EquipmentHandler(equipmentService, i18n)
    {
        public override Task<BotResponse> HandleAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("equipment"));
        public override Task<BotResponse> HandleTypeSelectedAsync(ConversationState state, string callbackData)
            => Task.FromResult(BotResponse.Create("equip_type_selected"));
    }

    private sealed class StubTaskHandler(ITaskService taskService, I18nService i18n) : TaskHandler(taskService, i18n)
    {
        public override Task<BotResponse> HandleListAsync(ConversationState state)
            => Task.FromResult(BotResponse.Create("task_list"));
        public override Task<BotResponse> HandleOverdueAsync(ConversationState state)
            => Task.FromResult(BotResponse.Create("task_overdue"));
    }

    private sealed class StubAppointmentHandler(IHrService hrService, I18nService i18n) : AppointmentHandler(hrService, i18n)
    {
        public override Task<BotResponse> HandleAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("appointment"));
        public override Task<BotResponse> HandleSlotSelectedAsync(ConversationState state, string callbackData)
            => Task.FromResult(BotResponse.Create("slot_selected"));
    }

    private sealed class StubFaqHandler(I18nService i18n) : FaqHandler(i18n)
    {
        public override Task<BotResponse> HandleAsync(ConversationState state, string userText)
            => Task.FromResult(BotResponse.Create("faq"));
        public override Task<BotResponse> HandleFaqItemAsync(ConversationState state, string callbackData)
            => Task.FromResult(BotResponse.Create("faq_item"));
    }

    private sealed class StubOnboardingHandler(IHrService hrService, I18nService i18n) : OnboardingHandler(hrService, i18n)
    {
        public override BotResponse StartOnboarding(ConversationState state)
            => BotResponse.Create("onboarding_start");
        public override Task<BotResponse> HandleDepartmentAsync(ConversationState state, string text)
            => Task.FromResult(BotResponse.Create("onboarding_department"));
        public override Task<BotResponse> HandlePositionAsync(ConversationState state, string text)
            => Task.FromResult(BotResponse.Create("onboarding_position"));
    }

    private sealed class FakeHrService(bool hasDepartment) : IHrService
    {
        public Task<Employee?> GetEmployeeByTelegramIdAsync(long telegramId)
            => Task.FromResult<Employee?>(new Employee
            {
                Id = (int)telegramId,
                TelegramId = telegramId,
                NameRu = "Test",
                Department = hasDepartment ? "IT" : null,
                Position = hasDepartment ? "Dev" : null
            });

        public Task<Employee?> GetEmployeeByIdAsync(int id)
            => Task.FromResult<Employee?>(new Employee
            {
                Id = id,
                TelegramId = id,
                NameRu = "Test",
                Department = hasDepartment ? "IT" : null,
                Position = hasDepartment ? "Dev" : null
            });

        public Task<(int Total, int Used, int Remaining)> GetVacationBalanceAsync(int employeeId)
            => Task.FromResult((24, 10, 14));

        public Task<VacationRequest?> GetNextVacationAsync(int employeeId)
            => Task.FromResult<VacationRequest?>(null);

        public Task<List<DateTime>> GetAvailableSlotsAsync()
            => Task.FromResult(new List<DateTime> { DateTime.UtcNow.AddDays(1) });

        public Task<HrAppointment> CreateAppointmentAsync(int employeeId, DateTime slot)
            => Task.FromResult(new HrAppointment { Id = 1, EmployeeId = employeeId, SlotDateTime = slot, HrManagerNameRu = "HR", HrManagerNameKk = "HR" });

        public Task<Employee> CreateOrUpdateEmployeeAsync(long telegramId, string firstName, string? lastName, string? username)
            => Task.FromResult(new Employee { Id = 1, TelegramId = telegramId, NameRu = firstName });

        public Task UpdateEmployeeProfileAsync(int employeeId, string department, string position) => Task.CompletedTask;
        public Task<List<Employee>> GetAllEmployeesAsync() => Task.FromResult(new List<Employee>());
        public Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime start, DateTime end) => throw new NotImplementedException();
        public Task<List<VacationRequest>> GetPendingVacationRequestsAsync() => Task.FromResult(new List<VacationRequest>());
        public Task<bool> ApproveVacationAsync(int requestId) => Task.FromResult(true);
        public Task<bool> RejectVacationAsync(int requestId) => Task.FromResult(true);
        public Task<List<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId) => Task.FromResult(new List<VacationRequest>());
        public Task<CertificateRequest> CreateCertificateRequestAsync(int employeeId, CertificateType type, string deliveryMethod)
            => Task.FromResult(new CertificateRequest { Id = 1, EmployeeId = employeeId, Type = type, CreatedAt = DateTime.UtcNow });
        public Task<List<CertificateRequest>> GetPendingCertificateRequestsAsync() => Task.FromResult(new List<CertificateRequest>());
        public Task<bool> ApproveCertificateAsync(int requestId) => Task.FromResult(true);
        public Task<bool> RejectCertificateAsync(int requestId) => Task.FromResult(true);
        public Task<OnboardingProgress> GetOrCreateOnboardingAsync(int employeeId) => Task.FromResult(new OnboardingProgress { EmployeeId = employeeId });
        public Task<OnboardingProgress> UpdateOnboardingStepAsync(int employeeId, string step, bool value) => Task.FromResult(new OnboardingProgress { EmployeeId = employeeId });
        public Task<HrAnalyticsDto> GetAnalyticsAsync() => Task.FromResult(new HrAnalyticsDto());
    }

    private sealed class StubHttpMessageHandler(Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> responder)
        : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
