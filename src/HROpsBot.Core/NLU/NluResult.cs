namespace HROpsBot.Core.NLU;

public class NluResult
{
    public string Intent { get; set; } = "fallback";
    public double Confidence { get; set; } = 0.0;
    public string DetectedLanguage { get; set; } = "ru"; // "ru" | "kk"
    public Dictionary<string, string> Entities { get; set; } = [];

    public bool IsHighConfidence => Confidence >= 0.7;
    public bool IsFallback => Intent == "fallback" || !IsHighConfidence;

    // Известные интенты
    public static class Intents
    {
        public const string VacationStatus = "vacation.status";
        public const string CertificateRequest = "certificate.request";
        public const string RegulationSearch = "regulation.search";
        public const string EquipmentRequest = "equipment.request";
        public const string TaskList = "task.list";
        public const string TaskStatus = "task.status";
        public const string HrAppointment = "hr.appointment";
        public const string FaqGeneral = "faq.general";
        public const string Fallback = "fallback";
        public const string Greeting = "greeting";
        public const string Help = "help";
    }
}
