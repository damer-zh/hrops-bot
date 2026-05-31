using HROpsBot.Domain.Entities;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HROpsBot.Core.Interfaces;

namespace HROpsBot.Infrastructure.Services;

public class TaskService(AppDbContext dbContext) : ITaskService
{
    public async Task<List<TaskItem>> GetActiveTasksAsync(int employeeId)
    {
        return await dbContext.TaskItems
            .Where(t => t.EmployeeId == employeeId && t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Cancelled)
            .OrderByDescending(t => t.Deadline < DateTime.UtcNow) // Сначала просроченные (IsOverdue логика)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.Deadline)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetOverdueTasksAsync(int employeeId)
    {
        return await dbContext.TaskItems
            .Where(t => t.EmployeeId == employeeId && t.Deadline < DateTime.UtcNow && t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Cancelled)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetAllActiveForNotificationsAsync()
    {
        var threshold = DateTime.UtcNow.AddHours(24);
        return await dbContext.TaskItems
            .Where(t =>
                t.Status != TaskItemStatus.Done &&
                t.Status != TaskItemStatus.Cancelled &&
                t.Deadline <= threshold &&
                (t.LastNotifiedAt == null || t.LastNotifiedAt < DateTime.UtcNow.AddHours(-4)))
            .ToListAsync();
    }

    public async Task MarkNotifiedAsync(int taskId)
    {
        var task = await dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task != null)
        {
            task.LastNotifiedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    public static (string Ru, string Kk) GetPriorityLabel(TaskPriority priority) =>
        priority switch
        {
            TaskPriority.Critical => ("🔴 Критичный", "🔴 Маңызды"),
            TaskPriority.High     => ("🟠 Высокий",   "🟠 Жоғары"),
            TaskPriority.Medium   => ("🟡 Средний",   "🟡 Орташа"),
            TaskPriority.Low      => ("🟢 Низкий",    "🟢 Төмен"),
            _                     => ("⚪ Обычный",   "⚪ Қалыпты")
        };

    public static (string Ru, string Kk) GetStatusLabel(TaskItemStatus status) =>
        status switch
        {
            TaskItemStatus.Todo       => ("📋 К выполнению", "📋 Орындалуы керек"),
            TaskItemStatus.InProgress => ("⚙️ В работе",     "⚙️ Орындалуда"),
            TaskItemStatus.Review     => ("👀 На проверке",  "👀 Тексерілуде"),
            TaskItemStatus.Done       => ("✅ Выполнено",    "✅ Орындалды"),
            _                         => ("❌ Отменено",     "❌ Бас тартылды")
        };
}
