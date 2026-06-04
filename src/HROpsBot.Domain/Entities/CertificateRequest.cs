namespace HROpsBot.Domain.Entities;

public class CertificateRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public CertificateType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? DeliveryMethod { get; set; } // "email" | "paper"
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadyAt { get; set; }
    public DateTime EstimatedReadyAt => CreatedAt.AddBusinessDays(3);
}

public enum CertificateType
{
    EmploymentConfirmation,   // Справка с места работы
    SalaryStatement,          // Справка о зарплате (2-НДФЛ)
    IncomeTax,                // КПН / ИПН
    WorkExperience            // Стаж работы
}

public enum RequestStatus
{
    Pending,
    InProgress,
    Approved,
    Ready,
    Delivered,
    Rejected,
    Cancelled
}

public static class DateTimeExtensions
{
    public static DateTime AddBusinessDays(this DateTime date, int days)
    {
        int added = 0;
        while (added < days)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                added++;
        }
        return date;
    }
}
