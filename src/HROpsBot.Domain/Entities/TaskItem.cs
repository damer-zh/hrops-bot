namespace HROpsBot.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string TitleRu { get; set; } = string.Empty;
    public string TitleKk { get; set; } = string.Empty;
    public string? DescriptionRu { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? Deadline { get; set; }
    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.UtcNow && Status != TaskItemStatus.Done;
    public DateTime? LastNotifiedAt { get; set; }
    public string? ExternalId { get; set; } // Jira/Planner task ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TaskItemStatus
{
    Todo,
    InProgress,
    Review,
    Done,
    Cancelled
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}
