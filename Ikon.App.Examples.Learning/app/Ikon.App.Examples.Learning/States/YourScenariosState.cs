namespace Ikon.App.Examples.Learning.States;

public class YourScenariosState(LearningApp outer) : ILearningState
{
    private readonly Dictionary<string, string> _scenarioImages = new();
    private readonly Reactive<int> _imagesVersion = new(0);
    private bool _imagesLoading = false;

    public Task EnterAsync()
    {
        LoadScenarioImages();
        return Task.CompletedTask;
    }

    private void LoadScenarioImages()
    {
        var scenarios = outer.UserState?.CreatedExercises ?? [];

        if (scenarios.Count == 0 || _imagesLoading)
        {
            return;
        }

        _imagesLoading = true;

        _ = Task.Run(async () =>
        {
            foreach (var scenario in scenarios)
            {
                if (_scenarioImages.ContainsKey(scenario.Id))
                {
                    continue;
                }

                try
                {
                    var description = $"Illustration for language learning scenario: {scenario.Scenario}";
                    var imageUrl = await outer.GetOrCreateImageAsync(
                        "scenarios",
                        scenario.Id,
                        description,
                        width: 400,
                        height: 200
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _scenarioImages[scenario.Id] = imageUrl;
                        _imagesVersion.Value++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warning($"Failed to load image for scenario {scenario.Id}: {ex.Message}");
                }
            }
        });
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
        var translations = outer.Translations;
        var userState = outer.UserState;
        var scenarios = userState?.CreatedExercises ?? [];
        var theme = outer.SelectedTheme.Value;
        var _ = _imagesVersion.Value;

        contentView.Column(["gap-4 md:gap-5 px-3 md:px-0"], content: view =>
        {
            // Header section
            view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: headerView =>
            {
                headerView.Row(["justify-between items-center gap-4"], content: row =>
                {
                    row.Row(["items-center gap-4"], content: leftRow =>
                    {
                        leftRow.Box([$"w-11 h-11 md:w-12 md:h-12 {LearningApp.Styles.GetAccentGradient(theme)} rounded-full flex items-center justify-center shadow-md"], content: iconBox =>
                        {
                            iconBox.Icon([Icon.Default, "w-5 h-5 md:w-6 md:h-6 text-white"], name: "theater");
                        });
                        leftRow.Column(["gap-0.5"], content: textCol =>
                        {
                            textCol.Text(["text-lg md:text-xl font-semibold text-[#1a1a1a]"], translations.YourScenarios);
                            textCol.Text(["text-xs md:text-sm text-[#6b7280]"], $"{scenarios.Count} custom scenarios");
                        });
                    });
                    row.Button([$"{LearningApp.Styles.GetAccentGradient(theme)} text-white px-4 py-2.5 rounded-xl font-medium text-sm shadow-md hover:shadow-lg transition-all duration-200"],
                        label: $"+ {translations.CreateScenario}",
                        onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.CreateExercise));
                });
            });

            if (scenarios.Count == 0)
            {
                // Empty state
                view.Box([LearningApp.Styles.GlassCardStrong, "p-8 md:p-10 rounded-3xl text-center"], content: emptyView =>
                {
                    emptyView.Column(["items-center gap-4"], content: col =>
                    {
                        col.Box(["w-20 h-20 rounded-full bg-gray-100 flex items-center justify-center"], content: iconWrapper =>
                        {
                            iconWrapper.Icon([Icon.Default, "w-10 h-10 text-gray-300"], name: "folder-open");
                        });
                        col.Text(["text-base text-[#6b7280]"], translations.NoScenariosFound);
                        col.Text(["text-sm text-[#9ca3af]"], "Create your first custom practice scenario");
                        col.Button([$"{LearningApp.Styles.GetAccentGradient(theme)} text-white px-6 py-3 rounded-xl font-semibold text-base shadow-lg hover:shadow-xl transition-all duration-200 mt-2"],
                            label: translations.CreateScenario,
                            onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.CreateExercise));
                    });
                });
            }
            else
            {
                // Scenario cards
                view.Column(["gap-3"], content: scenariosView =>
                {
                    foreach (var scenario in scenarios)
                    {
                        var currentScenario = scenario;
                        var hasImage = _scenarioImages.TryGetValue(scenario.Id, out var imageUrl);

                        scenariosView.Button([LearningApp.Styles.GlassCard, "overflow-hidden text-left w-full rounded-2xl hover:shadow-lg transition-all duration-200 hover:bg-white/90"],
                            onClick: async () =>
                            {
                                outer.CurrentExercise = currentScenario;
                                await outer.States.StateMachine.FireAsync(Trigger.StartExercise);
                            },
                            content: cardView =>
                            {
                                cardView.Row([], content: row =>
                                {
                                    // Image section (left side)
                                    if (hasImage && !string.IsNullOrEmpty(imageUrl))
                                    {
                                        row.Image(["w-24 h-24 md:w-28 md:h-28 object-cover"], src: imageUrl, alt: scenario.Name);
                                    }
                                    else
                                    {
                                        row.Box([$"w-24 h-24 md:w-28 md:h-28 {LearningApp.Styles.GetGoalsCard(theme)} flex items-center justify-center"], content: placeholder =>
                                        {
                                            placeholder.Icon([Icon.Default, $"w-8 h-8 {LearningApp.Styles.GetAccentText(theme)}/40"], name: "file-text");
                                        });
                                    }

                                    // Content section
                                    row.Row(["flex-1 p-4 justify-between items-center"], content: contentRow =>
                                    {
                                        contentRow.Column(["gap-1.5 flex-1 min-w-0"], content: col =>
                                        {
                                            col.Text(["text-base font-semibold text-[#1a1a1a] truncate"], scenario.Name);
                                            col.Text(["text-sm text-[#6b7280] line-clamp-2"], scenario.Scenario);
                                        });
                                        contentRow.Icon([Icon.Default, "text-gray-400 ml-2"], name: "chevron-right");
                                    });
                                });
                            });
                    }
                });
            }
        });
    }
}
