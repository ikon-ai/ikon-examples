namespace Ikon.App.Examples.Learning.States;

public interface ILearningState
{
    Task EnterAsync();
    Task ExitAsync();
    Task HandleUserMessageAsync(string userId, string text);
    Task HandleAIMessageAsync(string message);
    void Render(UIView contentView);

    /// <summary>
    /// Handle back navigation. Returns true if handled internally, false to go to main menu.
    /// </summary>
    Task<bool> HandleBackAsync() => Task.FromResult(false);
}

public class NullState : ILearningState
{
    public Task EnterAsync() => Task.CompletedTask;
    public Task ExitAsync() => Task.CompletedTask;
    public Task HandleUserMessageAsync(string userId, string text) => Task.CompletedTask;
    public Task HandleAIMessageAsync(string message) => Task.CompletedTask;
    public void Render(UIView contentView) { }
}
