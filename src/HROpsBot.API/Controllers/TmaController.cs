using HROpsBot.Core.Interfaces;
using HROpsBot.Domain.Entities;
using HROpsBot.API.Hubs;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HROpsBot.API.Controllers;

/// <summary>
/// Telegram Mini App API — управление профилем сотрудника, заявками и HR-процессами.
/// </summary>
[ApiController]
[Route("api/tma")]
[Produces("application/json")]
public class TmaController(
    IHrService hrService,
    IEquipmentService equipmentService,
    ITaskService taskService,
    IDocService docService,
    IItRequestService itRequestService,
    AppDbContext dbContext,
    IHubContext<NotificationHub> hubContext) : ControllerBase
{
    // ==================== AUTH ====================

    public class AuthRequest
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = "";
        public string? LastName { get; set; }
        public string? Username { get; set; }
    }

    /// <summary>Аутентификация / создание профиля сотрудника</summary>
    /// <remarks>Вызывается при запуске Mini App. Создаёт нового сотрудника или обновляет имя/username существующего.</remarks>
    /// <param name="request">Данные пользователя из <c>Telegram.WebApp.initDataUnsafe.user</c></param>
    /// <response code="200">Профиль сотрудника: Id, NameRu, NameKk, Department, Position, IsHrAdmin, HiredAt</response>
    [HttpPost("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Auth([FromBody] AuthRequest request)
    {
        var emp = await hrService.CreateOrUpdateEmployeeAsync(request.Id, request.FirstName, request.LastName, request.Username);
        return Ok(new
        {
            emp.Id,
            emp.NameRu,
            emp.NameKk,
            emp.Department,
            emp.Position,
            emp.IsHrAdmin,
            emp.HiredAt
        });
    }

    // ==================== ONBOARDING PROFILE ====================

    public class OnboardingRequest
    {
        public int EmployeeId { get; set; }
        public string Department { get; set; } = "";
        public string Position { get; set; } = "";
    }

    /// <summary>Заполнение анкеты при онбординге</summary>
    /// <remarks>Обновляет отдел и должность сотрудника и автоматически инициализирует чеклист онбординга.</remarks>
    /// <param name="req">EmployeeId, Department, Position</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    [HttpPost("onboarding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Onboarding([FromBody] OnboardingRequest req)
    {
        await hrService.UpdateEmployeeProfileAsync(req.EmployeeId, req.Department, req.Position);
        // Auto-create onboarding progress
        await hrService.GetOrCreateOnboardingAsync(req.EmployeeId);
        return Ok(new { success = true });
    }

    // ==================== ONBOARDING CHECKLIST ====================

    /// <summary>Получить прогресс онбординга сотрудника</summary>
    /// <param name="employeeId">Идентификатор сотрудника</param>
    /// <response code="200">Объект OnboardingProgress со всеми шагами и ProgressPercent</response>
    [HttpGet("onboarding-progress/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnboardingProgress(int employeeId)
    {
        var progress = await hrService.GetOrCreateOnboardingAsync(employeeId);
        return Ok(new
        {
            progress.Id,
            progress.EmployeeId,
            progress.DocsSubmitted,
            progress.AccessGranted,
            progress.EquipmentReceived,
            progress.MaterialsRead,
            progress.FirstTasksDone,
            progress.BuddyMet,
            progress.Hr1on1Done,
            progress.ProgressPercent,
            progress.StartedAt
        });
    }

    public class OnboardingStepRequest
    {
        public string Step { get; set; } = "";
        public bool Value { get; set; }
    }

    /// <summary>Отметить шаг онбординга как выполненный / невыполненный</summary>
    /// <param name="employeeId">Идентификатор сотрудника</param>
    /// <param name="req">Шаг (Step) и значение (Value). Допустимые шаги: DocsSubmitted, AccessGranted, EquipmentReceived, MaterialsRead, FirstTasksDone, BuddyMet, Hr1on1Done</param>
    /// <response code="200"><c>{ "progressPercent": 42 }</c></response>
    [HttpPost("onboarding-progress/{employeeId:int}/step")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOnboardingStep(int employeeId, [FromBody] OnboardingStepRequest req)
    {
        var progress = await hrService.UpdateOnboardingStepAsync(employeeId, req.Step, req.Value);
        return Ok(new { progress.ProgressPercent });
    }

    // ==================== EMPLOYEE DASHBOARD ====================

    /// <summary>Главный дашборд сотрудника</summary>
    /// <remarks>Возвращает баланс отпуска, активные задачи, запросы оборудования, заявки на отпуск и IT-заявки одним запросом.</remarks>
    /// <param name="employeeId">Идентификатор сотрудника</param>
    /// <response code="200">Агрегированный объект: vacation, tasks, equipment, vacations, itRequests</response>
    /// <response code="404">Сотрудник не найден</response>
    [HttpGet("dashboard/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(int employeeId)
    {
        var emp = await hrService.GetEmployeeByIdAsync(employeeId);
        if (emp == null) return NotFound();

        var (total, used, remaining) = await hrService.GetVacationBalanceAsync(employeeId);
        var tasks     = await taskService.GetActiveTasksAsync(employeeId);
        var equipment = await equipmentService.GetEmployeeRequestsAsync(employeeId);
        var vacations = await hrService.GetEmployeeVacationRequestsAsync(employeeId);
        var itRequests = await itRequestService.GetEmployeeItRequestsAsync(employeeId);

        return Ok(new
        {
            vacation = new { total, used, remaining },
            tasks = tasks.Select(t => new { t.Id, t.TitleRu, t.TitleKk, t.Status, t.Priority, t.IsOverdue }),
            equipment = equipment.Select(e => new { e.Id, e.TicketNumber, e.Type, e.Status }),
            vacations = vacations.Take(5).Select(v => new
            {
                v.Id, v.StartDate, v.EndDate, v.Status, v.DaysCount, v.CreatedAt
            }),
            itRequests = itRequests.Take(5).Select(r => new
            {
                r.Id, r.Type, r.SystemName, r.Status, r.Priority, r.CreatedAt
            })
        });
    }

    // ==================== CERTIFICATES ====================

    public class CertificateReq
    {
        public int EmployeeId { get; set; }
        public CertificateType Type { get; set; }
        public string DeliveryMethod { get; set; } = "digital";
    }

    /// <summary>Заказать справку</summary>
    /// <remarks>Типы: EmploymentConfirmation (справка с места работы), SalaryStatement (2-НДФЛ), IncomeTax (ИПН), WorkExperience (стаж).</remarks>
    /// <param name="req">EmployeeId, Type (CertificateType), DeliveryMethod («digital» | «paper»)</param>
    /// <response code="200">Созданная заявка на справку</response>
    [HttpPost("certificate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestCertificate([FromBody] CertificateReq req)
    {
        var result = await hrService.CreateCertificateRequestAsync(req.EmployeeId, req.Type, req.DeliveryMethod);
        return Ok(result);
    }

    // ==================== EQUIPMENT ====================

    public class EquipmentReq
    {
        public int EmployeeId { get; set; }
        public EquipmentType Type { get; set; }
    }

    /// <summary>Запросить оборудование</summary>
    /// <remarks>Типы: Laptop, Monitor, Keyboard, Mouse, Headset, Phone, Chair, Desk, Other.</remarks>
    /// <param name="req">EmployeeId, Type (EquipmentType)</param>
    /// <response code="200">Созданная заявка на оборудование</response>
    [HttpPost("equipment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestEquipment([FromBody] EquipmentReq req)
    {
        var result = await equipmentService.CreateRequestAsync(req.EmployeeId, req.Type);
        return Ok(result);
    }

    // ==================== VACATION ====================

    public class VacationReq
    {
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>Подать заявку на отпуск</summary>
    /// <param name="req">EmployeeId, StartDate, EndDate (EndDate должен быть позже StartDate)</param>
    /// <response code="200">Id, StartDate, EndDate, DaysCount, Status созданной заявки</response>
    /// <response code="400">Дата окончания раньше или равна дате начала</response>
    [HttpPost("vacation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestVacation([FromBody] VacationReq req)
    {
        if (req.EndDate <= req.StartDate)
            return BadRequest(new { error = "Дата окончания должна быть позже даты начала" });

        var result = await hrService.CreateVacationRequestAsync(req.EmployeeId, req.StartDate, req.EndDate);
        return Ok(new { result.Id, result.StartDate, result.EndDate, result.DaysCount, result.Status });
    }

    // ==================== IT REQUESTS ====================

    public class ItReq
    {
        public int EmployeeId { get; set; }
        public ItRequestType Type { get; set; }
        public string SystemName { get; set; } = "";
        public string Description { get; set; } = "";
        public int Priority { get; set; } = 2;
    }

    /// <summary>Создать IT-заявку</summary>
    /// <remarks>Приоритеты: 1 — Высокий, 2 — Обычный, 3 — Низкий. Типы: SystemAccess, FolderAccess, GroupAccess, EmailSetup, VpnAccess, Other.</remarks>
    /// <param name="req">EmployeeId, Type, SystemName, Description, Priority (1–3)</param>
    /// <response code="200">Id, Type, SystemName, Status, CreatedAt созданной заявки</response>
    [HttpPost("it-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateItRequest([FromBody] ItReq req)
    {
        var result = await itRequestService.CreateItRequestAsync(req.EmployeeId, req.Type, req.SystemName, req.Description, req.Priority);
        return Ok(new { result.Id, result.Type, result.SystemName, result.Status, result.CreatedAt });
    }

    /// <summary>Получить IT-заявки сотрудника</summary>
    /// <param name="employeeId">Идентификатор сотрудника</param>
    /// <response code="200">Список IT-заявок сотрудника</response>
    [HttpGet("it-requests/{employeeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyItRequests(int employeeId)
    {
        var requests = await itRequestService.GetEmployeeItRequestsAsync(employeeId);
        return Ok(requests.Select(r => new
        {
            r.Id, r.Type, r.SystemName, r.Description, r.Status, r.Priority, r.CreatedAt, r.ResolvedAt
        }));
    }

    // ==================== REGULATIONS ====================

    /// <summary>Поиск по регламентам и документам</summary>
    /// <param name="q">Поисковый запрос (минимум 1 символ)</param>
    /// <response code="200">Список найденных документов</response>
    [HttpGet("regulations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchRegulations([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        var docs = await docService.SearchAsync(q);
        return Ok(docs);
    }

    // ==================== APPOINTMENTS ====================

    /// <summary>Получить доступные слоты записи к HR</summary>
    /// <response code="200">Список доступных временных слотов</response>
    [HttpGet("appointments/slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlots()
    {
        var slots = await hrService.GetAvailableSlotsAsync();
        return Ok(slots);
    }

    public class AppointmentReq
    {
        public int EmployeeId { get; set; }
        public DateTime Slot { get; set; }
    }

    /// <summary>Записаться на встречу с HR</summary>
    /// <param name="req">EmployeeId и выбранный временной слот (Slot)</param>
    /// <response code="200">Id, SlotDateTime, HrManagerNameRu, Status созданной записи</response>
    [HttpPost("appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BookAppointment([FromBody] AppointmentReq req)
    {
        var appt = await hrService.CreateAppointmentAsync(req.EmployeeId, req.Slot);
        return Ok(new { appt.Id, appt.SlotDateTime, appt.HrManagerNameRu, appt.Status });
    }

    // ==================== FAQ ====================

    /// <summary>Список часто задаваемых вопросов</summary>
    /// <response code="200">Массив объектов <c>{ question, answer }</c></response>
    [HttpGet("faq")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetFaq()
    {
        return Ok(new[]
        {
            new { question = "Как получить справку с места работы?",      answer = "Зайдите в раздел 'Справка' → выберите тип → нажмите 'Заказать'. Справка будет готова в течение 1 рабочего дня." },
            new { question = "Когда выплачивается зарплата?",              answer = "Аванс — 20 числа, основная зарплата — 5 числа следующего месяца. При задержке обратитесь в бухгалтерию." },
            new { question = "Как оформить отпуск?",                      answer = "Через раздел 'Отпуск' → выберите даты → отправьте заявку. HR рассмотрит в течение 2 рабочих дней." },
            new { question = "Как запросить доступ к системе?",            answer = "Раздел 'IT-запросы' → тип запроса → название системы → описание. Срок выполнения 1-3 рабочих дня." },
            new { question = "Как связаться с HR?",                       answer = "Через запись на встречу в разделе 'К HR'. Доступные слоты обновляются ежедневно." },
            new { question = "Как изменить личные данные?",               answer = "Обратитесь напрямую к HR-менеджеру или через @hrops_bot с запросом на обновление данных." },
        });
    }

    // ==================== HR ADMIN ====================

    /// <summary>[ADMIN] Аналитика HR — сводная статистика</summary>
    /// <remarks>Требует прав HR-администратора (IsHrAdmin = true).</remarks>
    /// <response code="200">Агрегированная аналитика по заявкам, сотрудникам и онбордингу</response>
    [HttpGet("admin/analytics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics()
    {
        var analytics = await hrService.GetAnalyticsAsync();
        return Ok(analytics);
    }

    /// <summary>[ADMIN] Список всех сотрудников</summary>
    /// <response code="200">Список сотрудников: Id, NameRu, Department, Position, IsHrAdmin, HiredAt, VacationDaysRemaining</response>
    [HttpGet("admin/employees")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await hrService.GetAllEmployeesAsync();
        return Ok(employees.Select(e => new
        {
            e.Id, e.NameRu, e.Department, e.Position, e.IsHrAdmin,
            e.HiredAt, e.VacationDaysRemaining
        }));
    }

    /// <summary>[ADMIN] Все ожидающие заявки (отпуска, справки, IT, оборудование)</summary>
    /// <response code="200">Объект { vacations, certificates, itRequests, equipment } со списками Pending-заявок</response>
    [HttpGet("admin/requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPendingRequests()
    {
        var vacations   = await hrService.GetPendingVacationRequestsAsync();
        var certs       = await hrService.GetPendingCertificateRequestsAsync();
        var itRequests  = await itRequestService.GetPendingItRequestsAsync();
        var equipment   = await equipmentService.GetPendingRequestsAsync();

        return Ok(new
        {
            vacations = vacations.Select(v => new
            {
                v.Id, v.StartDate, v.EndDate, v.DaysCount, v.Status, v.CreatedAt,
                employee = new { v.Employee.Id, v.Employee.NameRu, v.Employee.Department }
            }),
            certificates = certs.Select(c => new
            {
                c.Id, c.Type, c.Status, c.DeliveryMethod, c.CreatedAt,
                employee = new { c.Employee.Id, c.Employee.NameRu, c.Employee.Department }
            }),
            itRequests = itRequests.Select(r => new
            {
                r.Id, r.Type, r.SystemName, r.Description, r.Status, r.Priority, r.CreatedAt,
                employee = new { r.Employee.Id, r.Employee.NameRu, r.Employee.Department }
            }),
            equipment = equipment.Select(e => new
            {
                e.Id, e.Type, e.DescriptionRu, e.Status, e.TicketNumber, e.CreatedAt,
                employee = new { e.Employee.Id, e.Employee.NameRu, e.Employee.Department }
            })
        });
    }

    // --- Одобрить/Отклонить отпуск ---
    public class DecisionReq
    {
        public string? Reason { get; set; }
    }

    /// <summary>[ADMIN] Одобрить заявку на отпуск</summary>
    /// <param name="id">Id заявки на отпуск</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/vacation/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveVacation(int id)
    {
        var ok = await hrService.ApproveVacationAsync(id);
        if (!ok) return NotFound();

        var req = await dbContext.VacationRequests.FindAsync(id);
        if (req != null)
        {
            await NotifyStatusChangedAsync(
                req.EmployeeId,
                "vacation",
                req.Id,
                "Approved",
                "Заявка на отпуск одобрена");
        }

        return Ok(new { success = true });
    }

    /// <summary>[ADMIN] Отклонить заявку на отпуск</summary>
    /// <param name="id">Id заявки на отпуск</param>
    /// <param name="req">Причина отказа (Reason, обязательна)</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    /// <response code="400">Причина отказа не указана</response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/vacation/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectVacation(int id, [FromBody] DecisionReq req)
    {
        var reason = req.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(new { error = "Укажите причину отказа" });

        var ok = await hrService.RejectVacationAsync(id);
        if (!ok) return NotFound();

        var vacation = await dbContext.VacationRequests.FindAsync(id);
        if (vacation != null)
        {
            vacation.CommentRu = reason;
            vacation.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            await NotifyStatusChangedAsync(
                vacation.EmployeeId,
                "vacation",
                vacation.Id,
                "Rejected",
                "Заявка на отпуск отклонена",
                reason);
        }

        return Ok(new { success = true });
    }

    // --- Одобрить/Отклонить справку ---
    /// <summary>[ADMIN] Одобрить заявку на справку</summary>
    /// <param name="id">Id заявки на справку</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/certificate/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveCertificate(int id)
    {
        var ok = await hrService.ApproveCertificateAsync(id);
        if (!ok) return NotFound();

        var req = await dbContext.CertificateRequests.FindAsync(id);
        if (req != null)
        {
            await NotifyStatusChangedAsync(
                req.EmployeeId,
                "certificate",
                req.Id,
                "Approved",
                "Справка одобрена");
        }

        return Ok(new { success = true });
    }

    /// <summary>[ADMIN] Отклонить заявку на справку</summary>
    /// <param name="id">Id заявки на справку</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/certificate/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectCertificate(int id, [FromBody] DecisionReq req)
    {
        var ok = await hrService.RejectCertificateAsync(id);
        if (!ok) return NotFound();

        var cert = await dbContext.CertificateRequests.FindAsync(id);
        if (cert != null)
        {
            var reason = req.Reason?.Trim();
            if (!string.IsNullOrWhiteSpace(reason))
            {
                cert.RejectionReason = reason;
                await dbContext.SaveChangesAsync();
            }

            await NotifyStatusChangedAsync(
                cert.EmployeeId,
                "certificate",
                cert.Id,
                "Rejected",
                "Справка отклонена",
                reason);
        }

        return Ok(new { success = true });
    }

    /// <summary>[ADMIN] Принять заявку на оборудование (перевести в InProgress)</summary>
    /// <param name="id">Id заявки на оборудование</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/equipment/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveEquipment(int id)
    {
        var ok = await equipmentService.UpdateStatusAsync(id, RequestStatus.InProgress);
        if (!ok) return NotFound();

        var req = await dbContext.EquipmentRequests.FindAsync(id);
        if (req != null)
        {
            await NotifyStatusChangedAsync(
                req.EmployeeId,
                "equipment",
                req.Id,
                "InProgress",
                "Заявка на оборудование взята в работу");
        }

        return Ok(new { success = true });
    }

    /// <summary>[ADMIN] Отклонить заявку на оборудование</summary>
    /// <param name="id">Id заявки на оборудование</param>
    /// <response code="200"><c>{ "success": true }</c></response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/equipment/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectEquipment(int id, [FromBody] DecisionReq req)
    {
        var ok = await equipmentService.UpdateStatusAsync(id, RequestStatus.Rejected);
        if (!ok) return NotFound();

        var equipment = await dbContext.EquipmentRequests.FindAsync(id);
        if (equipment != null)
        {
            var reason = req.Reason?.Trim();
            if (!string.IsNullOrWhiteSpace(reason))
            {
                equipment.RejectionReason = reason;
                await dbContext.SaveChangesAsync();
            }

            await NotifyStatusChangedAsync(
                equipment.EmployeeId,
                "equipment",
                equipment.Id,
                "Rejected",
                "Заявка на оборудование отклонена",
                reason);
        }

        return Ok(new { success = true });
    }

    // --- Обновить IT-заявку ---
    public class ItStatusReq
    {
        public ItRequestStatus Status { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>[ADMIN] Изменить статус IT-заявки</summary>
    /// <param name="id">Id IT-заявки</param>
    /// <param name="req">Новый статус (Status: Pending | InProgress | Done | Rejected) и заметка (Note, обязательна при Rejected)</param>
    /// <response code="200">Id и обновлённый Status заявки</response>
    /// <response code="400">Note не указан при статусе Rejected</response>
    /// <response code="404">Заявка не найдена</response>
    [HttpPost("admin/it-request/{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItRequestStatus(int id, [FromBody] ItStatusReq req)
    {
        if (req.Status == ItRequestStatus.Rejected && string.IsNullOrWhiteSpace(req.Note))
            return BadRequest(new { error = "Укажите причину отказа" });

        var result = await itRequestService.UpdateItRequestStatusAsync(id, req.Status, req.Note);
        if (result == null) return NotFound();

        var title = req.Status switch
        {
            ItRequestStatus.Done => "IT-заявка выполнена",
            ItRequestStatus.Rejected => "IT-заявка отклонена",
            ItRequestStatus.InProgress => "IT-заявка в работе",
            _ => "Статус IT-заявки обновлен"
        };

        await NotifyStatusChangedAsync(
            result.EmployeeId,
            "it",
            result.Id,
            result.Status.ToString(),
            title,
            req.Note);

        return Ok(new { result.Id, result.Status });
    }

    private async Task NotifyStatusChangedAsync(
        int employeeId,
        string requestType,
        int requestId,
        string status,
        string message,
        string? reason = null)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null) return;

        await hubContext.Clients.Group($"emp_{employee.TelegramId}").SendAsync("ReceiveNotification", new
        {
            requestType,
            requestId,
            status,
            message,
            reason,
            changedAt = DateTime.UtcNow
        });
    }
}
