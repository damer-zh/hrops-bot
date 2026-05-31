namespace HROpsBot.Core.Dialog;

public class ConversationState
{
    public long ChatId { get; set; }
    public int? EmployeeId { get; set; }
    public int? ConversationDbId { get; set; }
    public DialogStep CurrentStep { get; set; } = DialogStep.Idle;
    public string? PendingIntent { get; set; }
    public Dictionary<string, string> Context { get; set; } = [];
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public bool WaitingForCsat { get; set; } = false;

    public void Reset()
    {
        CurrentStep = DialogStep.Idle;
        PendingIntent = null;
        Context.Clear();
        WaitingForCsat = false;
    }

    public void Set(string key, string value) => Context[key] = value;
    public string? Get(string key) => Context.GetValueOrDefault(key);
}

public enum DialogStep
{
    Idle,
    WaitingCertificateType,
    WaitingCertificateDelivery,
    WaitingEquipmentType,
    WaitingRegulationQuery,
    WaitingAppointmentSlot,
    WaitingCsat,
    WaitingOnboardingDepartment,
    WaitingOnboardingPosition
}
