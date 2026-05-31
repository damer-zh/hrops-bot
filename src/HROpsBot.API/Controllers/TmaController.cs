using HROpsBot.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace HROpsBot.API.Controllers;

[ApiController]
[Route("api/tma")]
public class TmaController(
    HrService hrService,
    EquipmentService equipmentService,
    TaskService taskService) : ControllerBase
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

    [HttpGet("admin/stats")]
    public IActionResult GetAdminStats()
    {
        // Mock analytics data for HR Admin Dashboard
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
