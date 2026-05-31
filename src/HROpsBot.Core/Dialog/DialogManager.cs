using HROpsBot.Core.Handlers;
using HROpsBot.Core.NLU;
using HROpsBot.Core.Services;
using HROpsBot.MockAPI;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Dialog;

public class DialogManager(
    GeminiNluClient nluClient,
    VacationHandler vacationHandler,
    CertificateHandler certificateHandler,
    RegulationSearchHandler regulationHandler,
    EquipmentHandler equipmentHandler,
    TaskHandler taskHandler,
    AppointmentHandler appointmentHandler,
    FaqHandler faqHandler,
    CsatService csatService,
    MockHRService hrService,
    I18nService i18n,
    Microsoft.Extensions.Configuration.IConfiguration config,
    ILogger<DialogManager> logger)
{
    /// <summary>Обрабатывает входящее текстовое сообщение</summary>
    public async Task<BotResponse> HandleMessageAsync(ConversationState state, string userText)
    {
        // Шаг ожидания — пользователь в середине диалога
        if (state.CurrentStep != DialogStep.Idle)
            return await HandleStepAsync(state, userText);

        // Классифицируем через Gemini
        var nlu = await nluClient.ClassifyAsync(userText);
        logger.LogInformation("NLU: [{Intent}] conf={Confidence:F2} lang={Language} text={Text}",
            nlu.Intent, nlu.Confidence, nlu.DetectedLanguage, userText);

        return await RouteAsync(state, nlu, userText);
    }

    /// <summary>Обрабатывает callback от inline-кнопки</summary>
    public async Task<BotResponse> HandleCallbackAsync(ConversationState state, string callbackData)
    {
        logger.LogInformation("Callback: [{Data}] chatId={ChatId}", callbackData, state.ChatId);

        return callbackData switch
        {
            "main_menu"          => GetMainMenu(state),
            "vacation.status"    => await vacationHandler.HandleAsync(state, ""),
            "certificate.request"=> await certificateHandler.HandleAsync(state, ""),
            "regulation.search"  => await regulationHandler.HandleAsync(state, ""),
            "equipment.request"  => await equipmentHandler.HandleAsync(state, ""),
            "task.list"          => await taskHandler.HandleListAsync(state),
            "task.overdue"       => await taskHandler.HandleOverdueAsync(state),
            "hr.appointment"     => await appointmentHandler.HandleAsync(state, ""),
            "faq.general"        => await faqHandler.HandleAsync(state, ""),

            var cb when cb.StartsWith("cert_")     => await certificateHandler.HandleTypeSelectedAsync(state, cb),
            var cb when cb.StartsWith("delivery_") => await certificateHandler.HandleDeliverySelectedAsync(state, cb),
            var cb when cb.StartsWith("equip_")    => await equipmentHandler.HandleTypeSelectedAsync(state, cb),
            var cb when cb.StartsWith("slot_")     => await appointmentHandler.HandleSlotSelectedAsync(state, cb),
            var cb when cb.StartsWith("reg_open_") => await regulationHandler.HandleOpenDocAsync(state, int.Parse(cb[9..])),
            var cb when cb.StartsWith("faq_")      => await faqHandler.HandleFaqItemAsync(state, cb),
            var cb when cb.StartsWith("csat_")     => await csatService.HandleCsatAsync(state, cb),

            _ => BotResponse.Create(i18n.Get("fallback"), GetMainMenuKeyboard())
        };
    }

    private async Task<BotResponse> RouteAsync(ConversationState state, NluResult nlu, string userText)
    {
        // Подгружаем сотрудника при первом обращении
        if (state.EmployeeId == null)
        {
            var emp = await hrService.GetEmployeeByTelegramIdAsync(state.ChatId);
            if (emp != null) state.EmployeeId = emp.Id;
        }

        return nlu.Intent switch
        {
            NluResult.Intents.VacationStatus    => await vacationHandler.HandleAsync(state, userText),
            NluResult.Intents.CertificateRequest => await certificateHandler.HandleAsync(state, userText),
            NluResult.Intents.RegulationSearch  => await regulationHandler.HandleAsync(state, userText),
            NluResult.Intents.EquipmentRequest  => await equipmentHandler.HandleAsync(state, userText),
            NluResult.Intents.TaskList          => await taskHandler.HandleListAsync(state),
            NluResult.Intents.TaskStatus        => await taskHandler.HandleOverdueAsync(state),
            NluResult.Intents.HrAppointment     => await appointmentHandler.HandleAsync(state, userText),
            NluResult.Intents.FaqGeneral        => await faqHandler.HandleAsync(state, userText),
            NluResult.Intents.Greeting          => GetWelcome(state),
            NluResult.Intents.Help              => GetHelp(state),
            _                                   => GetFallback(state)
        };
    }

    /// <summary>Обрабатывает шаг многошагового диалога (ожидание ввода)</summary>
    private async Task<BotResponse> HandleStepAsync(ConversationState state, string userText)
    {
        return state.CurrentStep switch
        {
            DialogStep.WaitingRegulationQuery => await regulationHandler.HandleQueryAsync(state, userText),
            _ => GetFallback(state) // Для остальных шагов ожидаем callback
        };
    }

    private BotResponse GetWelcome(ConversationState state) =>
        BotResponse.Create(i18n.Get("welcome"), GetMainMenuKeyboard());

    private BotResponse GetHelp(ConversationState state) =>
        BotResponse.Create(i18n.Get("help"), GetMainMenuKeyboard());

    private BotResponse GetFallback(ConversationState state) =>
        BotResponse.Create(i18n.Get("fallback"), GetMainMenuKeyboard());

    private BotResponse GetMainMenu(ConversationState state)
    {
        state.Reset();
        return BotResponse.Create(i18n.Get("help"), GetMainMenuKeyboard());
    }

    public InlineKeyboardMarkup GetMainMenuKeyboard()
    {
        var webAppUrl = config["Telegram:WebAppUrl"] ?? "https://example.com";
        return new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithWebApp("📱 Открыть Mini App / Mini App ашу", new Telegram.Bot.Types.WebAppInfo { Url = webAppUrl })
            ],
            [
                InlineKeyboardButton.WithCallbackData("📅 Отпуск / Демалыс", "vacation.status"),
                InlineKeyboardButton.WithCallbackData("📄 Справка / Анықтама", "certificate.request")
            ],
            [
                InlineKeyboardButton.WithCallbackData("🔍 Регламент / Ереже", "regulation.search"),
                InlineKeyboardButton.WithCallbackData("💻 Оборудование / Жабдық", "equipment.request")
            ],
            [
                InlineKeyboardButton.WithCallbackData("✅ Задачи / Тапсырмалар", "task.list"),
                InlineKeyboardButton.WithCallbackData("🗓️ К HR / HR-ға", "hr.appointment")
            ],
            [
                InlineKeyboardButton.WithCallbackData("❓ FAQ", "faq.general")
            ]
        ]);
    }
}
