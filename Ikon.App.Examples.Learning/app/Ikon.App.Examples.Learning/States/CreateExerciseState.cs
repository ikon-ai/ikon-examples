namespace Ikon.App.Examples.Learning.States;

public class CreateExerciseState(LearningApp outer) : ILearningState
{
    private readonly Reactive<string> _scenarioDescription = new("");
    private readonly Reactive<bool> _isGenerating = new(false);
    private readonly Reactive<string?> _error = new(null);

    public Task EnterAsync()
    {
        _scenarioDescription.Value = "";
        _isGenerating.Value = false;
        _error.Value = null;

        outer.TranscriptionCallback = transcribedText =>
        {
            if (string.IsNullOrWhiteSpace(_scenarioDescription.Value))
            {
                _scenarioDescription.Value = transcribedText;
            }
            else
            {
                _scenarioDescription.Value += " " + transcribedText;
            }
        };

        return Task.CompletedTask;
    }

    public Task ExitAsync()
    {
        outer.TranscriptionCallback = null;
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

    private async Task GenerateExercise(string scenario)
    {
        _isGenerating.Value = true;
        _error.Value = null;

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
                scenario,
                ExerciseType.Conversational,
                ExerciseSource.Custom,
                ExerciseCategory.Assignment
            );

            if (exercise == null || string.IsNullOrEmpty(exercise.Id) || string.IsNullOrEmpty(exercise.Name))
            {
                _error.Value = "Failed to generate exercise. Please try again.";
                _isGenerating.Value = false;
                return;
            }

            outer.UserState?.CreatedExercises.Add(exercise);
            outer.CurrentExercise = exercise;

            await outer.States.StateMachine.FireAsync(Trigger.StartExercise);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error generating exercise: {ex.Message}");
            _error.Value = "An error occurred. Please try again.";
            _isGenerating.Value = false;
        }
    }

    public void Render(UIView contentView)
    {
        var translations = outer.Translations;
        var theme = outer.SelectedTheme.Value;

        contentView.Column(["gap-4 md:gap-5 px-3 md:px-0"], content: view =>
        {
            // Header section
            view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: headerView =>
            {
                headerView.Row(["items-center gap-4"], content: row =>
                {
                    row.Box([$"w-11 h-11 md:w-12 md:h-12 {LearningApp.Styles.GetAccentGradient(theme)} rounded-full flex items-center justify-center shadow-md"], content: iconBox =>
                    {
                        iconBox.Icon([Icon.Default, "w-5 h-5 md:w-6 md:h-6 text-white"], name: "wand-2");
                    });
                    row.Column(["gap-0.5"], content: textCol =>
                    {
                        textCol.Text(["text-lg md:text-xl font-semibold text-[#1a1a1a]"], translations.CreateYourOwnScenario);
                        textCol.Text(["text-xs md:text-sm text-[#6b7280]"], "Describe a situation you'd like to practice");
                    });
                });
            });

            // Input section
            view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: inputView =>
            {
                inputView.Column(["gap-5"], content: col =>
                {
                    col.TextArea(["min-h-[180px] bg-white/60 border border-gray-200/50 rounded-2xl px-4 py-3 text-[#1a1a1a] placeholder-[#9ca3af] text-[15px] focus:border-gray-300 focus:bg-white/80 transition-all duration-200 resize-none"],
                        value: _scenarioDescription.Value,
                        placeholder: "Describe the scenario you want to practice...\n\nFor example:\n• Ordering food at a restaurant\n• Asking for directions in Helsinki\n• A job interview in Finnish",
                        onValueChange: value =>
                        {
                            _scenarioDescription.Value = value;
                            return Task.CompletedTask;
                        });

                    // Voice input option
                    col.Box(["p-4 bg-white/50 rounded-2xl"], content: voiceBox =>
                    {
                        voiceBox.Row(["items-center gap-4"], content: row =>
                        {
                            var micStyle = outer.IsRecording.Value
                                ? "w-14 h-14 rounded-full bg-red-500 text-white flex items-center justify-center animate-pulse shadow-lg text-xl"
                                : $"w-14 h-14 rounded-full bg-white hover:bg-gray-50 text-[#6b7280] hover:text-[#1a1a1a] flex items-center justify-center transition-all duration-200 border border-gray-200/50 shadow-sm text-xl";

                            row.CaptureButton(
                                [micStyle],
                                kind: MediaCaptureKind.Audio,
                                label: "🎤",
                                captureMode: MediaCaptureButtonMode.Hold,
                                audioOptions: new ClientAudioCaptureOptions
                                {
                                    AutoGainControl = true,
                                    NoiseSuppression = true,
                                    EchoCancellation = true
                                },
                                onCaptureStart: async e =>
                                {
                                    outer.IsRecording.Value = true;
                                    outer.InterruptSpeaking();
                                    await Task.CompletedTask;
                                },
                                onCaptureStop: async e =>
                                {
                                    outer.IsRecording.Value = false;
                                    await Task.CompletedTask;
                                });

                            row.Column(["gap-0.5"], content: textCol =>
                            {
                                textCol.Text(["text-sm font-medium text-[#1a1a1a]"], outer.IsRecording.Value ? "Recording..." : "Voice input");
                                textCol.Text(["text-xs text-[#6b7280]"], outer.IsRecording.Value ? "Release to transcribe" : "Hold to describe your scenario");
                            });
                        });
                    });

                    if (_error.Value != null)
                    {
                        col.Box(["p-4 bg-red-50/80 rounded-2xl border border-red-200/50"], content: errorView =>
                        {
                            errorView.Text(["text-sm text-red-600"], _error.Value);
                        });
                    }

                    if (_isGenerating.Value)
                    {
                        col.Box([LearningApp.Styles.GetGoalsCard(theme), "p-5 rounded-2xl"], content: loadingView =>
                        {
                            loadingView.Row(["items-center justify-center gap-3"], content: row =>
                            {
                                row.Icon([Icon.Default, $"w-5 h-5 {LearningApp.Styles.GetAccentText(theme)} animate-spin"], name: "loader");
                                row.Text(["text-base font-medium", LearningApp.Styles.GetGoalsTitle(theme)], translations.Generating);
                            });
                        });
                    }
                    else
                    {
                        col.Button([$"{LearningApp.Styles.GetAccentGradient(theme)} text-white w-full py-4 rounded-2xl font-semibold text-base shadow-lg hover:shadow-xl hover:opacity-95 active:scale-[0.98] transition-all duration-200"],
                            label: translations.Start,
                            disabled: string.IsNullOrWhiteSpace(_scenarioDescription.Value),
                            onClick: async () =>
                            {
                                if (_isGenerating.Value || string.IsNullOrWhiteSpace(_scenarioDescription.Value))
                                {
                                    return;
                                }

                                await GenerateExercise(_scenarioDescription.Value);
                            });
                    }
                });
            });
        });
    }
}
