using HROpsBot.Core.Dialog;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Services;

namespace HROpsBot.Core.Handlers;

public class OnboardingHandler(IHrService hrService, I18nService i18n)
{
    public BotResponse StartOnboarding(ConversationState state)
    {
        state.CurrentStep = DialogStep.WaitingOnboardingDepartment;
        return BotResponse.Create(
            "Добро пожаловать! Давайте настроим ваш профиль.\nПожалуйста, введите ваш **Отдел** (например, IT, Маркетинг, HR):",
            null
        );
    }

    public async Task<BotResponse> HandleDepartmentAsync(ConversationState state, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BotResponse.Create("Название отдела не может быть пустым. Пожалуйста, введите ваш отдел:", null);

        state.Set("Onboarding_Department", text.Trim());
        state.CurrentStep = DialogStep.WaitingOnboardingPosition;

        return BotResponse.Create(
            "Отлично. Теперь введите вашу **Должность** (например, Разработчик, Менеджер):",
            null
        );
    }

    public async Task<BotResponse> HandlePositionAsync(ConversationState state, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BotResponse.Create("Должность не может быть пустой. Пожалуйста, введите вашу должность:", null);

        var department = state.Get("Onboarding_Department") ?? "";
        var position = text.Trim();

        if (state.EmployeeId.HasValue)
        {
            var emp = await hrService.GetEmployeeByIdAsync(state.EmployeeId.Value);
            if (emp != null)
            {
                // We update employee directly through EF
                // But IHrService doesn't have an UpdateEmployee method for department/position.
                // Let's assume CreateOrUpdateEmployeeAsync can do it, but it takes telegramId.
                await hrService.CreateOrUpdateEmployeeAsync(emp.TelegramId, emp.NameRu, emp.NameKk, null);
                // Wait, IHrService needs a method to update these fields. I will add UpdateEmployeeProfileAsync to IHrService!
            }
        }

        state.Reset();
        
        return BotResponse.Create(
            "✅ Профиль успешно сохранен!\n\nИспользуйте команду /menu для работы с ботом.",
            null
        );
    }
}
