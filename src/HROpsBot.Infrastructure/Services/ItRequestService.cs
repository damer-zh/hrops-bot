using HROpsBot.Domain.Entities;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HROpsBot.Core.Interfaces;

namespace HROpsBot.Infrastructure.Services;

public class ItRequestService(AppDbContext dbContext) : IItRequestService
{
    public async Task<ItRequest> CreateItRequestAsync(int employeeId, ItRequestType type, string systemName, string description, int priority = 2)
    {
        var request = new ItRequest
        {
            EmployeeId  = employeeId,
            Type        = type,
            SystemName  = systemName,
            Description = description,
            Priority    = priority,
            Status      = ItRequestStatus.Pending,
            CreatedAt   = DateTime.UtcNow
        };
        dbContext.ItRequests.Add(request);
        await dbContext.SaveChangesAsync();
        return request;
    }

    public async Task<List<ItRequest>> GetEmployeeItRequestsAsync(int employeeId) =>
        await dbContext.ItRequests
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<List<ItRequest>> GetPendingItRequestsAsync() =>
        await dbContext.ItRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == ItRequestStatus.Pending || r.Status == ItRequestStatus.InProgress)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

    public async Task<ItRequest?> UpdateItRequestStatusAsync(int id, ItRequestStatus status, string? note = null)
    {
        var request = await dbContext.ItRequests.FindAsync(id);
        if (request == null) return null;

        request.Status = status;
        if (note != null) request.ResolutionNote = note;
        if (status is ItRequestStatus.Done or ItRequestStatus.Rejected)
            request.ResolvedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return request;
    }
}
