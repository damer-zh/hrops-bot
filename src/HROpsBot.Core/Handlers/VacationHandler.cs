using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class VacationHandler(IHrService hrService, I18nService i18n)
{
    public virtual async Task<BotResponse> HandleAsync(ConversationState state, string userText)
    {
        if (state.EmployeeId == null)
            return BotResponse.Create(i18n.Get("fallback"));

        var (total, used, remaining) = await hrService.GetVacationBalanceAsync(state.EmployeeId.Value);
        var nextVacation = await hrService.GetNextVacationAsync(state.EmployeeId.Value);

        var balanceMsg = i18n.Get("vacation.days_remaining", new
        {
            Total = total,
            Used = used,
            Remaining = remaining
        });

        string nextMsg;
        if (nextVacation != null)
        {
            var dateRu = $"{nextVacation.StartDate:dd MMMM} – {nextVacation.EndDate:dd MMMM yyyy}";
            var statusRu = nextVacation.Status == Domain.Entities.VacationStatus.Approved ? "✅ Утверждён" : "⏳ На согласовании";
            var statusKk = nextVacation.Status == Domain.Entities.VacationStatus.Approved ? "✅ Бекітілді" : "⏳ Келісілуде";
            nextMsg = $"\n\n📆 Следующий отпуск: *{dateRu}* ({statusRu})" +
                      $"\n\n📆 Келесі демалыс: *{dateRu}* ({statusKk})";
        }
        else
        {
            nextMsg = $"\n\n{i18n.Get("vacation.no_upcoming")}";
        }

        state.Reset();
        return BotResponse.Create(balanceMsg + nextMsg, MainMenuKeyboard());
    }

    private static InlineKeyboardMarkup MainMenuKeyboard() =>
        new([[InlineKeyboardButton.WithCallbackData("🏠 Главное меню / Басты мәзір", "main_menu")]]);
}
