using HROpsBot.Core.Helpers;
using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Tests;

public class FormatHelperTests
{
    [Fact]
    public void GetTaskPriorityLabel_High_ReturnsExpectedLabels()
    {
        var result = FormatHelper.GetTaskPriorityLabel(TaskPriority.High);

        Assert.Equal("🟠 Высокий", result.Ru);
        Assert.Equal("🟠 Жоғары", result.Kk);
    }

    [Fact]
    public void GetTaskStatusLabel_UnknownEnum_UsesFallback()
    {
        var result = FormatHelper.GetTaskStatusLabel((TaskItemStatus)999);

        Assert.Equal("❌ Отменено", result.Ru);
        Assert.Equal("❌ Бас тартылды", result.Kk);
    }
}
