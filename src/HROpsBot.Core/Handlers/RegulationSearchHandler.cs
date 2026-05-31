using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class RegulationSearchHandler(IDocService docService, I18nService i18n)
{
    public Task<BotResponse> HandleAsync(ConversationState state, string userText)
    {
        // Если уже есть текст в запросе — ищем сразу
        if (!string.IsNullOrWhiteSpace(userText) && userText.Length > 3 &&
            !userText.ToLower().Contains("регламент") &&
            !userText.ToLower().Contains("ереже") &&
            !userText.ToLower().Contains("политик"))
        {
            state.Set("reg_query", userText);
            return SearchAndRespond(state, userText);
        }

        state.CurrentStep = DialogStep.WaitingRegulationQuery;
        return Task.FromResult(BotResponse.Create(i18n.Get("regulation.ask_query")));
    }

    public Task<BotResponse> HandleQueryAsync(ConversationState state, string query)
    {
        state.Set("reg_query", query);
        return SearchAndRespond(state, query);
    }

    private async Task<BotResponse> SearchAndRespond(ConversationState state, string query)
    {
        var results = await docService.SearchAsync(query);

        if (results.Count == 0)
        {
            var notFoundRu = $"😔 По запросу «{query}» ничего не найдено.";
            var notFoundKk = $"😔 «{query}» бойынша ештеңе табылмады.";
            state.Reset();
            return BotResponse.Create($"{notFoundRu}\n\n{notFoundKk}",
                new InlineKeyboardMarkup([[
                    InlineKeyboardButton.WithCallbackData("🔍 Новый поиск / Жаңа іздеу", "regulation.search"),
                    InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
                ]]));
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📚 Найдено: {results.Count} / Табылды: {results.Count}");
        sb.AppendLine();

        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var reg in results.Take(5))
        {
            var previewRu = reg.ContentRu.Length > 80 ? reg.ContentRu[..80] + "…" : reg.ContentRu;
            var previewKk = reg.ContentKk.Length > 80 ? reg.ContentKk[..80] + "…" : reg.ContentKk;
            sb.AppendLine($"📄 *{reg.TitleRu}* / *{reg.TitleKk}*");
            sb.AppendLine($"_{previewRu}_");
            sb.AppendLine();
            buttons.Add([InlineKeyboardButton.WithCallbackData($"📖 {reg.TitleRu}", $"reg_open_{reg.Id}")]);
        }

        buttons.Add([
            InlineKeyboardButton.WithCallbackData("🔍 Ещё / Тағы", "regulation.search"),
            InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
        ]);

        state.Reset();
        return BotResponse.Create(sb.ToString(), new InlineKeyboardMarkup(buttons));
    }

    public async Task<BotResponse> HandleOpenDocAsync(ConversationState state, int docId)
    {
        var reg = await docService.GetByIdAsync(docId);
        if (reg == null) return BotResponse.Create(i18n.Get("error.general"));

        var text = $"📄 *{reg.TitleRu}*\n*{reg.TitleKk}*\n\n" +
                   $"🇷🇺 {reg.ContentRu}\n\n" +
                   $"🇰🇿 {reg.ContentKk}";

        return BotResponse.Create(text, new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("⬅️ Назад / Артқа", "regulation.search"),
            InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
        ]]));
    }
}
