using HROpsBot.Core.Dialog;

namespace HROpsBot.Core.Tests;

public class ConversationStateTests
{
    [Fact]
    public void Reset_ClearsStateAndContext()
    {
        var state = new ConversationState
        {
            CurrentStep = DialogStep.WaitingAppointmentSlot,
            PendingIntent = "task.list",
            WaitingForCsat = true
        };
        state.Set("x", "y");

        state.Reset();

        Assert.Equal(DialogStep.Idle, state.CurrentStep);
        Assert.Null(state.PendingIntent);
        Assert.False(state.WaitingForCsat);
        Assert.Empty(state.Context);
    }
}
