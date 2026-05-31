namespace HROpsBot.Domain.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public MessageRole Role { get; set; }
    public string ContentRu { get; set; } = string.Empty;
    public string ContentKk { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public double? Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MessageRole
{
    User,
    Bot
}
