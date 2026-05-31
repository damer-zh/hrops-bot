using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.Services;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Services;

public class CsatService(I18nService i18n)
{
    public BotResponse AskCsat(ConversationState state)
    {
        state.CurrentStep = DialogStep.WaitingCsat;
        state.WaitingForCsat = true;

        var text = $"⭐ Оцените качество обслуживания / Қызмет сапасын бағалаңыз:";

        var keyboard = new InlineKeyboardMarkup(
        [[
            InlineKeyboardButton.WithCallbackData("⭐ 1", "csat_1"),
            InlineKeyboardButton.WithCallbackData("⭐⭐ 2", "csat_2"),
            InlineKeyboardButton.WithCallbackData("⭐⭐⭐ 3", "csat_3"),
            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐ 4", "csat_4"),
            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐⭐ 5", "csat_5")
        ]]);

        return BotResponse.Create(text, keyboard);
    }

    public Task<BotResponse> HandleCsatAsync(ConversationState state, string callbackData)
    {
        var scoreStr = callbackData.Replace("csat_", "");
        if (!int.TryParse(scoreStr, out var score) || score < 1 || score > 5)
            return Task.FromResult(BotResponse.Create(i18n.Get("error.general")));

        state.Reset();

        var stars = new string('⭐', score);
        var text = $"{stars} Спасибо за оценку!\n\n{stars} Бағаңыз үшін рахмет!\n\n" +
                   "_Ваш отзыв помогает нам улучшаться. / Пікіріңіз жақсаруға көмектеседі._";

        return Task.FromResult(BotResponse.Create(text, new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("🏠 Главное меню / Басты мәзір", "main_menu")
        ]])));
    }
}
