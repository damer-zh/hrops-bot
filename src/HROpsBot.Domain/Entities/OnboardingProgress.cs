namespace HROpsBot.Domain.Entities;

public class OnboardingProgress
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }

    // Шаги онбординга
    public bool FireSafetyDone          { get; set; } = false; // Пройти инструктаж по пожарной безопасности
    public bool GeneralSafetyDone       { get; set; } = false; // Пройти инструктаж по общей безопасности
    public bool CyberSafetyDone         { get; set; } = false; // Пройти инструктаж по кибербезопасности
    public bool PassReceived            { get; set; } = false; // Получить пропуск
    public bool FaceIdDone              { get; set; } = false; // Сделать FaceId для входа
    public bool WorkplaceSetupRequested { get; set; } = false; // Запустить заявку на настройку рабочего места

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    // Навигационное свойство
    public Employee Employee { get; set; } = null!;

    // Вычисляемый процент прогресса
    public int ProgressPercent
    {
        get
        {
            var steps = new[] { FireSafetyDone, GeneralSafetyDone, CyberSafetyDone, PassReceived, FaceIdDone, WorkplaceSetupRequested };
            return (int)Math.Round((double)steps.Count(s => s) / steps.Length * 100);
        }
    }
}
