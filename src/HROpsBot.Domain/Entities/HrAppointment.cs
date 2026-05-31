namespace HROpsBot.Domain.Entities;

public class HrAppointment
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime SlotDateTime { get; set; }
    public string HrManagerNameRu { get; set; } = string.Empty;
    public string HrManagerNameKk { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? TopicRu { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    Cancelled,
    Completed
}
