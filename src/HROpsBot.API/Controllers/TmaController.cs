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
    IDocService docService) : ControllerBase
{
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
            emp.IsHrAdmin
        });
    }

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
        return Ok(new { success = true });
    }

    [HttpGet("dashboard/{employeeId:int}")]
    public async Task<IActionResult> GetDashboard(int employeeId)
    {
        var emp = await hrService.GetEmployeeByIdAsync(employeeId);
        if (emp == null) return NotFound();

        var (total, used, remaining) = await hrService.GetVacationBalanceAsync(employeeId);
        var tasks = await taskService.GetActiveTasksAsync(employeeId);
        var equipment = await equipmentService.GetEmployeeRequestsAsync(employeeId);

        return Ok(new
        {
            vacation = new { total, used, remaining },
            tasks = tasks.Select(t => new { t.Id, t.TitleRu, t.TitleKk, t.Status, t.Priority, t.IsOverdue }),
            equipment = equipment.Select(e => new { e.Id, e.TicketNumber, e.Type, e.Status })
        });
    }

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

    [HttpGet("regulations")]
    public async Task<IActionResult> SearchRegulations([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        var docs = await docService.SearchAsync(q);
        return Ok(docs);
    }

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
        return Ok(appt);
    }

    [HttpGet("faq")]
    public IActionResult GetFaq()
    {
        // Static FAQ data for TMA
        return Ok(new[]
        {
            new { question = "Как получить справку с места работы?", answer = "Вы можете заказать её через меню 'Справка'. Справка будет готова в течение дня." },
            new { question = "Когда выплачивается зарплата?", answer = "Аванс выплачивается 20 числа, а зарплата 5 числа следующего месяца." },
            new { question = "Как оформить больничный?", answer = "Нужно открыть больничный лист в поликлинике и сообщить вашему руководителю и HR." }
        });
    }

    [HttpGet("admin/stats")]
    public IActionResult GetAdminStats()
    {
        return Ok(new
        {
            csatScore = 4.8,
            csatTotal = 152,
            intents = new[]
            {
                new { name = "Отпуск", value = 400 },
                new { name = "Справки", value = 300 },
                new { name = "Задачи", value = 200 },
                new { name = "Оборудование", value = 150 },
                new { name = "Регламенты", value = 100 }
            },
            avgResponseTimeMs = 850
        });
    }
}
