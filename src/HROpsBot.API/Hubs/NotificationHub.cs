using Microsoft.AspNetCore.SignalR;

namespace HROpsBot.API.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Сотрудник подключается к SignalR из TMA
        var telegramIdStr = Context.GetHttpContext()?.Request.Query["telegramId"];
        if (long.TryParse(telegramIdStr, out var telegramId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"emp_{telegramId}");
        }

        // Подключение админов в общую группу (опционально)
        var isAdminStr = Context.GetHttpContext()?.Request.Query["isAdmin"];
        if (bool.TryParse(isAdminStr, out var isAdmin) && isAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "hr_admins");
        }

        await base.OnConnectedAsync();
    }
}
