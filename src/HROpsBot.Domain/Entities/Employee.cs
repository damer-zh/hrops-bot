namespace HROpsBot.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string NameRu { get; set; } = string.Empty;
    public string NameKk { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsHrAdmin { get; set; } = false;
    public DateTime HiredAt { get; set; }
    public int VacationDaysTotal { get; set; } = 28;
    public int VacationDaysUsed { get; set; } = 0;
    public int VacationDaysRemaining => VacationDaysTotal - VacationDaysUsed;

    public ICollection<Conversation> Conversations { get; set; } = [];
    public ICollection<VacationRequest> VacationRequests { get; set; } = [];
    public ICollection<CertificateRequest> CertificateRequests { get; set; } = [];
    public ICollection<EquipmentRequest> EquipmentRequests { get; set; } = [];
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
