namespace HROpsBot.Domain.Entities;

public enum ItRequestType
{
    SystemAccess = 1,   // Доступ к системе (Jira, GitHub, 1C...)
    FolderAccess = 2,   // Доступ к папке/диску
    GroupAccess  = 3,   // Добавление в группу/команду
    EmailSetup   = 4,   // Настройка корпоративной почты
    VpnAccess    = 5,   // VPN доступ
    Other        = 6    // Прочее
}

public enum ItRequestStatus
{
    Pending    = 1,
    InProgress = 2,
    Done       = 3,
    Rejected   = 4
}

public class ItRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public ItRequestType Type { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItRequestStatus Status { get; set; } = ItRequestStatus.Pending;
    public int Priority { get; set; } = 2; // 1=High, 2=Normal, 3=Low
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }

    public Employee Employee { get; set; } = null!;
}
