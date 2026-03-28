namespace Ikon.App.Examples.Learning.States;

public class SettingsState(LearningApp outer) : ILearningState
{
    public async Task EnterAsync()
    {
        // Settings is now a modal overlay, redirect back to main menu
        // and open the settings panel
        outer.ShowSettingsPanel.Value = true;
        await outer.States.StateMachine.FireAsync(Trigger.ReturnToMainMenu);
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public Task HandleUserMessageAsync(string userId, string text)
    {
        return Task.CompletedTask;
    }

    public Task HandleAIMessageAsync(string message)
    {
        return Task.CompletedTask;
    }

    public void Render(UIView contentView)
    {
        // Settings is rendered as a modal overlay, not as a state
    }
}
