using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class FaqHandler(I18nService i18n)
{
    private static readonly List<(string[] KeywordsRu, string[] KeywordsKk, string AnswerRu, string AnswerKk)> _faq =
    [
        (
            ["больничный", "болею", "болезнь", "листок нетрудоспособности"],
            ["ауру", "науқас", "ауру парағы"],
            "🤒 *Больничный лист:*\n1. Уведомите руководителя и HR в первый день болезни\n2. Предоставьте больничный лист в HR в течение 3 рабочих дней после выздоровления\n3. Оплата — согласно ТК РК",
            "🤒 *Ауру парағы:*\n1. Ауырған күні жетекші мен HR-ға хабарлаңыз\n2. Жазылғаннан кейін 3 жұмыс күні ішінде ауру парағын HR-ға тапсырыңыз\n3. Төлем — ҚР Еңбек кодексіне сәйкес"
        ),
        (
            ["kpi", "кпи", "оценка", "результат", "показатели"],
            ["kpi", "кпи", "баға", "нәтиже", "көрсеткіш"],
            "📊 *KPI и оценка:*\nОценка KPI проводится раз в квартал. Результаты обсуждаются с руководителем на 1-on-1. Бонус привязан к выполнению KPI на 100%+",
            "📊 *KPI және баға:*\nKPI бағасы тоқсанына бір рет жүргізіледі. Нәтижелер жетекшімен 1-on-1 кездесуде талқыланады. Бонус KPI 100%+ орындалғанда беріледі"
        ),
        (
            ["зарплата", "оклад", "аванс", "выплата", "деньги"],
            ["жалақы", "айлық", "аванс", "төлем", "ақша"],
            "💰 *Зарплата:*\nАванс — 25-го числа каждого месяца\nОсновная выплата — 10-го числа следующего месяца\nПо вопросам — обратитесь в бухгалтерию",
            "💰 *Жалақы:*\nАванс — ай сайын 25-інде\nНегізгі төлем — келесі айдың 10-ында\nСұрақтар бойынша — бухгалтерияға хабарласыңыз"
        ),
        (
            ["испытательный", "испытание", "стажировка", "пробный"],
            ["сынақ", "тәжірибе", "стажировка"],
            "📋 *Испытательный срок:*\n3 месяца для всех новых сотрудников. За 2 недели до окончания — встреча с HR и руководителем для оценки. Досрочное завершение возможно при отличных показателях",
            "📋 *Сынақ мерзімі:*\nБарлық жаңа қызметкерлер үшін 3 ай. Аяқталуына 2 апта қалғанда — HR мен жетекшімен кездесу. Тамаша нәтижелерде мерзімінен бұрын аяқтауға болады"
        )
    ];

    public virtual Task<BotResponse> HandleAsync(ConversationState state, string userText)
    {
        var lowerText = userText.ToLower();

        // Ищем подходящий FAQ
        var match = _faq.FirstOrDefault(f =>
            f.KeywordsRu.Any(k => lowerText.Contains(k)) ||
            f.KeywordsKk.Any(k => lowerText.Contains(k)));

        string responseText;
        if (match.AnswerRu != null)
        {
            responseText = $"{match.AnswerRu}\n\n{match.AnswerKk}";
        }
        else
        {
            // Показываем общее меню FAQ
            responseText = "❓ Часто задаваемые вопросы / Жиі қойылатын сұрақтар:";
        }

        var keyboard = match.AnswerRu != null
            ? new InlineKeyboardMarkup([[
                InlineKeyboardButton.WithCallbackData("❓ Другой вопрос / Басқа сұрақ", "faq.general"),
                InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
              ]])
            : new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithCallbackData("🤒 Больничный / Ауру парағы", "faq_sick")],
                [InlineKeyboardButton.WithCallbackData("📊 KPI и оценка / KPI және баға", "faq_kpi")],
                [InlineKeyboardButton.WithCallbackData("💰 Зарплата / Жалақы", "faq_salary")],
                [InlineKeyboardButton.WithCallbackData("📋 Испытательный срок / Сынақ мерзімі", "faq_probation")],
                [InlineKeyboardButton.WithCallbackData("🏠 Главное меню / Басты мәзір", "main_menu")]
              ]);

        state.Reset();
        return Task.FromResult(BotResponse.Create(responseText, keyboard));
    }

    public virtual Task<BotResponse> HandleFaqItemAsync(ConversationState state, string item)
    {
        var index = item switch
        {
            "faq_sick"      => 0,
            "faq_kpi"       => 1,
            "faq_salary"    => 2,
            "faq_probation" => 3,
            _               => -1
        };

        if (index < 0 || index >= _faq.Count)
            return Task.FromResult(BotResponse.Create(i18n.Get("fallback")));

        var faq = _faq[index];
        var text = $"{faq.AnswerRu}\n\n{faq.AnswerKk}";

        return Task.FromResult(BotResponse.Create(text, new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("⬅️ Назад / Артқа", "faq.general"),
            InlineKeyboardButton.WithCallbackData("🏠 Меню", "main_menu")
        ]])));
    }
}
