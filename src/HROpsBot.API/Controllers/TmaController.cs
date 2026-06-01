using HROpsBot.Core.Interfaces;
using HROpsBot.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HROpsBot.API.Controllers;

[ApiController]
[Route("api/tma")]
public class TmaController(
    IHrService hrService,
    IEquipmentService equipmentService,
    ITaskService taskService,
    IDocService docService,
    IItRequestService itRequestService) : ControllerBase
{
    // ==================== AUTH ====================

    public class AuthRequest
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = "";
        public string? LastName { get; set; }
        public string? Username { get; set; }
    }

    [HttpPost("auth")]
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

    [HttpPost("onboarding")]
    public async Task<IActionResult> Onboarding([FromBody] OnboardingRequest req)
    {
        await hrService.UpdateEmployeeProfileAsync(req.EmployeeId, req.Department, req.Position);
        // Auto-create onboarding progress
        await hrService.GetOrCreateOnboardingAsync(req.EmployeeId);
        return Ok(new { success = true });
    }

    // ==================== ONBOARDING CHECKLIST ====================

    [HttpGet("onboarding-progress/{employeeId:int}")]
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

    [HttpPost("onboarding-progress/{employeeId:int}/step")]
    public async Task<IActionResult> UpdateOnboardingStep(int employeeId, [FromBody] OnboardingStepRequest req)
    {
        var progress = await hrService.UpdateOnboardingStepAsync(employeeId, req.Step, req.Value);
        return Ok(new { progress.ProgressPercent });
    }

    // ==================== EMPLOYEE DASHBOARD ====================

    [HttpGet("dashboard/{employeeId:int}")]
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

    [HttpPost("certificate")]
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

    [HttpPost("equipment")]
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

    [HttpPost("vacation")]
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

    [HttpPost("it-request")]
    public async Task<IActionResult> CreateItRequest([FromBody] ItReq req)
    {
        var result = await itRequestService.CreateItRequestAsync(req.EmployeeId, req.Type, req.SystemName, req.Description, req.Priority);
        return Ok(new { result.Id, result.Type, result.SystemName, result.Status, result.CreatedAt });
    }

    [HttpGet("it-requests/{employeeId:int}")]
    public async Task<IActionResult> GetMyItRequests(int employeeId)
    {
        var requests = await itRequestService.GetEmployeeItRequestsAsync(employeeId);
        return Ok(requests.Select(r => new
        {
            r.Id, r.Type, r.SystemName, r.Description, r.Status, r.Priority, r.CreatedAt, r.ResolvedAt
        }));
    }

    // ==================== REGULATIONS ====================

    [HttpGet("regulations")]
    public async Task<IActionResult> SearchRegulations([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        var docs = await docService.SearchAsync(q);
        return Ok(docs);
    }

    // ==================== APPOINTMENTS ====================

    [HttpGet("appointments/slots")]
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

    [HttpPost("appointments")]
    public async Task<IActionResult> BookAppointment([FromBody] AppointmentReq req)
    {
        var appt = await hrService.CreateAppointmentAsync(req.EmployeeId, req.Slot);
        return Ok(new { appt.Id, appt.SlotDateTime, appt.HrManagerNameRu, appt.Status });
    }

    // ==================== FAQ ====================

    [HttpGet("faq")]
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

    [HttpGet("admin/analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var analytics = await hrService.GetAnalyticsAsync();
        return Ok(analytics);
    }

    [HttpGet("admin/employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await hrService.GetAllEmployeesAsync();
        return Ok(employees.Select(e => new
        {
            e.Id, e.NameRu, e.Department, e.Position, e.IsHrAdmin,
            e.HiredAt, e.VacationDaysRemaining
        }));
    }

    [HttpGet("admin/requests")]
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
    [HttpPost("admin/vacation/{id:int}/approve")]
    public async Task<IActionResult> ApproveVacation(int id)
    {
        var ok = await hrService.ApproveVacationAsync(id);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("admin/vacation/{id:int}/reject")]
    public async Task<IActionResult> RejectVacation(int id)
    {
        var ok = await hrService.RejectVacationAsync(id);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    // --- Одобрить/Отклонить справку ---
    [HttpPost("admin/certificate/{id:int}/approve")]
    public async Task<IActionResult> ApproveCertificate(int id)
    {
        var ok = await hrService.ApproveCertificateAsync(id);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("admin/certificate/{id:int}/reject")]
    public async Task<IActionResult> RejectCertificate(int id)
    {
        var ok = await hrService.RejectCertificateAsync(id);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("admin/equipment/{id:int}/approve")]
    public async Task<IActionResult> ApproveEquipment(int id)
    {
        var ok = await equipmentService.UpdateStatusAsync(id, RequestStatus.InProgress);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("admin/equipment/{id:int}/reject")]
    public async Task<IActionResult> RejectEquipment(int id)
    {
        var ok = await equipmentService.UpdateStatusAsync(id, RequestStatus.Rejected);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    // --- Обновить IT-заявку ---
    public class ItStatusReq
    {
        public ItRequestStatus Status { get; set; }
        public string? Note { get; set; }
    }

    [HttpPost("admin/it-request/{id:int}/status")]
    public async Task<IActionResult> UpdateItRequestStatus(int id, [FromBody] ItStatusReq req)
    {
        var result = await itRequestService.UpdateItRequestStatusAsync(id, req.Status, req.Note);
        return result != null ? Ok(new { result.Id, result.Status }) : NotFound();
    }
}
