namespace HROpsBot.Domain.Entities;

public class Regulation
{
    public int Id { get; set; }
    public string TitleRu { get; set; } = string.Empty;
    public string TitleKk { get; set; } = string.Empty;
    public string ContentRu { get; set; } = string.Empty;
    public string ContentKk { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "vacation", "travel", "hr", "safety", etc.
    public string Tags { get; set; } = string.Empty; // comma-separated
    public string? DocumentUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
