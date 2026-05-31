using HROpsBot.Domain.Entities;

namespace HROpsBot.MockAPI;

/// <summary>Mock сервис задач (имитация Jira/MS Planner)</summary>
public class MockTaskService
{
    private static readonly List<TaskItem> _tasks =
    [
        new() {
            Id = 1, EmployeeId = 1,
            TitleRu = "Провести код-ревью PR #142",
            TitleKk = "PR #142 код шолуын өткізу",
            Status = TaskItemStatus.InProgress, Priority = TaskPriority.High,
            Deadline = DateTime.UtcNow.AddDays(1), ExternalId = "TASK-142"
        },
        new() {
            Id = 2, EmployeeId = 1,
            TitleRu = "Написать unit-тесты для модуля авторизации",
            TitleKk = "Авторизация модулі үшін unit-тесттер жазу",
            Status = TaskItemStatus.Todo, Priority = TaskPriority.Medium,
            Deadline = DateTime.UtcNow.AddDays(3), ExternalId = "TASK-138"
        },
        new() {
            Id = 3, EmployeeId = 1,
            TitleRu = "Обновить документацию API",
            TitleKk = "API құжаттамасын жаңарту",
            Status = TaskItemStatus.Todo, Priority = TaskPriority.Low,
            Deadline = DateTime.UtcNow.AddDays(-1), ExternalId = "TASK-130" // просрочена
        },
        new() {
            Id = 4, EmployeeId = 2,
            TitleRu = "Подготовить презентацию для клиента",
            TitleKk = "Клиент үшін презентация дайындау",
            Status = TaskItemStatus.InProgress, Priority = TaskPriority.Critical,
            Deadline = DateTime.UtcNow.AddHours(4), ExternalId = "TASK-201"
        },
        new() {
            Id = 5, EmployeeId = 2,
            TitleRu = "Согласовать бюджет на Q3",
            TitleKk = "Q3 бюджетін келісу",
            Status = TaskItemStatus.Todo, Priority = TaskPriority.High,
            Deadline = DateTime.UtcNow.AddDays(-2), ExternalId = "TASK-188" // просрочена
        }
    ];

    public Task<List<TaskItem>> GetActiveTasksAsync(int employeeId)
    {
        var tasks = _tasks
            .Where(t => t.EmployeeId == employeeId && t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Cancelled)
            .OrderByDescending(t => t.IsOverdue)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.Deadline)
            .ToList();
        return Task.FromResult(tasks);
    }

    public Task<List<TaskItem>> GetOverdueTasksAsync(int employeeId)
    {
        var tasks = _tasks
            .Where(t => t.EmployeeId == employeeId && t.IsOverdue)
            .ToList();
        return Task.FromResult(tasks);
    }

    public Task<List<TaskItem>> GetAllActiveForNotificationsAsync()
    {
        // Возвращает задачи которые скоро истекают и ещё не были уведомлены
        var threshold = DateTime.UtcNow.AddHours(24);
        var tasks = _tasks
            .Where(t =>
                t.Status != TaskItemStatus.Done &&
                t.Status != TaskItemStatus.Cancelled &&
                t.Deadline.HasValue &&
                t.Deadline.Value <= threshold &&
                (t.LastNotifiedAt == null || t.LastNotifiedAt < DateTime.UtcNow.AddHours(-4)))
            .ToList();
        return Task.FromResult(tasks);
    }

    public Task MarkNotifiedAsync(int taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null) task.LastNotifiedAt = DateTime.UtcNow;
        return Task.CompletedTask;
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
