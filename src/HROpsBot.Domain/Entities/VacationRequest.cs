namespace HROpsBot.Domain.Entities;

public class VacationRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysCount => (EndDate - StartDate).Days + 1;
    public VacationStatus Status { get; set; } = VacationStatus.Pending;
    public string? CommentRu { get; set; }
    public string? CommentKk { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum VacationStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}
