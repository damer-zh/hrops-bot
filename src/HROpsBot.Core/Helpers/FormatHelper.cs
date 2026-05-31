using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Helpers;

public static class FormatHelper
{
    public static (string Ru, string Kk) GetTaskPriorityLabel(TaskPriority priority) =>
        priority switch
        {
            TaskPriority.Critical => ("🔴 Критичный", "🔴 Маңызды"),
            TaskPriority.High     => ("🟠 Высокий",   "🟠 Жоғары"),
            TaskPriority.Medium   => ("🟡 Средний",   "🟡 Орташа"),
            TaskPriority.Low      => ("🟢 Низкий",    "🟢 Төмен"),
            _                     => ("⚪ Обычный",   "⚪ Қалыпты")
        };

    public static (string Ru, string Kk) GetTaskStatusLabel(TaskItemStatus status) =>
        status switch
        {
            TaskItemStatus.Todo       => ("📋 К выполнению", "📋 Орындалуы керек"),
            TaskItemStatus.InProgress => ("⚙️ В работе",     "⚙️ Орындалуда"),
            TaskItemStatus.Review     => ("👀 На проверке",  "👀 Тексерілуде"),
            TaskItemStatus.Done       => ("✅ Выполнено",    "✅ Орындалды"),
            _                         => ("❌ Отменено",     "❌ Бас тартылды")
        };

    public static (string Ru, string Kk) GetEquipmentTypeName(EquipmentType type) =>
        type switch
        {
            EquipmentType.Laptop   => ("Ноутбук", "Ноутбук"),
            EquipmentType.Monitor  => ("Монитор", "Монитор"),
            EquipmentType.Keyboard => ("Клавиатура", "Пернетақта"),
            EquipmentType.Mouse    => ("Мышь", "Тінтуір"),
            EquipmentType.Headset  => ("Гарнитура", "Гарнитура"),
            EquipmentType.Phone    => ("Телефон", "Телефон"),
            EquipmentType.Chair    => ("Кресло", "Орындық"),
            EquipmentType.Desk     => ("Стол", "Үстел"),
            _                      => ("Оборудование", "Жабдық")
        };
}
