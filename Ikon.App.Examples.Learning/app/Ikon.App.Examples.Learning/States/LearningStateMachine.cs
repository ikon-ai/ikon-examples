namespace Ikon.App.Examples.Learning.States;

public enum LearningState
{
    MainMenu,
    ExerciseMenu,
    Exercise,
    YourProgress,
    News,
    CreateExercise,
    YourScenarios,
    Chat,
    Settings,
    Null
}

public enum Trigger
{
    SelectExerciseMenu,
    StartExercise,
    YourProgress,
    News,
    CreateExercise,
    YourScenarios,
    Chat,
    Settings,
    ReturnToMainMenu
}

public class LearningStateMachineManager
{
    public StateMachine<LearningState, Trigger> StateMachine { get; }
    public ILearningState CurrentState { get; set; }

    private readonly ILearningState _mainMenuState;
    private readonly ILearningState _exerciseMenuState;
    private readonly ILearningState _exerciseState;
    private readonly ILearningState _yourProgressState;
    private readonly ILearningState _newsState;
    private readonly ILearningState _createExerciseState;
    private readonly ILearningState _yourScenariosState;
    private readonly ILearningState _chatState;
    private readonly ILearningState _settingsState;
    private readonly LearningApp _outer;

    public LearningStateMachineManager(LearningApp outer)
    {
        _outer = outer;
        _mainMenuState = new MainMenuState(outer);
        _exerciseMenuState = new ExerciseMenuState(outer);
        _exerciseState = new ExerciseState(outer);
        _yourProgressState = new YourProgressState(outer);
        _newsState = new NewsState(outer);
        _createExerciseState = new CreateExerciseState(outer);
        _yourScenariosState = new YourScenariosState(outer);
        _chatState = new ChatState(outer);
        _settingsState = new SettingsState(outer);

        CurrentState = new NullState();

        StateMachine = new StateMachine<LearningState, Trigger>(LearningState.Null);

        async Task OnEntry(ILearningState state)
        {
            CurrentState = state;
            await CurrentState.EnterAsync();
        }

        async Task OnExit()
        {
            if (CurrentState != null)
            {
                await CurrentState.ExitAsync();
            }
        }

        StateMachine.Configure(LearningState.Null)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu);

        StateMachine.Configure(LearningState.MainMenu)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Permit(Trigger.StartExercise, LearningState.Exercise)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Ignore(Trigger.ReturnToMainMenu)
            .OnEntryAsync(async () => await OnEntry(_mainMenuState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.YourProgress)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Ignore(Trigger.YourProgress)
            .OnEntryAsync(async () => await OnEntry(_yourProgressState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.News)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.StartExercise, LearningState.Exercise)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Ignore(Trigger.News)
            .OnEntryAsync(async () => await OnEntry(_newsState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.YourScenarios)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.CreateExercise, LearningState.CreateExercise)
            .Permit(Trigger.StartExercise, LearningState.Exercise)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Ignore(Trigger.YourScenarios)
            .OnEntryAsync(async () => await OnEntry(_yourScenariosState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.CreateExercise)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.StartExercise, LearningState.Exercise)
            .OnEntryAsync(async () => await OnEntry(_createExerciseState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.Chat)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Ignore(Trigger.Chat)
            .OnEntryAsync(async () => await OnEntry(_chatState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.Settings)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Ignore(Trigger.Settings)
            .OnEntryAsync(async () => await OnEntry(_settingsState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.ExerciseMenu)
            .Permit(Trigger.StartExercise, LearningState.Exercise)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Ignore(Trigger.SelectExerciseMenu)
            .OnEntryAsync(async () => await OnEntry(_exerciseMenuState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.Configure(LearningState.Exercise)
            .Permit(Trigger.SelectExerciseMenu, LearningState.ExerciseMenu)
            .Permit(Trigger.ReturnToMainMenu, LearningState.MainMenu)
            .Permit(Trigger.YourScenarios, LearningState.YourScenarios)
            .Permit(Trigger.News, LearningState.News)
            .Permit(Trigger.YourProgress, LearningState.YourProgress)
            .Permit(Trigger.Chat, LearningState.Chat)
            .Permit(Trigger.Settings, LearningState.Settings)
            .Ignore(Trigger.StartExercise)
            .OnEntryAsync(async () => await OnEntry(_exerciseState))
            .OnExitAsync(async () => await OnExit());

        StateMachine.OnTransitionedAsync(async transition =>
        {
            Log.Instance.Debug($"Changing state from {transition.Source} to {transition.Destination}");
            _outer.CurrentStateReactive.Value = transition.Destination;
        });
    }
}
