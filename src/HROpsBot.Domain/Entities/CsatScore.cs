namespace HROpsBot.Domain.Entities;

public class CsatScore
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public int Score { get; set; } // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
