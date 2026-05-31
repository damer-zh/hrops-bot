namespace HROpsBot.Domain.Entities;

public class OnboardingProgress
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }

    // Шаги онбординга
    public bool DocsSubmitted      { get; set; } = false; // Документы сданы
    public bool AccessGranted      { get; set; } = false; // Получил доступы к системам
    public bool EquipmentReceived  { get; set; } = false; // Получил технику
    public bool MaterialsRead      { get; set; } = false; // Прочитал вводные материалы
    public bool FirstTasksDone     { get; set; } = false; // Выполнил первые задачи
    public bool BuddyMet           { get; set; } = false; // Познакомился с бадди/ментором
    public bool Hr1on1Done         { get; set; } = false; // Прошёл встречу 1-на-1 с HR

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    // Навигационное свойство
    public Employee Employee { get; set; } = null!;

    // Вычисляемый процент прогресса
    public int ProgressPercent
    {
        get
        {
            var steps = new[] { DocsSubmitted, AccessGranted, EquipmentReceived, MaterialsRead, FirstTasksDone, BuddyMet, Hr1on1Done };
            return (int)Math.Round((double)steps.Count(s => s) / steps.Length * 100);
        }
    }
}
