using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class TaskHandler(ITaskService taskService, I18nService i18n)
{
    public async Task<BotResponse> HandleListAsync(ConversationState state)
    {
        if (state.EmployeeId == null) return BotResponse.Create(i18n.Get("fallback"));

        var tasks = await taskService.GetActiveTasksAsync(state.EmployeeId.Value);

        if (tasks.Count == 0)
        {
            state.Reset();
            return BotResponse.Create(
                $"🎉 Нет активных задач!\n\n🎉 Белсенді тапсырмалар жоқ!",
                new InlineKeyboardMarkup([[
                    InlineKeyboardButton.WithCallbackData("🏠 Меню / Мәзір", "main_menu")
                ]]));
        }

        var overdue = tasks.Where(t => t.IsOverdue).ToList();
        var sb = new System.Text.StringBuilder();

        if (overdue.Count > 0)
            sb.AppendLine($"⚠️ *Просрочено: {overdue.Count}* / *Мерзімі өткен: {overdue.Count}*\n");

        sb.AppendLine($"✅ Ваши задачи / Сіздің тапсырмаларыңыз ({tasks.Count}):\n");

        foreach (var task in tasks.Take(5))
        {
            var icon = task.IsOverdue ? "🔴" : task.Status == Domain.Entities.TaskItemStatus.InProgress ? "⚙️" : "📋";
            var (priorityRu, priorityKk) = FormatHelper.GetTaskPriorityLabel(task.Priority);
            var deadline = task.Deadline.HasValue
                ? task.Deadline.Value.ToString("dd.MM HH:mm")
                : "—";
            var overdueFlag = task.IsOverdue ? " ‼️" : "";

            sb.AppendLine($"{icon} *{task.TitleRu}*{overdueFlag}");
            sb.AppendLine($"   _{task.TitleKk}_");
            sb.AppendLine($"   📅 {deadline} | {priorityRu}");
            sb.AppendLine();
        }

        if (tasks.Count > 5)
            sb.AppendLine($"_...и ещё {tasks.Count - 5} задач / ...және тағы {tasks.Count - 5} тапсырма_");

        state.Reset();
        return BotResponse.Create(sb.ToString(), new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("🔴 Просроченные / Мерзімі өткен", "task.overdue"),
            InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
        ]]));
    }

    public async Task<BotResponse> HandleOverdueAsync(ConversationState state)
    {
        if (state.EmployeeId == null) return BotResponse.Create(i18n.Get("fallback"));

        var overdue = await taskService.GetOverdueTasksAsync(state.EmployeeId.Value);

        if (overdue.Count == 0)
        {
            return BotResponse.Create(
                "✅ Просроченных задач нет!\n\n✅ Мерзімі өткен тапсырмалар жоқ!",
                new InlineKeyboardMarkup([[
                    InlineKeyboardButton.WithCallbackData("⬅️ Все задачи / Барлық тапсырмалар", "task.list"),
                    InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
                ]]));
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🔴 *Просроченные задачи ({overdue.Count}):*");
        sb.AppendLine($"🔴 *Мерзімі өткен тапсырмалар ({overdue.Count}):*\n");

        foreach (var task in overdue)
        {
            var deadline = task.Deadline.HasValue ? task.Deadline.Value.ToString("dd.MM.yyyy") : "—";
            var daysOverdue = task.Deadline.HasValue
                ? (int)(DateTime.UtcNow - task.Deadline.Value).TotalDays
                : 0;

            sb.AppendLine($"‼️ *{task.TitleRu}*");
            sb.AppendLine($"   _{task.TitleKk}_");
            sb.AppendLine($"   📅 Дедлайн: {deadline} (просрочено на {daysOverdue} дн. / {daysOverdue} күн)");
            sb.AppendLine();
        }

        return BotResponse.Create(sb.ToString(), new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("⬅️ Все задачи / Барлық", "task.list"),
            InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
        ]]));
    }
}
