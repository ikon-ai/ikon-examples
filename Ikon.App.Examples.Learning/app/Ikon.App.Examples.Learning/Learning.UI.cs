public partial class LearningApp
{
    private void RenderUI()
    {
        UI.Root(style: [Page.Default, "font-sans h-screen w-screen overflow-hidden relative", Styles.BackgroundBase], content: view =>
        {
            var currentState = CurrentStateReactive.Value;
            var showSettings = ShowSettingsPanel.Value;
            var theme = SelectedTheme.Value;

            var currentModel = AvailableModels[SelectedModelIndex.Value];
            var currentView = ViewModes[ViewModeIndex.Value];

            // Get transform based on current view mode
            var desktopTransform = ViewModeIndex.Value switch
            {
                0 => currentModel.FullBody,
                1 => currentModel.Portrait,
                2 => currentModel.Face,
                _ => currentModel.Portrait
            };

            // Desktop: Full-screen avatar canvas as background - positioned via 3D camera
            view.Box(["absolute inset-0 pointer-events-none z-0",
                "hidden md:block"], content: avatarContainer =>
            {
                avatarContainer.Live2DCanvas(
                    source: currentModel.Path,
                    isListening: IsListening.Value,
                    expression: _currentExpression.Value,
                    motion: _currentMotion.Value,
                    viewMode: currentView.ViewMode,
                    scale: desktopTransform.Scale,
                    offsetY: desktopTransform.OffsetY,
                    style: ["absolute inset-0 w-full h-full"]
                );
            });

            // Mobile: Avatar layout - different for chat state vs other states
            var mobileTransform = currentModel.Mobile;
            var isChatState = currentState == LearningState.Chat;

            // Mobile only: Avatar layouts
            // Chat state: Avatar in top 1/3 of screen, full width
            if (isChatState)
            {
                var mobileChatTransform = currentModel.MobileChat;
                view.Box(["md:hidden absolute top-0 left-0 right-0 h-[33vh] z-10 pointer-events-none"], content: mobileAvatarWrapper =>
                {
                    // Set canvas resolution to match the container aspect ratio (wide rectangle for mobile portrait)
                    // Container is full width (~400px) by 33vh (~280px) = roughly 1.4:1 aspect ratio
                    mobileAvatarWrapper.Live2DCanvas(
                        source: currentModel.Path,
                        isListening: IsListening.Value,
                        expression: _currentExpression.Value,
                        motion: _currentMotion.Value,
                        viewMode: "face",
                        scale: mobileChatTransform.Scale,
                        offsetY: mobileChatTransform.OffsetY,
                        canvasWidth: 600,
                        canvasHeight: 420,
                        style: ["w-full h-full"]
                    );
                });
            }

            // Other states: Circular avatar in top-left corner (mobile only)
            if (!isChatState)
            {
                view.Box(["md:hidden absolute top-4 left-4 z-20 pointer-events-none"], content: mobileAvatarWrapper =>
                {
                    mobileAvatarWrapper.Box(["w-20 h-20 rounded-full overflow-hidden shadow-lg border-4 border-white/80 bg-gradient-to-br from-white/90 to-gray-100/90"], content: circleContainer =>
                    {
                        circleContainer.Live2DCanvas(
                            source: currentModel.Path,
                            isListening: IsListening.Value,
                            expression: _currentExpression.Value,
                            motion: _currentMotion.Value,
                            viewMode: "face",
                            scale: mobileTransform.Scale,
                            offsetY: mobileTransform.OffsetY,
                            style: ["w-full h-full"]
                        );
                    });
                });
            }

            // Top bar - clean, minimal with proper spacing for mobile avatar
            view.Row(["absolute top-0 left-0 right-0 px-4 py-3 md:p-4 flex justify-between items-center z-40"], content: topBar =>
            {
                // Back button (only show if not on main menu and settings panel is closed)
                // On mobile, offset to the right of the circular avatar (except in chat state where avatar is full width)
                if (!showSettings && currentState != LearningState.MainMenu && currentState != LearningState.Null)
                {
                    var backButtonMargin = isChatState ? "ml-0" : "ml-24 md:ml-0";
                    topBar.Button(
                        [Button.Base, Styles.GlassCard, $"{backButtonMargin} w-10 h-10 md:w-11 md:h-11 rounded-full text-gray-700 hover:bg-white/90 transition-all duration-200 hover:shadow-md flex items-center justify-center"],
                        onClick: async () =>
                        {
                            // Let the current state handle back navigation first
                            var handled = await States.CurrentState.HandleBackAsync();

                            if (!handled)
                            {
                                await States.StateMachine.FireAsync(Trigger.ReturnToMainMenu);
                            }
                        },
                        content: btnView =>
                        {
                            btnView.Icon([Icon.Default, "w-5 h-5 md:w-6 md:h-6"], name: "chevron-left");
                        });
                }
                else
                {
                    topBar.Box(["flex-1"]);
                }

                // Settings button - refined with better hover states
                topBar.Button(
                    [Button.Base, showSettings
                        ? $"{Styles.GetAccentGradient(theme)} w-10 h-10 md:w-11 md:h-11 rounded-full text-white shadow-lg scale-95"
                        : $"{Styles.GlassCard} w-10 h-10 md:w-11 md:h-11 rounded-full text-gray-600 hover:bg-white/95 hover:shadow-md transition-all duration-200"],
                    label: showSettings ? "✕" : "⚙",
                    onClick: () =>
                    {
                        ShowSettingsPanel.Value = !ShowSettingsPanel.Value;
                        return Task.CompletedTask;
                    });
            });

            // Main content area - responsive layout
            view.Box(["absolute inset-0 flex flex-col z-10 pointer-events-none"], content: contentWrapper =>
            {
                // Top spacing - taller for chat state on mobile to account for larger avatar
                var topSpacing = isChatState ? "h-[35vh] md:h-16" : "h-16 md:h-16";
                contentWrapper.Box([topSpacing]);

                // Content area - overlays avatar with glass effect for modern look
                contentWrapper.Box(["flex-1 flex flex-col min-h-0 pointer-events-auto",
                    "w-full md:ml-auto md:w-[55%] lg:w-[50%]",
                    "px-4 pb-4 md:px-8 md:pb-6"], content: chatArea =>
                {
                    chatArea.Box(["w-full max-w-2xl mx-auto h-full flex flex-col"], content: innerView =>
                    {
                        States.CurrentState.Render(innerView);
                    });
                });
            });

            // Settings panel overlay
            if (showSettings)
            {
                RenderSettingsPanel(view);
            }
        });
    }

    private void RenderSettingsPanel(UIView view)
    {
        var translations = Translations;
        var theme = SelectedTheme.Value;

        // Overlay backdrop with blur - clicking closes the panel
        view.Button(["absolute inset-0 bg-black/40 backdrop-blur-sm z-30 cursor-default", Styles.FadeIn],
            label: "",
            onClick: () =>
            {
                ShowSettingsPanel.Value = false;
                return Task.CompletedTask;
            });

        // Panel container - refined bottom sheet with Finnish minimalism
        view.Box(["absolute bottom-0 left-0 right-0 max-h-[80vh] bg-white/98 backdrop-blur-2xl rounded-t-[32px] shadow-2xl overflow-hidden z-30",
            "motion-[0:translate-y-[100%]_opacity-0,100:translate-y-0_opacity-100] motion-duration-300ms motion-fill-both motion-ease-out"],
            content: panel =>
            {
                panel.Column(["h-full"], content: settingsView =>
                {
                    // Drag handle indicator - subtle
                    settingsView.Row(["justify-center pt-4 pb-2"], content: handleRow =>
                    {
                        handleRow.Box(["w-12 h-1.5 bg-gray-200 rounded-full"]);
                    });

                    // Scrollable content
                    settingsView.ScrollArea(rootStyle: [ScrollArea.Root, "flex-1 px-6 pb-10"], content: scrollContent =>
                    {
                        scrollContent.Column(["gap-6"], content: content =>
                        {
                            // Header
                            content.Row(["justify-between items-center py-3"], content: headerRow =>
                            {
                                headerRow.Text(["text-2xl font-semibold text-[#1a1a1a]"], translations.Settings);
                                headerRow.Button(["w-10 h-10 rounded-full bg-gray-100 hover:bg-gray-200 text-gray-500 flex items-center justify-center transition-all duration-200 hover:scale-105 active:scale-95"],
                                    label: "✕",
                                    onClick: () =>
                                    {
                                        ShowSettingsPanel.Value = false;
                                        return Task.CompletedTask;
                                    });
                            });

                            // Theme selection
                            content.Column(["gap-4"], content: section =>
                            {
                                section.Text(["text-xs font-bold text-[#9ca3af] uppercase tracking-widest"], "Color Theme");
                                section.Row(["gap-1.5 flex-wrap"], content: themesView =>
                                {
                                    RenderThemeOption(themesView, LearningTheme.LakeBlue, "Lake Blue", theme);
                                    RenderThemeOption(themesView, LearningTheme.PineGreen, "Pine Green", theme);
                                    RenderThemeOption(themesView, LearningTheme.NordicTeal, "Nordic Teal", theme);
                                });
                            });

                            // Character selection
                            content.Column(["gap-4"], content: section =>
                            {
                                section.Text(["text-xs font-bold text-[#9ca3af] uppercase tracking-widest"], translations.SelectCharacter);
                                section.Row(["gap-2.5 flex-wrap"], content: charactersView =>
                                {
                                    string[] characters = ["Pina", "Haru", "Hibiki", "Hiyori", "Mark"];

                                    for (int i = 0; i < characters.Length; i++)
                                    {
                                        var index = i;
                                        var name = characters[i];
                                        var isSelected = SelectedModelIndex.Value == index;

                                        charactersView.Button([isSelected
                                            ? $"{Styles.GetAccentGradient(theme)} text-white px-5 py-3 rounded-2xl font-semibold shadow-md text-sm transition-all duration-200"
                                            : "bg-gray-50 hover:bg-gray-100 text-[#1a1a1a] px-5 py-3 rounded-2xl font-medium text-sm transition-all duration-200 border border-gray-100 hover:border-gray-200"],
                                            label: name,
                                            onClick: () =>
                                            {
                                                SelectedModelIndex.Value = index;
                                                return Task.CompletedTask;
                                            });
                                    }
                                });
                            });

                            // Voice selection - grouped by provider
                            content.Column(["gap-4"], content: section =>
                            {
                                section.Text(["text-xs font-bold text-[#9ca3af] uppercase tracking-widest"], translations.SelectVoice);

                                // Group voices by provider
                                var voicesByProvider = AvailableVoices
                                    .Select((voice, index) => (voice, index))
                                    .GroupBy(v => v.voice.Provider)
                                    .ToList();

                                section.Column(["gap-3"], content: providersCol =>
                                {
                                    foreach (var providerGroup in voicesByProvider)
                                    {
                                        providersCol.Column(["gap-2"], content: providerSection =>
                                        {
                                            // Provider header with model info
                                            var modelName = providerGroup.First().voice.Model switch
                                            {
                                                SpeechGeneratorModel.Eleven3 => "Eleven v3",
                                                SpeechGeneratorModel.Gpt4OmniMiniTts => "GPT-4o Mini TTS",
                                                SpeechGeneratorModel.AzureSpeechService => "Neural TTS",
                                                _ => ""
                                            };
                                            providerSection.Text(["text-xs font-medium text-[#6b7280]"],
                                                $"{providerGroup.Key} ({modelName})");

                                            providerSection.Row(["gap-1.5 flex-wrap"], content: voicesRow =>
                                            {
                                                foreach (var (voice, index) in providerGroup)
                                                {
                                                    var voiceIndex = index;
                                                    var isSelected = SelectedVoiceIndex.Value == voiceIndex;

                                                    voicesRow.Button([isSelected
                                                        ? $"{Styles.GetAccentGradient(theme)} text-white px-3 py-2 rounded-lg font-semibold shadow-md text-xs transition-all duration-200"
                                                        : "bg-gray-50 hover:bg-gray-100 text-[#1a1a1a] px-3 py-2 rounded-lg font-medium text-xs transition-all duration-200 border border-gray-100 hover:border-gray-200"],
                                                        label: voice.Name,
                                                        onClick: () =>
                                                        {
                                                            SelectedVoiceIndex.Value = voiceIndex;
                                                            return Task.CompletedTask;
                                                        });
                                                }
                                            });
                                        });
                                    }
                                });
                            });

                            // View mode selection
                            content.Column(["gap-4"], content: section =>
                            {
                                section.Text(["text-xs font-bold text-[#9ca3af] uppercase tracking-widest"], "View Mode");
                                section.Row(["gap-2.5"], content: modesView =>
                                {
                                    string[] modes = ["Full Body", "Portrait", "Face"];

                                    for (int i = 0; i < modes.Length; i++)
                                    {
                                        var index = i;
                                        var name = modes[i];
                                        var isSelected = ViewModeIndex.Value == index;

                                        modesView.Button([isSelected
                                            ? $"{Styles.GetAccentGradient(theme)} text-white px-5 py-3 rounded-2xl font-semibold shadow-md text-sm transition-all duration-200"
                                            : "bg-gray-50 hover:bg-gray-100 text-[#1a1a1a] px-5 py-3 rounded-2xl font-medium text-sm transition-all duration-200 border border-gray-100 hover:border-gray-200"],
                                            label: name,
                                            onClick: () =>
                                            {
                                                ViewModeIndex.Value = index;
                                                return Task.CompletedTask;
                                            });
                                    }
                                });
                            });

                            // Bottom safe area padding
                            content.Box(["h-6"]);
                        });
                    });
                });
            });
    }

    private void RenderThemeOption(UIView container, LearningTheme themeOption, string label, LearningTheme currentTheme)
    {
        var isSelected = currentTheme == themeOption;
        var gradientClass = Styles.GetAccentGradient(themeOption);

        container.Button([isSelected
            ? $"{gradientClass} text-white px-3 py-2 rounded-lg font-semibold shadow-md text-xs transition-all duration-200"
            : "bg-gray-50 hover:bg-gray-100 text-[#1a1a1a] px-3 py-2 rounded-lg font-medium text-xs transition-all duration-200 border border-gray-100 hover:border-gray-200"],
            onClick: () =>
            {
                SetTheme(themeOption);
                return Task.CompletedTask;
            },
            content: btnView =>
            {
                btnView.Row(["gap-2 items-center"], content: row =>
                {
                    // Small color indicator
                    row.Box([$"w-4 h-4 rounded-full {gradientClass}"]);
                    row.Text([], label);
                });
            });
    }
}
