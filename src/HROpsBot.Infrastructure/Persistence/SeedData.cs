using HROpsBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HROpsBot.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Employees.AnyAsync()) return;

        // --- Сотрудники ---
        var employees = new List<Employee>
        {
            new() {
                TelegramId = 479526836,
                NameRu = "Алия Бекова",
                NameKk = "Әлия Бекова",
                Department = "Разработка",
                Position = "Старший разработчик",
                Email = "a.bekova@company.kz",
                HiredAt = DateTime.SpecifyKind(new DateTime(2021, 3, 15), DateTimeKind.Utc),
                VacationDaysTotal = 28,
                VacationDaysUsed = 10,
                IsHrAdmin = true // Сделаем вас HR-админом для полного доступа
            }
        };
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        // --- Регламенты ---
        var regulations = new List<Regulation>
        {
            new() {
                TitleRu = "Политика отпусков",
                TitleKk = "Демалыс саясаты",
                ContentRu = "Ежегодный оплачиваемый отпуск составляет 28 календарных дней. Отпуск предоставляется по графику, утверждённому до 15 декабря текущего года. Перенос отпуска допускается по согласованию с руководителем. Компенсация за неиспользованный отпуск выплачивается при увольнении.",
                ContentKk = "Жылдық ақылы демалыс 28 күнтізбелік күнді құрайды. Демалыс ағымдағы жылдың 15 желтоқсанына дейін бекітілген кестеге сәйкес беріледі. Демалысты ауыстыруға жетекшімен келісу бойынша рұқсат етіледі.",
                Category = "vacation",
                Tags = "отпуск,демалыс,ежегодный,calendar",
                UpdatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new() {
                TitleRu = "Политика командировок",
                TitleKk = "Іссапар саясаты",
                ContentRu = "Командировки оформляются приказом не позднее чем за 3 рабочих дня. Суточные для поездок по Казахстану — 5000 тенге/день, за рубеж — согласно актуальным нормам. Авансовый отчёт предоставляется в течение 5 рабочих дней после возвращения.",
                ContentKk = "Іссапарлар кемінде 3 жұмыс күні бұрын бұйрықпен рәсімделеді. Қазақстан бойынша тәуліктік — 5000 теңге/күн, шетелге — ағымдағы нормаларға сәйкес.",
                Category = "travel",
                Tags = "командировка,іссапар,суточные,авансовый",
                UpdatedAt = DateTime.UtcNow.AddMonths(-1)
            },
            new() {
                TitleRu = "Дресс-код",
                TitleKk = "Киім үлгісі",
                ContentRu = "В офисе принят деловой casual стиль одежды. В пятницу допускается casual. Встречи с клиентами — строго деловой стиль. Спортивная одежда не допускается.",
                ContentKk = "Кеңседе іскери casual киім үлгісі қабылданған. Жұмада casual рұқсат етіледі. Клиенттермен кездесу — қатаң іскери үлгі.",
                Category = "hr",
                Tags = "дресс-код,одежда,офис,style",
                UpdatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new() {
                TitleRu = "Политика удалённой работы",
                TitleKk = "Қашықтан жұмыс саясаты",
                ContentRu = "Сотрудники могут работать удалённо до 2 дней в неделю по согласованию с руководителем. Обязательное присутствие в офисе в понедельник и четверг. Для удалённой работы необходимо уведомить команду в Telegram-канале.",
                ContentKk = "Қызметкерлер жетекшімен келісу бойынша аптасына 2 күнге дейін қашықтан жұмыс істей алады. Дүйсенбі мен бейсенбіде кеңседе болу міндетті.",
                Category = "remote",
                Tags = "удалённая,remote,work,дистанционная",
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new() {
                TitleRu = "Охрана труда и безопасность",
                TitleKk = "Еңбекті қорғау және қауіпсіздік",
                ContentRu = "Все сотрудники обязаны пройти инструктаж по охране труда при трудоустройстве. Повторный инструктаж — раз в год. О любом несчастном случае необходимо немедленно уведомить HR и службу безопасности.",
                ContentKk = "Барлық қызметкерлер жұмысқа орналасу кезінде еңбекті қорғау жөніндегі нұсқаулықтан өтуге міндетті. Қайталама нұсқаулық — жылына бір рет.",
                Category = "safety",
                Tags = "охрана труда,безопасность,қауіпсіздік,инструктаж",
                UpdatedAt = DateTime.UtcNow.AddMonths(-3)
            }
        };
        context.Regulations.AddRange(regulations);

        // --- Задачи для сотрудников ---
        var tasks = new List<TaskItem>
        {
            new() {
                EmployeeId = employees[0].Id,
                TitleRu = "Провести код-ревью PR #142",
                TitleKk = "PR #142 код шолуын өткізу",
                Status = TaskItemStatus.InProgress,
                Priority = TaskPriority.High,
                Deadline = DateTime.UtcNow.AddDays(1),
                ExternalId = "TASK-142"
            },
            new() {
                EmployeeId = employees[0].Id,
                TitleRu = "Написать unit-тесты для модуля авторизации",
                TitleKk = "Авторизация модулі үшін unit-тесттер жазу",
                Status = TaskItemStatus.Todo,
                Priority = TaskPriority.Medium,
                Deadline = DateTime.UtcNow.AddDays(3),
                ExternalId = "TASK-138"
            },
            new() {
                EmployeeId = employees[0].Id,
                TitleRu = "Обновить документацию API",
                TitleKk = "API құжаттамасын жаңарту",
                Status = TaskItemStatus.Todo,
                Priority = TaskPriority.Low,
                Deadline = DateTime.UtcNow.AddDays(-1), // просрочена!
                ExternalId = "TASK-130"
            }
        };
        context.TaskItems.AddRange(tasks);

        await context.SaveChangesAsync();
    }
}
