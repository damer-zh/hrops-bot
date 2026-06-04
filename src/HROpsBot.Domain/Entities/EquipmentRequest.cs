namespace HROpsBot.Domain.Entities;

public class EquipmentRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public EquipmentType Type { get; set; }
    public string DescriptionRu { get; set; } = string.Empty;
    public string DescriptionKk { get; set; } = string.Empty;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? TicketNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public enum EquipmentType
{
    Laptop,
    Monitor,
    Keyboard,
    Mouse,
    Headset,
    Phone,
    Chair,
    Desk,
    Other
}
