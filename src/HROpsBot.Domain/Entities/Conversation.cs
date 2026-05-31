namespace HROpsBot.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }
    public long TelegramChatId { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Language { get; set; } = "ru"; // "ru" | "kk" | "both"
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive => EndedAt == null;

    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<RequestLog> RequestLogs { get; set; } = [];
    public CsatScore? CsatScore { get; set; }
}
