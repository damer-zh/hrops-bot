using HROpsBot.Infrastructure.Telegram;
using HROpsBot.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HROpsBot.API.BackgroundServices;

public class TaskPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<TaskPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TaskPollingService started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollAndNotifyAsync();
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PollAndNotifyAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
            var hrService = scope.ServiceProvider.GetRequiredService<IHrService>();
            var botAdapter = scope.ServiceProvider.GetRequiredService<TelegramBotAdapter>();

            var tasks = await taskService.GetAllActiveForNotificationsAsync();

            foreach (var task in tasks)
            {
                var employee = await hrService.GetEmployeeByIdAsync(task.EmployeeId);
                if (employee == null) continue;

                var daysLeft = task.Deadline.HasValue
                    ? (task.Deadline.Value - DateTime.UtcNow).TotalDays
                    : 0;

                string messageRu, messageKk;
                if (task.IsOverdue)
                {
                    var daysOverdue = Math.Abs((int)daysLeft);
                    messageRu = $"🔴 *Просроченная задача!*\n\n*{task.TitleRu}*\nПросрочено на {daysOverdue} дн.";
                    messageKk = $"🔴 *Мерзімі өткен тапсырма!*\n\n*{task.TitleKk}*\n{daysOverdue} күнге кешіктірілді.";
                }
                else
                {
                    var hoursLeft = (int)(daysLeft * 24);
                    messageRu = $"⚠️ *Дедлайн скоро!*\n\n*{task.TitleRu}*\nОсталось: ~{hoursLeft} ч.";
                    messageKk = $"⚠️ *Мерзім жақындады!*\n\n*{task.TitleKk}*\nҚалды: ~{hoursLeft} сағ.";
                }

                var fullMessage = $"{messageRu}\n\n{messageKk}";

                try
                {
                    await botAdapter.SendNotificationAsync(employee.TelegramId, fullMessage);
                    await taskService.MarkNotifiedAsync(task.Id);
                    logger.LogInformation("Notified employee {EmployeeId} about task {TaskId}",
                        task.EmployeeId, task.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to notify employee {EmployeeId}", task.EmployeeId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TaskPollingService error");
        }
    }
}
