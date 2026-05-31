using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Interfaces;

public interface IEquipmentService
{
    Task<EquipmentRequest> CreateRequestAsync(int employeeId, EquipmentType type);
    Task<EquipmentRequest?> GetRequestAsync(int id);
    Task<List<EquipmentRequest>> GetEmployeeRequestsAsync(int employeeId);
}
