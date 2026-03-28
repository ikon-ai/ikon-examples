namespace Ikon.App.Examples.Learning.States;

public class YourProgressState(LearningApp outer) : ILearningState
{
    public Task EnterAsync()
    {
        return Task.CompletedTask;
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
                        iconBox.Icon([Icon.Default, "w-5 h-5 md:w-6 md:h-6 text-white"], name: "bar-chart-2");
                    });
                    row.Column(["gap-0.5"], content: textCol =>
                    {
                        textCol.Text(["text-lg md:text-xl font-semibold text-[#1a1a1a]"], translations.YourProgress);
                        textCol.Text(["text-xs md:text-sm text-[#6b7280]"], "Track your learning journey");
                    });
                });
            });

            // Stats cards - refined with consistent gradients
            view.Row(["gap-3 md:gap-4"], content: statsView =>
            {
                // Streak - warm gradient
                statsView.Box(["flex-1 p-4 md:p-5 rounded-2xl bg-gradient-to-br from-amber-400 to-orange-500 shadow-md"], content: v =>
                {
                    v.Column(["gap-1 items-center text-center"], content: col =>
                    {
                        col.Text(["text-2xl md:text-3xl font-bold text-white"], $"{userState?.CurrentStreak ?? 0}");
                        col.Text(["text-xs text-white/75 font-medium"], translations.WinningStreak);
                    });
                });

                // Exercises completed - theme accent
                statsView.Box([$"flex-1 p-4 md:p-5 rounded-2xl {LearningApp.Styles.GetAccentGradient(theme)} shadow-md"], content: v =>
                {
                    v.Column(["gap-1 items-center text-center"], content: col =>
                    {
                        col.Text(["text-2xl md:text-3xl font-bold text-white"], $"{userState?.ExerciseHistory.Count ?? 0}");
                        col.Text(["text-xs text-white/75 font-medium"], translations.ExercisesCompleted);
                    });
                });

                // Total points - emerald
                statsView.Box(["flex-1 p-4 md:p-5 rounded-2xl bg-gradient-to-br from-emerald-400 to-green-600 shadow-md"], content: v =>
                {
                    v.Column(["gap-1 items-center text-center"], content: col =>
                    {
                        col.Text(["text-2xl md:text-3xl font-bold text-white"], $"{userState?.TotalPoints ?? 0}");
                        col.Text(["text-xs text-white/75 font-medium"], translations.TotalPoints);
                    });
                });
            });

            // Achievements section
            view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: achievementsCard =>
            {
                achievementsCard.Column(["gap-4"], content: col =>
                {
                    col.Text(["text-base md:text-lg font-semibold text-[#1a1a1a]"], translations.Achievements);

                    var achievements = userState?.Achievements ?? [];

                    if (achievements.Count == 0)
                    {
                        col.Box(["p-6 bg-white/50 rounded-2xl text-center"], content: emptyView =>
                        {
                            emptyView.Column(["items-center gap-3"], content: emptyCol =>
                            {
                                emptyCol.Box(["w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center"], content: iconWrapper =>
                                {
                                    iconWrapper.Icon([Icon.Default, "w-8 h-8 text-gray-300"], name: "trophy");
                                });
                                emptyCol.Text(["text-sm text-[#6b7280]"], "No achievements yet");
                                emptyCol.Text(["text-xs text-[#9ca3af]"], "Keep learning to earn badges!");
                            });
                        });
                    }
                    else
                    {
                        col.Row(["gap-3 flex-wrap"], content: achievementsView =>
                        {
                            foreach (var record in achievements)
                            {
                                achievementsView.Box(["p-4 bg-gradient-to-br from-amber-50/80 to-yellow-50/60 backdrop-blur-sm rounded-2xl min-w-[140px] border border-amber-200/30 shadow-sm"], content: achievementView =>
                                {
                                    achievementView.Column(["gap-2 items-center text-center"], content: innerCol =>
                                    {
                                        innerCol.Box(["w-10 h-10 rounded-full bg-gradient-to-br from-amber-400 to-yellow-500 flex items-center justify-center shadow-sm"], content: iconWrapper =>
                                        {
                                            iconWrapper.Icon([Icon.Default, "w-5 h-5 text-white"], name: "award");
                                        });
                                        innerCol.Text(["text-sm font-semibold text-[#1a1a1a]"], record.Achievement.Title(translations));
                                        innerCol.Text(["text-xs text-[#6b7280]"], record.Achievement.Description(translations));
                                    });
                                });
                            }
                        });
                    }
                });
            });

            // Recent exercises section
            if (userState?.ExerciseHistory.Count > 0)
            {
                view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: historyCard =>
                {
                    historyCard.Column(["gap-4"], content: col =>
                    {
                        col.Text(["text-base md:text-lg font-semibold text-[#1a1a1a]"], "Recent Exercises");

                        col.Column(["gap-2"], content: historyCol =>
                        {
                            foreach (var exercise in userState.ExerciseHistory.TakeLast(5).Reverse())
                            {
                                historyCol.Box(["p-4 bg-white/60 rounded-xl border border-gray-100/50"], content: exerciseView =>
                                {
                                    exerciseView.Row(["justify-between items-center"], content: row =>
                                    {
                                        row.Column(["gap-0.5"], content: textCol =>
                                        {
                                            textCol.Text(["text-sm font-medium text-[#1a1a1a]"], exercise.ExerciseName);
                                            textCol.Text(["text-xs text-[#6b7280]"], exercise.CompletedAt.ToString("MMM dd, yyyy"));
                                        });

                                        var scoreColor = exercise.Score >= 80 ? "text-emerald-600 bg-emerald-50" :
                                                        exercise.Score >= 60 ? "text-amber-600 bg-amber-50" : "text-red-500 bg-red-50";
                                        row.Box([$"px-3 py-1 rounded-full {scoreColor}"], content: scoreBox =>
                                        {
                                            scoreBox.Text(["text-sm font-semibold"], $"{exercise.Score}%");
                                        });
                                    });
                                });
                            }
                        });
                    });
                });
            }
        });
    }
}
