namespace HROpsBot.Core.Handlers;

using Telegram.Bot.Types.ReplyMarkups;

/// <summary>Унифицированный ответ бота — текст + опциональная клавиатура</summary>
public class BotResponse
{
    public string Text { get; init; } = string.Empty;
    public InlineKeyboardMarkup? Keyboard { get; init; }
    public bool EndConversation { get; init; }

    public static BotResponse Create(string text, InlineKeyboardMarkup? keyboard = null) =>
        new() { Text = text, Keyboard = keyboard };

    public static BotResponse End(string text) =>
        new() { Text = text, EndConversation = true };
}
