using HROpsBot.Core.Dialog;
using HROpsBot.Core.Handlers;
using HROpsBot.Core.Interfaces;
using HROpsBot.Core.Services;
using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Tests;

public class TaskHandlerTests
{
    [Fact]
    public async Task HandleListAsync_WhenNoEmployeeId_ReturnsFallback()
    {
        var i18n = new I18nService();
        var handler = new TaskHandler(new FakeTaskService(), i18n);
        var state = new ConversationState();

        var response = await handler.HandleListAsync(state);

        Assert.Equal(i18n.Get("fallback"), response.Text);
    }

    [Fact]
    public async Task HandleListAsync_WhenNoTasks_ReturnsEmptyStateMessage()
    {
        var i18n = new I18nService();
        var handler = new TaskHandler(new FakeTaskService(), i18n);
        var state = new ConversationState { EmployeeId = 10, CurrentStep = DialogStep.WaitingCsat };

        var response = await handler.HandleListAsync(state);

        Assert.Contains("Нет активных задач", response.Text);
        Assert.Equal(DialogStep.Idle, state.CurrentStep);
        Assert.NotNull(response.Keyboard);
    }

    [Fact]
    public async Task HandleOverdueAsync_WithOverdueTasks_ReturnsOverdueSummary()
    {
        var i18n = new I18nService();
        var service = new FakeTaskService
        {
            Overdue =
            [
                new TaskItem
                {
                    TitleRu = "Закрыть отчет",
                    TitleKk = "Есепті жабу",
                    Deadline = DateTime.UtcNow.AddDays(-2),
                    Status = TaskItemStatus.InProgress
                }
            ]
        };
        var handler = new TaskHandler(service, i18n);
        var state = new ConversationState { EmployeeId = 10 };

        var response = await handler.HandleOverdueAsync(state);

        Assert.Contains("Просроченные задачи", response.Text);
        Assert.Contains("Закрыть отчет", response.Text);
        Assert.NotNull(response.Keyboard);
    }

    private sealed class FakeTaskService : ITaskService
    {
        public List<TaskItem> Active { get; set; } = [];
        public List<TaskItem> Overdue { get; set; } = [];

        public Task<List<TaskItem>> GetActiveTasksAsync(int employeeId) => Task.FromResult(Active);
        public Task<List<TaskItem>> GetOverdueTasksAsync(int employeeId) => Task.FromResult(Overdue);
        public Task<List<TaskItem>> GetAllActiveForNotificationsAsync() => throw new NotImplementedException();
        public Task MarkNotifiedAsync(int taskId) => throw new NotImplementedException();
    }
}
