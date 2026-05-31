using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Interfaces;

public interface IItRequestService
{
    Task<ItRequest> CreateItRequestAsync(int employeeId, ItRequestType type, string systemName, string description, int priority = 2);
    Task<List<ItRequest>> GetEmployeeItRequestsAsync(int employeeId);
    Task<List<ItRequest>> GetPendingItRequestsAsync();
    Task<ItRequest?> UpdateItRequestStatusAsync(int id, ItRequestStatus status, string? note = null);
}
