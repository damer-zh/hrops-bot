using HROpsBot.Core.Dialog;
using HROpsBot.Core.Services;
using HROpsBot.Domain.Entities;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Helpers;
using Telegram.Bot.Types.ReplyMarkups;

namespace HROpsBot.Core.Handlers;

public class EquipmentHandler(IEquipmentService equipmentService, I18nService i18n)
{
    public virtual Task<BotResponse> HandleAsync(ConversationState state, string userText)
    {
        state.CurrentStep = DialogStep.WaitingEquipmentType;
        var text = i18n.Get("equipment.choose_type");

        var keyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("💻 Ноутбук",           "equip_laptop"),
                InlineKeyboardButton.WithCallbackData("🖥️ Монитор",           "equip_monitor")
            ],
            [
                InlineKeyboardButton.WithCallbackData("⌨️ Клавиатура / Пернетақта", "equip_keyboard"),
                InlineKeyboardButton.WithCallbackData("🖱️ Мышь / Тінтуір",   "equip_mouse")
            ],
            [
                InlineKeyboardButton.WithCallbackData("🎧 Гарнитура",         "equip_headset"),
                InlineKeyboardButton.WithCallbackData("📱 Телефон",           "equip_phone")
            ],
            [
                InlineKeyboardButton.WithCallbackData("🪑 Кресло / Орындық", "equip_chair"),
                InlineKeyboardButton.WithCallbackData("📦 Другое / Басқа",    "equip_other")
            ],
            [InlineKeyboardButton.WithCallbackData("⬅️ Назад / Артқа",        "main_menu")]
        ]);

        return Task.FromResult(BotResponse.Create(text, keyboard));
    }

    public virtual async Task<BotResponse> HandleTypeSelectedAsync(ConversationState state, string callbackData)
    {
        if (state.EmployeeId == null) return BotResponse.Create(i18n.Get("fallback"));

        var type = callbackData switch
        {
            "equip_laptop"   => EquipmentType.Laptop,
            "equip_monitor"  => EquipmentType.Monitor,
            "equip_keyboard" => EquipmentType.Keyboard,
            "equip_mouse"    => EquipmentType.Mouse,
            "equip_headset"  => EquipmentType.Headset,
            "equip_phone"    => EquipmentType.Phone,
            "equip_chair"    => EquipmentType.Chair,
            _                => EquipmentType.Other
        };

        var req = await equipmentService.CreateRequestAsync(state.EmployeeId.Value, type);
        var (nameRu, nameKk) = FormatHelper.GetEquipmentTypeName(type);

        var text = $"✅ Заявка создана! / Өтінім жасалды!\n\n" +
                   $"💻 *{nameRu}* / *{nameKk}*\n" +
                   $"🎫 Номер заявки / Өтінім нөмірі: `{req.TicketNumber}`\n" +
                   $"⏱️ Срок обработки / Өңдеу мерзімі: ~3 рабочих дня / жұмыс күні\n\n" +
                   $"_IT-отдел свяжется с вами. / IT-бөлімі сізбен байланысады._";

        state.Reset();
        return BotResponse.Create(text, new InlineKeyboardMarkup([[
            InlineKeyboardButton.WithCallbackData("🏠 Главное меню / Басты мәзір", "main_menu")
        ]]));
    }
}
