using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.Services;
using HROpsBot.Infrastructure.Cache;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HROpsBot.Infrastructure.Telegram;

public class TelegramBotAdapter(
    ITelegramBotClient botClient,
    DialogManager dialogManager,
    RedisSessionStore sessionStore,
    CsatService csatService,
    ILogger<TelegramBotAdapter> logger)
{
    public async Task HandleUpdateAsync(Update update)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
                await HandleMessageAsync(update.Message);
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                await HandleCallbackAsync(update.CallbackQuery);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in update handler");
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var text = message.Text!;

        logger.LogInformation("Message from {ChatId}: {Text}", chatId, text);

        var state = await sessionStore.GetOrCreateAsync(chatId);
        BotResponse response;

        if (text == "/start")
        {
            state.Reset();
            response = BotResponse.Create(
                "👋 Привет! Я HROps-бот. Помогу с HR-вопросами.\n\n" +
                "👋 Сәлем! Мен HROps-боты. HR-сұрақтарыңызда көмектесемін.",
                dialogManager.GetMainMenuKeyboard());
        }
        else if (text == "/help" || text == "/menu")
        {
            response = BotResponse.Create(
                "📋 Что я умею / Мен не істей аламын:",
                dialogManager.GetMainMenuKeyboard());
        }
        else
        {
            response = await dialogManager.HandleMessageAsync(state, text);
        }

        await SendResponseAsync(chatId, response);

        if (response.EndConversation && !state.WaitingForCsat)
        {
            var csatResponse = csatService.AskCsat(state);
            await SendResponseAsync(chatId, csatResponse);
        }

        await sessionStore.SaveAsync(state);
    }

    private async Task HandleCallbackAsync(CallbackQuery callback)
    {
        var chatId = callback.Message!.Chat.Id;
        var data = callback.Data ?? "";

        try {
            await botClient.AnswerCallbackQueryAsync(callbackQueryId: callback.Id);
        } catch { /* ignore if already answered */ }

        var state = await sessionStore.GetOrCreateAsync(chatId);
        var response = await dialogManager.HandleCallbackAsync(state, data);

        try
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: callback.Message.MessageId,
                text: response.Text,
                parseMode: ParseMode.Markdown,
                replyMarkup: response.Keyboard
            );
        }
        catch
        {
            await SendResponseAsync(chatId, response);
        }

        if (response.EndConversation && !state.WaitingForCsat)
        {
            var csatResponse = csatService.AskCsat(state);
            await SendResponseAsync(chatId, csatResponse);
        }

        await sessionStore.SaveAsync(state);
    }

    private async Task SendResponseAsync(long chatId, BotResponse response)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: response.Text,
            parseMode: ParseMode.Markdown,
            replyMarkup: response.Keyboard
        );
    }

    public async Task SendNotificationAsync(long telegramChatId, string text)
    {
        await botClient.SendTextMessageAsync(
            chatId: telegramChatId,
            text: text,
            parseMode: ParseMode.Markdown
        );
    }
}
