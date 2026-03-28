namespace Ikon.App.Examples.Learning.States;

public class ExerciseMenuState(LearningApp outer) : ILearningState
{
    // Theme data with descriptions for image generation (descriptions in English for image generation prompts)
    private static readonly (string Id, string Name, string Description)[] Themes =
    [
        ("everyday-conversations", "Arkikeskustelut", "Two people having a friendly chat at a coffee shop in Helsinki"),
        ("at-the-workplace", "Työpaikalla", "A professional office setting with colleagues collaborating"),
        ("shopping-and-services", "Ostokset ja palvelut", "A customer at a Finnish grocery store or market"),
        ("travel-and-transport", "Matkailu ja liikenne", "A train station or bus stop in a Nordic city")
    ];

    // State
    private readonly Reactive<(string Id, string Name, string Description)?> _selectedTheme = new(null);
    private readonly Reactive<List<Scenario>> _scenarios = new([]);
    private readonly Reactive<bool> _isLoadingScenarios = new(false);
    private readonly Reactive<bool> _isGeneratingExercise = new(false);

    // Image caches
    private readonly Dictionary<string, string> _themeImages = new();
    private readonly Dictionary<string, string> _scenarioImages = new();
    private readonly Reactive<int> _imagesVersion = new(0);
    private bool _themeImagesLoading = false;

    public Task EnterAsync()
    {
        _selectedTheme.Value = null;
        _scenarios.Value = [];
        LoadThemeImages();
        return Task.CompletedTask;
    }

    private void LoadThemeImages()
    {
        if (_themeImagesLoading)
        {
            return;
        }

        _themeImagesLoading = true;

        _ = Task.Run(async () =>
        {
            foreach (var theme in Themes)
            {
                if (_themeImages.ContainsKey(theme.Id))
                {
                    continue;
                }

                try
                {
                    var imageUrl = await outer.GetOrCreateImageAsync(
                        "themes",
                        theme.Id,
                        theme.Description,
                        width: 400,
                        height: 200
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _themeImages[theme.Id] = imageUrl;
                        _imagesVersion.Value++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warning($"Failed to load image for theme {theme.Name}: {ex.Message}");
                }
            }
        });
    }

    private async Task SelectThemeAsync((string Id, string Name, string Description) theme)
    {
        _selectedTheme.Value = theme;
        _isLoadingScenarios.Value = true;
        _scenarios.Value = [];

        try
        {
            var scenarios = await LoadOrGenerateScenariosAsync(theme);
            _scenarios.Value = scenarios;
            LoadScenarioImages(scenarios);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to load scenarios for theme {theme.Name}: {ex.Message}");
            _scenarios.Value = [];
        }
        finally
        {
            _isLoadingScenarios.Value = false;
        }
    }

    private async Task<List<Scenario>> LoadOrGenerateScenariosAsync((string Id, string Name, string Description) theme)
    {
        var scenarios = await outer.GetThemeScenariosAsync(theme.Id);

        if (scenarios.Count >= 5)
        {
            return scenarios;
        }

        Log.Instance.Info($"Generating scenarios for theme {theme.Name}");

        var targetLanguage = outer.UserState?.TargetLanguage ?? "Finnish";
        var languageLevel = outer.UserState?.CurrentLanguageLevel ?? "A1";

        var generated = await GenerateScenariosShader.GenerateAsync(
            LLMModel.Gpt41.ToString(),
            nameof(ReasoningEffort.None),
            theme.Name,
            theme.Description,
            targetLanguage,
            languageLevel,
            scenarios,
            count: 5 - scenarios.Count
        );

        foreach (var scenario in generated.Scenarios)
        {
            scenario.ThemeId = theme.Id;
        }

        scenarios.AddRange(generated.Scenarios);
        await outer.SaveThemeScenariosAsync(theme.Id, scenarios);

        return scenarios;
    }

    private void LoadScenarioImages(List<Scenario> scenarios)
    {
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
                    var imageUrl = await outer.GetOrCreateImageAsync(
                        "scenarios",
                        scenario.Id,
                        $"Illustration for language learning scenario: {scenario.Description}",
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
                    Log.Instance.Warning($"Failed to load image for scenario {scenario.Name}: {ex.Message}");
                }
            }
        });
    }

    private async Task SelectScenarioAsync(Scenario scenario)
    {
        _isGeneratingExercise.Value = true;

        try
        {
            var exercise = await GenerateExerciseShader.GenerateAsync(
                LLMModel.Gpt41.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState!,
                null,
                null,
                null,
                null,
                null,
                $"Theme: {_selectedTheme.Value?.Name}\nScenario: {scenario.Name}\nDescription: {scenario.Description}",
                ExerciseType.Conversational,
                ExerciseSource.Content,
                ExerciseCategory.Assignment
            );

            outer.CurrentExercise = exercise;
            await outer.States.StateMachine.FireAsync(Trigger.StartExercise);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to generate exercise: {ex.Message}");
        }
        finally
        {
            _isGeneratingExercise.Value = false;
        }
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public Task<bool> HandleBackAsync()
    {
        // If a theme is selected, go back to theme selection
        if (_selectedTheme.Value != null)
        {
            _selectedTheme.Value = null;
            _scenarios.Value = [];
            return Task.FromResult(true); // Handled internally
        }

        return Task.FromResult(false); // Go to main menu
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
        var selectedTheme = _selectedTheme.Value;
        var _ = _imagesVersion.Value; // Read to trigger re-render when images load

        contentView.Column(["gap-4 md:gap-5"], content: view =>
        {
            if (selectedTheme == null)
            {
                RenderThemeSelection(view, translations);
            }
            else
            {
                RenderScenarioSelection(view, translations, selectedTheme.Value);
            }
        });
    }

    private void RenderThemeSelection(UIView view, Translations translations)
    {
        // Header section - compact
        view.Box([LearningApp.Styles.GlassCardStrong, "p-4 md:p-5 rounded-2xl"], content: headerView =>
        {
            headerView.Column(["gap-1"], content: col =>
            {
                col.Text(["text-xl md:text-2xl font-semibold text-[#1a1a1a]"], translations.LetsPractice);
                col.Text(["text-sm md:text-base text-[#6b7280]"], "Valitse teema aloittaaksesi harjoittelun");
            });
        });

        // Theme cards - cleaner design
        view.Column(["gap-3 md:gap-4"], content: themesView =>
        {
            foreach (var theme in Themes)
            {
                var currentTheme = theme;
                var hasImage = _themeImages.TryGetValue(theme.Id, out var imageUrl);

                themesView.Button(["overflow-hidden text-left w-full bg-white/90 backdrop-blur-md rounded-2xl shadow-sm border border-white/50 hover:shadow-md hover:bg-white/95 transition-all",
                    "motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-200ms"],
                    onClick: async () => await SelectThemeAsync(currentTheme),
                    content: cardView =>
                    {
                        cardView.Column([], content: col =>
                        {
                            // Image section - responsive height
                            if (hasImage && !string.IsNullOrEmpty(imageUrl))
                            {
                                col.Image(["w-full h-28 md:h-36 object-cover"], src: imageUrl, alt: theme.Name);
                            }
                            else
                            {
                                col.Box([$"w-full h-28 md:h-36 {LearningApp.Styles.GetAccentGradient(outer.SelectedTheme.Value)} opacity-20 flex items-center justify-center"], content: placeholder =>
                                {
                                    placeholder.Icon([Icon.Default, "w-10 h-10 md:w-12 md:h-12 text-white/60"], name: "image");
                                });
                            }

                            // Content section
                            col.Row(["p-3 md:p-4 justify-between items-center"], content: rowView =>
                            {
                                rowView.Column(["gap-0.5 flex-1"], content: textCol =>
                                {
                                    textCol.Text(["text-base md:text-lg font-semibold text-[#1a1a1a]"], theme.Name);
                                    textCol.Text(["text-xs md:text-sm text-[#6b7280]"], "Harjoittele yleisiä tilanteita");
                                });
                                rowView.Icon([Icon.Default, "text-[#9ca3af] w-5 h-5"], name: "chevron-right");
                            });
                        });
                    });
            }
        });
    }

    private void RenderScenarioSelection(UIView view, Translations translations, (string Id, string Name, string Description) theme)
    {
        var appTheme = outer.SelectedTheme.Value;

        // Header with back button - cleaner
        view.Box([LearningApp.Styles.GlassCardStrong, "p-4 md:p-5 rounded-2xl"], content: headerView =>
        {
            headerView.Column(["gap-3"], content: col =>
            {
                col.Button(["self-start text-sm font-medium text-[#6b7280] hover:text-[#1a1a1a] px-0 py-0"],
                    label: "← Takaisin teemoihin",
                    onClick: () =>
                    {
                        _selectedTheme.Value = null;
                        _scenarios.Value = [];
                        return Task.CompletedTask;
                    });

                col.Column(["gap-1"], content: titleCol =>
                {
                    titleCol.Text(["text-xl md:text-2xl font-semibold text-[#1a1a1a]"], theme.Name);
                    titleCol.Text(["text-sm md:text-base text-[#6b7280]"], "Valitse harjoiteltava tilanne");
                });
            });
        });

        if (_isLoadingScenarios.Value)
        {
            view.Box([LearningApp.Styles.GlassCard, "p-8 rounded-2xl text-center"], content: loadingView =>
            {
                loadingView.Column(["items-center gap-3"], content: col =>
                {
                    col.Icon([Icon.Default, $"w-10 h-10 {LearningApp.Styles.GetAccentText(appTheme)} animate-spin"], name: "loader");
                    col.Text(["text-sm text-[#6b7280]"], "Ladataan tilanteita...");
                });
            });
        }
        else if (_isGeneratingExercise.Value)
        {
            view.Box([LearningApp.Styles.GlassCard, "p-8 rounded-2xl text-center"], content: loadingView =>
            {
                loadingView.Column(["items-center gap-3"], content: col =>
                {
                    col.Icon([Icon.Default, $"w-10 h-10 {LearningApp.Styles.GetAccentText(appTheme)} animate-spin"], name: "loader");
                    col.Text(["text-sm text-[#6b7280]"], translations.OrganizingContent);
                });
            });
        }
        else if (_scenarios.Value.Count == 0)
        {
            view.Box([LearningApp.Styles.GlassCard, "p-8 rounded-2xl text-center"], content: emptyView =>
            {
                emptyView.Column(["items-center gap-3"], content: col =>
                {
                    col.Icon([Icon.Default, "w-12 h-12 text-[#d1d5db]"], name: "folder-open");
                    col.Text(["text-sm text-[#6b7280]"], "Ei tilanteita saatavilla");
                });
            });
        }
        else
        {
            // Scenario cards - compact and clean
            view.Column(["gap-3"], content: scenariosView =>
            {
                foreach (var scenario in _scenarios.Value)
                {
                    var currentScenario = scenario;
                    var hasImage = _scenarioImages.TryGetValue(scenario.Id, out var imageUrl);

                    scenariosView.Button(["overflow-hidden text-left w-full bg-white/90 backdrop-blur-md rounded-2xl shadow-sm border border-white/50 hover:shadow-md hover:bg-white/95 transition-all",
                        "motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-200ms"],
                        onClick: async () => await SelectScenarioAsync(currentScenario),
                        content: cardView =>
                        {
                            cardView.Row([], content: row =>
                            {
                                // Image section (left side) - smaller on mobile
                                if (hasImage && !string.IsNullOrEmpty(imageUrl))
                                {
                                    row.Image(["w-20 h-20 md:w-24 md:h-24 object-cover"], src: imageUrl, alt: scenario.Name);
                                }
                                else
                                {
                                    row.Box([$"w-20 h-20 md:w-24 md:h-24 {LearningApp.Styles.GetAccentGradient(appTheme)} opacity-20 flex items-center justify-center"], content: placeholder =>
                                    {
                                        placeholder.Icon([Icon.Default, "w-6 h-6 md:w-8 md:h-8 text-white/60"], name: "message-circle");
                                    });
                                }

                                // Content section
                                row.Row(["flex-1 p-3 md:p-4 justify-between items-center"], content: contentRow =>
                                {
                                    contentRow.Column(["gap-0.5 flex-1 min-w-0"], content: textCol =>
                                    {
                                        textCol.Text(["text-sm md:text-base font-semibold text-[#1a1a1a] truncate"], scenario.Name);
                                        textCol.Text(["text-xs md:text-sm text-[#6b7280] line-clamp-2"], scenario.Description);
                                    });
                                    contentRow.Icon([Icon.Default, "text-[#9ca3af] w-4 h-4 md:w-5 md:h-5 ml-2"], name: "chevron-right");
                                });
                            });
                        });
                }
            });
        }
    }
}
