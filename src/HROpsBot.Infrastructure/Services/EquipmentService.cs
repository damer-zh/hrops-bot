using HROpsBot.Domain.Entities;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HROpsBot.Core.Interfaces;

namespace HROpsBot.Infrastructure.Services;

public class EquipmentService(AppDbContext dbContext) : IEquipmentService
{
    public async Task<EquipmentRequest> CreateRequestAsync(int employeeId, EquipmentType type)
    {
        var typeNames = new Dictionary<EquipmentType, (string Ru, string Kk)>
        {
            [EquipmentType.Laptop]   = ("Ноутбук", "Ноутбук"),
            [EquipmentType.Monitor]  = ("Монитор", "Монитор"),
            [EquipmentType.Keyboard] = ("Клавиатура", "Пернетақта"),
            [EquipmentType.Mouse]    = ("Мышь", "Тінтуір"),
            [EquipmentType.Headset]  = ("Гарнитура", "Гарнитура"),
            [EquipmentType.Phone]    = ("Телефон", "Телефон"),
            [EquipmentType.Chair]    = ("Кресло", "Орындық"),
            [EquipmentType.Desk]     = ("Стол", "Үстел"),
            [EquipmentType.Other]    = ("Другое оборудование", "Басқа жабдық")
        };

        var names = typeNames.GetValueOrDefault(type, ("Оборудование", "Жабдық"));
        var req = new EquipmentRequest
        {
            EmployeeId = employeeId,
            Type = type,
            DescriptionRu = names.Item1,
            DescriptionKk = names.Item2,
            Status = RequestStatus.Pending,
            TicketNumber = $"IT-{Random.Shared.Next(10000, 99999)}",
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.EquipmentRequests.Add(req);
        await dbContext.SaveChangesAsync();
        return req;
    }

    public async Task<EquipmentRequest?> GetRequestAsync(int id) =>
        await dbContext.EquipmentRequests.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<EquipmentRequest>> GetEmployeeRequestsAsync(int employeeId) =>
        await dbContext.EquipmentRequests.Where(r => r.EmployeeId == employeeId).ToListAsync();

    public async Task<List<EquipmentRequest>> GetPendingRequestsAsync() =>
        await dbContext.EquipmentRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.InProgress)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<bool> UpdateStatusAsync(int id, RequestStatus status)
    {
        var req = await dbContext.EquipmentRequests.FindAsync(id);
        if (req == null) return false;
        req.Status = status;
        if (status is RequestStatus.Ready or RequestStatus.Delivered or RequestStatus.Rejected)
            req.ResolvedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return true;
    }
}
