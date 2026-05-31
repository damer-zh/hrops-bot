using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Interfaces;

public interface ITaskService
{
    Task<List<TaskItem>> GetActiveTasksAsync(int employeeId);
    Task<List<TaskItem>> GetOverdueTasksAsync(int employeeId);
    Task<List<TaskItem>> GetAllActiveForNotificationsAsync();
    Task MarkNotifiedAsync(int taskId);
}
