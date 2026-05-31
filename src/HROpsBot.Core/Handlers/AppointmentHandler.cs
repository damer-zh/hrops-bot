using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using HROpsBot.MockAPI;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class AppointmentHandler(MockHRService hrService, I18nService i18n)
{
    public async Task<BotResponse> HandleAsync(ConversationState state, string userText)
    {
        state.CurrentStep = DialogStep.WaitingAppointmentSlot;

        var slots = await hrService.GetAvailableSlotsAsync();
        var text = $"🗓️ Выберите время для встречи с HR:\n" +
                   $"🗓️ HR-мен кездесу уақытын таңдаңыз:";

        var buttons = slots.Take(6).Select(slot =>
            new[] { InlineKeyboardButton.WithCallbackData(
                $"📅 {slot:dd.MM HH:mm}",
                $"slot_{slot:yyyyMMddHHmm}"
            )}
        ).ToList();

        buttons.Add([InlineKeyboardButton.WithCallbackData("⬅️ Назад / Артқа", "main_menu")]);

        return BotResponse.Create(text, new InlineKeyboardMarkup(buttons));
    }

    public async Task<BotResponse> HandleSlotSelectedAsync(ConversationState state, string callbackData)
    {
        if (state.EmployeeId == null) return BotResponse.Create(i18n.Get("fallback"));

        // callbackData: "slot_202607151000"
        var dateStr = callbackData.Replace("slot_", "");
        if (!DateTime.TryParseExact(dateStr, "yyyyMMddHHmm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var slotDate))
        {
            return BotResponse.Create(i18n.Get("error.general"));
        }

        var appt = await hrService.CreateAppointmentAsync(state.EmployeeId.Value, slotDate);

        var dateFormatted = slotDate.ToString("dd MMMM yyyy, HH:mm");
        var text = $"✅ Встреча запланирована! / Кездесу жоспарланды!\n\n" +
                   $"📅 *{dateFormatted}*\n" +
                   $"👤 HR-менеджер: *{appt.HrManagerNameRu}* / *{appt.HrManagerNameKk}*\n\n" +
                   $"_Подтверждение придёт за 1 час. / Растау 1 сағат бұрын келеді._";

        state.Reset();
        return BotResponse.Create(text, new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("🏠 Главное меню / Басты мәзір", "main_menu")
        ]]));
    }
}
