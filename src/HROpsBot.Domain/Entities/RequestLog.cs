namespace HROpsBot.Domain.Entities;

public class RequestLog
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public string ScenarioType { get; set; } = string.Empty; // "vacation.status", "equipment.request", etc.
    public RequestLogStatus Status { get; set; } = RequestLogStatus.Started;
    public long ProcessingMs { get; set; } // время обработки ботом
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum RequestLogStatus
{
    Started,
    Completed,
    Failed,
    Fallback // не понял запрос
}
