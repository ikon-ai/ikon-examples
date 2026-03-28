namespace Ikon.App.Examples.Learning.States;

public class MainMenuState(LearningApp outer) : ILearningState
{
    public Task EnterAsync()
    {
        // Clear exercise context when returning to main menu
        outer.CurrentExercise = null;
        outer.CurrentArticle = null;
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

        contentView.Column(["gap-4 md:gap-5 pt-2 md:pt-0"], content: view =>
        {
            // Header with greeting and stats - clean Finnish design
            view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: headerView =>
            {
                headerView.Column(["gap-4 md:gap-5"], content: innerView =>
                {
                    // Greeting with Aino's name - warm and personal
                    innerView.Column(["gap-1"], content: greetingCol =>
                    {
                        greetingCol.Text(["text-sm text-[#6b7280] font-medium tracking-wide"], "TERVETULOA");
                        greetingCol.Text(["text-2xl md:text-3xl font-semibold text-[#1a1a1a]"], $"{translations.Hey}!");
                    });

                    // Stats row - refined Finnish-style cards with subtle gradients
                    innerView.Row(["gap-3 md:gap-4"], content: statsView =>
                    {
                        // Language level - primary accent
                        statsView.Box([$"flex-1 p-4 md:p-5 rounded-2xl {LearningApp.Styles.GetAccentGradient(theme)} shadow-md"], content: v =>
                        {
                            v.Column(["gap-1"], content: col =>
                            {
                                col.Text(["text-[10px] md:text-xs text-white/75 font-semibold uppercase tracking-wider"], translations.LanguageLevel);
                                col.Text(["text-xl md:text-2xl font-bold text-white"], userState?.CurrentLanguageLevel ?? "A1");
                            });
                        });

                        // Streak - warm amber
                        statsView.Box(["flex-1 p-4 md:p-5 rounded-2xl bg-gradient-to-br from-amber-400 to-orange-500 shadow-md"], content: v =>
                        {
                            v.Column(["gap-1"], content: col =>
                            {
                                col.Text(["text-[10px] md:text-xs text-white/75 font-semibold uppercase tracking-wider"], translations.Streak);
                                col.Row(["items-baseline gap-1.5"], content: row =>
                                {
                                    row.Text(["text-xl md:text-2xl font-bold text-white"], $"{userState?.CurrentStreak ?? 0}");
                                    row.Text(["text-xs text-white/60 font-medium"], "days");
                                });
                            });
                        });

                        // Points - fresh green
                        statsView.Box(["flex-1 p-4 md:p-5 rounded-2xl bg-gradient-to-br from-emerald-400 to-green-600 shadow-md"], content: v =>
                        {
                            v.Column(["gap-1"], content: col =>
                            {
                                col.Text(["text-[10px] md:text-xs text-white/75 font-semibold uppercase tracking-wider"], translations.TotalPoints);
                                col.Text(["text-xl md:text-2xl font-bold text-white"], $"{userState?.TotalPoints ?? 0}");
                            });
                        });
                    });
                });
            });

            // What to learn section - refined Finnish design
            view.Box([LearningApp.Styles.GlassCardStrong, "p-5 md:p-6 rounded-3xl"], content: sectionView =>
            {
                sectionView.Column(["gap-4 md:gap-5"], content: col =>
                {
                    col.Text(["text-lg md:text-xl font-semibold text-[#1a1a1a]"], translations.WhatToLearn);

                    col.Column(["gap-3 md:gap-4"], content: menuView =>
                    {
                        // Let's practice button - prominent CTA with subtle shadow
                        menuView.Button([$"{LearningApp.Styles.GetAccentGradient(theme)} text-white w-full py-4 md:py-5 rounded-2xl font-semibold text-base md:text-lg shadow-lg hover:shadow-xl hover:scale-[1.02] active:scale-[0.98] transition-all duration-200", LearningApp.Styles.SlideUp],
                            label: translations.LetsPractice,
                            onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.SelectExerciseMenu));

                        // Secondary buttons - clean with icons hint
                        menuView.Button(["w-full py-3.5 md:py-4 rounded-2xl font-medium text-[#1a1a1a] bg-white/80 hover:bg-white border border-gray-200/60 hover:border-gray-300/80 hover:shadow-sm transition-all duration-200"],
                            label: $"📰  {translations.TodaysNews}",
                            onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.News));

                        menuView.Button(["w-full py-3.5 md:py-4 rounded-2xl font-medium text-[#1a1a1a] bg-white/80 hover:bg-white border border-gray-200/60 hover:border-gray-300/80 hover:shadow-sm transition-all duration-200"],
                            label: $"🎭  {translations.YourScenarios}",
                            onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.YourScenarios));

                        // Chat with Aino - special outlined style
                        menuView.Button([$"w-full py-3.5 md:py-4 rounded-2xl font-semibold {LearningApp.Styles.GetAccentText(theme)} bg-white/60 hover:bg-white/90 border-2 {LearningApp.Styles.GetAccentBorder(theme)} hover:shadow-md transition-all duration-200"],
                            label: $"💬  Chat with Aino",
                            onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.Chat));
                    });
                });
            });

            // Progress button - subtle link style at bottom
            view.Button(["w-full py-3.5 rounded-2xl font-medium text-[#6b7280] hover:text-[#1a1a1a] bg-white/40 hover:bg-white/70 transition-all duration-200"],
                label: $"📊  {translations.YourProgress}",
                onClick: async () => await outer.States.StateMachine.FireAsync(Trigger.YourProgress));
        });
    }
}
