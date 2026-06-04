using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using HROpsBot.Domain.Entities;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class CertificateHandler(IHrService hrService, I18nService i18n)
{
    public virtual Task<BotResponse> HandleAsync(ConversationState state, string userText)
    {
        // Шаг 1: показываем выбор типа справки
        state.CurrentStep = DialogStep.WaitingCertificateType;

        var text = i18n.Get("certificate.choose_type");
        var keyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("📋 С места работы / Жұмыс орнынан", "cert_employment"),
                InlineKeyboardButton.WithCallbackData("💰 О зарплате / Жалақы", "cert_salary")
            ],
            [
                InlineKeyboardButton.WithCallbackData("📊 ИПН/КПН", "cert_tax"),
                InlineKeyboardButton.WithCallbackData("⏱️ Стаж / Еңбек өтілі", "cert_experience")
            ],
            [InlineKeyboardButton.WithCallbackData("⬅️ Назад / Артқа", "main_menu")]
        ]);

        return Task.FromResult(BotResponse.Create(text, keyboard));
    }

    public virtual async Task<BotResponse> HandleTypeSelectedAsync(ConversationState state, string callbackData)
    {
        var type = callbackData switch
        {
            "cert_employment" => CertificateType.EmploymentConfirmation,
            "cert_salary"     => CertificateType.SalaryStatement,
            "cert_tax"        => CertificateType.IncomeTax,
            "cert_experience" => CertificateType.WorkExperience,
            _                 => CertificateType.EmploymentConfirmation
        };

        state.Set("cert_type", type.ToString());
        state.CurrentStep = DialogStep.WaitingCertificateDelivery;

        var typeNameRu = type switch
        {
            CertificateType.EmploymentConfirmation => "Справка с места работы",
            CertificateType.SalaryStatement        => "Справка о зарплате",
            CertificateType.IncomeTax              => "Справка по ИПН/КПН",
            CertificateType.WorkExperience         => "Справка о стаже",
            _                                      => "Справка"
        };

        var text = $"📄 *{typeNameRu}*\n\n" +
                   i18n.Get("certificate.choose_delivery");

        var keyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("📧 На Email / Email-ге", "delivery_email"),
                InlineKeyboardButton.WithCallbackData("🖨️ Бумажная / Қағаз", "delivery_paper")
            ],
            [InlineKeyboardButton.WithCallbackData("⬅️ Назад / Артқа", "certificate.request")]
        ]);

        return await Task.FromResult(BotResponse.Create(text, keyboard));
    }

    public virtual async Task<BotResponse> HandleDeliverySelectedAsync(ConversationState state, string callbackData)
    {
        if (state.EmployeeId == null) return BotResponse.Create(i18n.Get("fallback"));

        var certTypeStr = state.Get("cert_type") ?? "EmploymentConfirmation";
        var certType = Enum.Parse<CertificateType>(certTypeStr);
        var delivery = callbackData == "delivery_email" ? "email" : "paper";
        var deliveryRu = delivery == "email" ? "📧 на Email" : "🖨️ бумажная";
        var deliveryKk = delivery == "email" ? "📧 Email-ге" : "🖨️ қағаз түрінде";

        var req = await hrService.CreateCertificateRequestAsync(state.EmployeeId.Value, certType, delivery);

        var typeNamesRu = new Dictionary<CertificateType, string>
        {
            [CertificateType.EmploymentConfirmation] = "Справка с места работы",
            [CertificateType.SalaryStatement]        = "Справка о зарплате",
            [CertificateType.IncomeTax]              = "Справка по ИПН/КПН",
            [CertificateType.WorkExperience]         = "Справка о стаже"
        };
        var typeNamesKk = new Dictionary<CertificateType, string>
        {
            [CertificateType.EmploymentConfirmation] = "Жұмыс орнынан анықтама",
            [CertificateType.SalaryStatement]        = "Жалақы туралы анықтама",
            [CertificateType.IncomeTax]              = "ЖТС/КТС бойынша анықтама",
            [CertificateType.WorkExperience]         = "Еңбек өтілі туралы анықтама"
        };

        var readyDate = req.EstimatedReadyAt.ToString("dd MMMM yyyy");
        var text = $"✅ Заявка принята! / Өтінім қабылданды!\n\n" +
                   $"📄 *{typeNamesRu[certType]}*\n" +
                   $"📄 *{typeNamesKk[certType]}*\n\n" +
                   $"📅 Готовность / Дайын болу: *{readyDate}*\n" +
                   $"📬 Способ / Тәсіл: {deliveryRu} / {deliveryKk}";

        state.Reset();
        return BotResponse.Create(text, new InlineKeyboardMarkup(
        [[InlineKeyboardButton.WithCallbackData("🏠 Главное меню / Басты мәзір", "main_menu")]]));
    }
}
