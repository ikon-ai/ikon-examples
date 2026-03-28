namespace Ikon.App.Examples.Learning.States;

public class ExerciseState(LearningApp outer) : ILearningState
{
    private int _currentSubGoalIndex = 0;
    private int _currentQuestionIndex = 0;
    private int _helpCount = 0;
    private readonly List<UserScore> _userScores = [];
    private readonly Dictionary<string, int> _retryCounts = new();
    private bool _hasConversationEnded = false;
    private DateTime _exerciseStartTime;
    private string _lastAiQuestion = string.Empty;
    private string _lastHint = string.Empty;
    private bool _audioInputUsed = false;

    private readonly Reactive<bool> _isGeneratingHint = new(false);
    private readonly Reactive<bool> _isGeneratingFeedback = new(false);
    private readonly Reactive<bool> _isGeneratingReport = new(false);
    private readonly Reactive<bool> _showReport = new(false);
    private readonly Reactive<Report?> _currentReport = new(null);
    private readonly Reactive<string?> _detailedTranslationWord = new(null);
    private readonly Reactive<DetailedTranslationResult?> _detailedTranslationResult = new(null);
    private readonly Reactive<bool> _isLoadingDetailedTranslation = new(false);
    private readonly Reactive<string[]> _tokenizedWords = new([]);
    public async Task EnterAsync()
    {
        _currentSubGoalIndex = 0;
        _currentQuestionIndex = 0;
        _helpCount = 0;
        _userScores.Clear();
        _retryCounts.Clear();
        _hasConversationEnded = false;
        _exerciseStartTime = DateTime.UtcNow;
        _lastAiQuestion = string.Empty;
        _lastHint = string.Empty;
        _audioInputUsed = false;
        _showReport.Value = false;
        _currentReport.Value = null;
        _detailedTranslationWord.Value = null;
        _detailedTranslationResult.Value = null;
        _tokenizedWords.Value = [];

        outer.ChatMessages.Clear();
        outer.ChatMessagesVersion.Value++;

        if (outer.CurrentExercise != null)
        {
            var exercise = outer.CurrentExercise;

            // Build display greeting (full structured content)
            var displayGreeting = BuildDisplayGreeting(exercise);

            // Build speech greeting (only Finnish/target language parts)
            var speechGreeting = BuildSpeechGreeting(exercise);

            outer.AddChatMessage(ChatRole.Assistant, displayGreeting, isStructuredBrief: true);
            _lastAiQuestion = displayGreeting;

            // Only speak the Finnish/target language parts
            _ = outer.SpeakAsync(speechGreeting);
        }
    }

    private string BuildDisplayGreeting(Exercise exercise)
    {
        var parts = new List<string>();

        // Title
        parts.Add($"**{exercise.Name}**");

        // Scenario (target language)
        if (!string.IsNullOrEmpty(exercise.Scenario))
        {
            parts.Add($"📖 {exercise.Scenario}");
        }

        // Roles
        var rolesParts = new List<string>();

        if (!string.IsNullOrEmpty(exercise.Roles.AI))
        {
            rolesParts.Add($"🤖 AI: {exercise.Roles.AI}");
        }

        if (!string.IsNullOrEmpty(exercise.Roles.User?.Role))
        {
            rolesParts.Add($"👤 You: {exercise.Roles.User.Role}");
        }

        if (rolesParts.Count > 0)
        {
            parts.Add(string.Join("\n", rolesParts));
        }

        // Goals
        if (exercise.Type == ExerciseType.Conversational && exercise.Roles.User?.SubGoals.Count > 0)
        {
            var goals = exercise.Roles.User.SubGoals.Where(g => !g.Optional).ToList();

            if (goals.Count > 0)
            {
                var goalsText = "🎯 Goals:\n" + string.Join("\n", goals.Select(g => $"• {g.Description}"));
                parts.Add(goalsText);
            }
        }

        return string.Join("\n\n", parts);
    }

    private string BuildSpeechGreeting(Exercise exercise)
    {
        // Only speak the scenario in the target language - keep it short!
        if (!string.IsNullOrEmpty(exercise.Scenario))
        {
            return exercise.Scenario;
        }

        return exercise.Name;
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public Task HandleUserMessageAsync(string userId, string text)
    {
        outer.AddChatMessage(ChatRole.User, text);
        return Task.CompletedTask;
    }

    public async Task HandleAIMessageAsync(string message)
    {
        outer.AddChatMessage(ChatRole.Assistant, message);
        _lastAiQuestion = message;
        await outer.SpeakAsync(message);

        await CheckConversationProgress(message);
    }

    private async Task CheckConversationProgress(string aiMessage)
    {
        if (outer.CurrentExercise == null)
        {
            return;
        }

        var exercise = outer.CurrentExercise;

        if (exercise.Type == ExerciseType.SimpleQA)
        {
            if (_currentQuestionIndex >= exercise.Questions.Count)
            {
                _hasConversationEnded = true;
            }
        }
        else if (exercise.Type == ExerciseType.Conversational)
        {
            var requiredGoals = exercise.Roles.User?.SubGoals.Where(g => !g.Optional).ToList() ?? [];
            if (_currentSubGoalIndex >= requiredGoals.Count)
            {
                _hasConversationEnded = true;
            }
        }

        if (_hasConversationEnded && !_showReport.Value)
        {
            await GenerateExerciseReport();
        }
    }

    private async Task GenerateFeedbackForResponse(string userAnswer)
    {
        if (outer.CurrentExercise == null || outer.UserState == null)
        {
            return;
        }

        _isGeneratingFeedback.Value = true;

        try
        {
            var exercise = outer.CurrentExercise;
            var subGoalId = exercise.Type == ExerciseType.Conversational ? _currentSubGoalIndex + 1 : 0;
            var questionId = exercise.Type == ExerciseType.SimpleQA ? _currentQuestionIndex + 1 : 0;

            var score = await GenerateFeedbackShader.GenerateAsync(
                LLMModel.Gpt41.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState,
                exercise,
                _lastAiQuestion,
                userAnswer,
                subGoalId,
                questionId,
                _helpCount,
                _audioInputUsed
            );

            ApplyHelpPenalty(score);

            score.SubGoalId = subGoalId;
            score.QuestionId = questionId;
            score.AiResponse = _lastAiQuestion;
            score.ProvidedAnswer = userAnswer;
            score.AiResponseTimestamp = DateTime.UtcNow.AddSeconds(-10).ToString("o");
            score.ProvidedAnswerTimestamp = DateTime.UtcNow.ToString("o");
            score.HintsAsked = _helpCount > 0 ? "Yes" : "No";
            score.AudioInput = _audioInputUsed ? "Yes" : "No";

            _userScores.Add(score);

            if (exercise.Type == ExerciseType.SimpleQA)
            {
                _currentQuestionIndex++;
            }
            else
            {
                _currentSubGoalIndex++;
            }

            _helpCount = 0;
            _lastHint = string.Empty;
            _audioInputUsed = false;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error generating feedback: {ex.Message}");
        }
        finally
        {
            _isGeneratingFeedback.Value = false;
        }
    }

    private void ApplyHelpPenalty(UserScore score)
    {
        if (_helpCount > 0)
        {
            score.Score.TaskFulfillment = Math.Max(0, score.Score.TaskFulfillment - _helpCount);
            score.Score.OrganizationAndStructure = Math.Max(0, score.Score.OrganizationAndStructure - _helpCount);
            score.Score.LinguisticResourceAndAccuracy = Math.Max(0, score.Score.LinguisticResourceAndAccuracy - _helpCount);
        }
    }

    private async Task GenerateExerciseReport()
    {
        if (outer.CurrentExercise == null || outer.UserState == null || _userScores.Count == 0)
        {
            return;
        }

        _isGeneratingReport.Value = true;

        try
        {
            var timeTaken = (int)(DateTime.UtcNow - _exerciseStartTime).TotalSeconds;

            var report = await GenerateReportShader.GenerateAsync(
                LLMModel.Gpt41.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState,
                outer.CurrentExercise,
                _userScores,
                timeTaken
            );

            _currentReport.Value = report;
            _showReport.Value = true;

            outer.UserState.ExerciseHistory.Add(new ExerciseReport
            {
                ExerciseId = outer.CurrentExercise.Id,
                ExerciseName = outer.CurrentExercise.Name,
                Score = report.OverallScore,
                CompletedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(timeTaken)
            });

            var pointsEarned = report.OverallScore / 10;
            outer.UserState.TotalPoints += pointsEarned;

            await outer.CheckAndAwardAchievementsAsync();
            await outer.SaveUserStateAsync();
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error generating report: {ex.Message}");
        }
        finally
        {
            _isGeneratingReport.Value = false;
        }
    }

    private async Task RequestHint()
    {
        if (outer.CurrentExercise == null || outer.UserState == null)
        {
            return;
        }

        _isGeneratingHint.Value = true;

        try
        {
            var conversationHistory = string.Join("\n", outer.ChatMessages.Select(m =>
                $"{(m.Role == ChatRole.User ? "User" : "AI")}: {m.Content.Value}"));

            var hint = await ContinuationShader.GenerateAsync(
                LLMModel.Gpt41Mini.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState,
                outer.CurrentExercise,
                conversationHistory,
                _lastAiQuestion,
                _lastHint
            );

            _lastHint = hint.Suggestion;
            _helpCount++;

            outer.AddChatMessage(ChatRole.Assistant, $"💡 **Hint:** {hint.Suggestion}\n\n_{hint.Explanation}_");
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error generating hint: {ex.Message}");
            outer.AddChatMessage(ChatRole.Assistant, "Sorry, I couldn't generate a hint right now. Please try again.");
        }
        finally
        {
            _isGeneratingHint.Value = false;
        }
    }

    private async Task TranslateMessage(ChatMessage message)
    {
        if (message.IsTranslating.Value || message.TranslationValue.Value != null)
        {
            message.ShowTranslation.Value = !message.ShowTranslation.Value;
            return;
        }

        message.IsTranslating.Value = true;

        try
        {
            var translation = await CreateTranslationShader.GenerateAsync(
                LLMModel.Gpt41Mini.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState!,
                message.Content.Value
            );

            message.TranslationValue.Value = translation;
            message.ShowTranslation.Value = true;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error translating message: {ex.Message}");
        }
        finally
        {
            message.IsTranslating.Value = false;
        }
    }

    private async Task ShowDetailedTranslation(string messageContent)
    {
        if (outer.UserState == null)
        {
            return;
        }

        _isLoadingDetailedTranslation.Value = true;
        _detailedTranslationWord.Value = null;
        _detailedTranslationResult.Value = null;

        try
        {
            var needsLLMTokenization = messageContent.Any(c =>
                (c >= 0x4E00 && c <= 0x9FFF) ||
                (c >= 0x3040 && c <= 0x30FF) ||
                (c >= 0x0600 && c <= 0x06FF) ||
                (c >= 0xAC00 && c <= 0xD7AF));

            if (needsLLMTokenization)
            {
                var tokenized = await TokenizeShader.GenerateAsync(
                    LLMModel.Gpt41Mini.ToString(),
                    nameof(ReasoningEffort.None),
                    messageContent,
                    outer.UserState.TargetLanguage
                );
                _tokenizedWords.Value = tokenized.Words;
            }
            else
            {
                _tokenizedWords.Value = messageContent
                    .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim('.', ',', '!', '?', '"', '\'', '(', ')', '[', ']', ':', ';'))
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error tokenizing message: {ex.Message}");
            _tokenizedWords.Value = messageContent.Split(' ');
        }
        finally
        {
            _isLoadingDetailedTranslation.Value = false;
        }
    }

    private async Task GetWordDetails(string word, string sentenceContext)
    {
        if (outer.UserState == null)
        {
            return;
        }

        _isLoadingDetailedTranslation.Value = true;
        _detailedTranslationWord.Value = word;

        try
        {
            var result = await CreateDetailedTranslationShader.GenerateAsync(
                LLMModel.Gpt41.ToString(),
                nameof(ReasoningEffort.None),
                outer.UserState,
                word,
                sentenceContext
            );

            _detailedTranslationResult.Value = result;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error getting word details: {ex.Message}");
        }
        finally
        {
            _isLoadingDetailedTranslation.Value = false;
        }
    }

    private float CalculateProgress()
    {
        if (outer.CurrentExercise == null)
        {
            return 0f;
        }

        var exercise = outer.CurrentExercise;

        if (exercise.Type == ExerciseType.SimpleQA)
        {
            if (exercise.Questions.Count == 0)
            {
                return 0f;
            }

            return (_currentQuestionIndex / (float)exercise.Questions.Count) * 100f;
        }
        else
        {
            var requiredGoals = exercise.Roles.User?.SubGoals.Where(g => !g.Optional).ToList() ?? [];

            if (requiredGoals.Count == 0)
            {
                return 0f;
            }

            return (_currentSubGoalIndex / (float)requiredGoals.Count) * 100f;
        }
    }

    private string GetScoreColor(float averageScore)
    {
        if (averageScore >= 2.5f)
        {
            return "text-green-500";
        }

        if (averageScore >= 1.5f)
        {
            return "text-yellow-500";
        }

        return "text-red-500";
    }

    public void Render(UIView contentView)
    {
        var translations = outer.Translations;
        var _ = outer.ChatMessagesVersion.Value;

        if (_showReport.Value && _currentReport.Value != null)
        {
            RenderReport(contentView, _currentReport.Value);
            return;
        }

        contentView.Column(["h-full flex flex-col gap-2 md:gap-3 px-3 md:px-0"], content: view =>
        {
            // Header - fixed height
            RenderHeader(view);

            // Messages area - scrollable, takes remaining space
            view.Box(["flex-1 min-h-0 overflow-hidden"], content: scrollContainer =>
            {
                RenderMessages(scrollContainer);
            });

            // Detailed translation panel (if open)
            if (_tokenizedWords.Value.Length > 0)
            {
                RenderDetailedTranslationPanel(view);
            }

            // Input area - fixed at bottom
            RenderInputArea(view);
        });
    }

    private void RenderHeader(UIView view)
    {
        var theme = outer.SelectedTheme.Value;

        view.Box([LearningApp.Styles.GlassCardStrong, "px-4 py-3 md:p-4 rounded-2xl"], content: headerBox =>
        {
            headerBox.Row(["justify-between items-center gap-3"], content: headerView =>
            {
                headerView.Column(["gap-2 flex-1 min-w-0"], content: col =>
                {
                    col.Text(["text-base md:text-lg font-semibold text-[#1a1a1a] truncate"], outer.CurrentExercise?.Name ?? "Exercise");

                    var progress = CalculateProgress();
                    col.Box(["w-full h-1.5 md:h-2 bg-gray-200/50 rounded-full overflow-hidden"], content: progressView =>
                    {
                        progressView.Box([$"h-full {LearningApp.Styles.GetAccentGradient(theme)} rounded-full transition-all duration-300", $"w-[{(int)progress}%]"]);
                    });
                });

                if (_userScores.Count > 0)
                {
                    var avgScore = _userScores.Average(s =>
                        (s.Score.TaskFulfillment + s.Score.OrganizationAndStructure + s.Score.LinguisticResourceAndAccuracy) / 3f);
                    var scoreColor = GetScoreColor(avgScore);

                    headerView.Box([$"px-3 py-1.5 rounded-lg {scoreColor.Replace("text-", "bg-")}/10"], content: scoreBox =>
                    {
                        scoreBox.Text(["text-sm md:text-base font-bold", scoreColor], $"{avgScore:F1}/3");
                    });
                }
            });
        });
    }

    private void RenderMessages(UIView view)
    {
        var theme = outer.SelectedTheme.Value;

        view.ScrollArea(
            autoScroll: true,
            autoScrollKey: outer.ChatMessagesVersion.Value.ToString(),
            rootStyle: [ScrollArea.Root, "h-full px-4 py-4"],
            content: scrollView =>
        {
            foreach (var message in outer.ChatMessages)
            {
                if (string.IsNullOrEmpty(message.Content.Value))
                {
                    continue;
                }

                var currentMessage = message;
                var messageContent = message.Content.Value;
                var isUser = message.Role == ChatRole.User;

                // Render structured briefs differently
                if (message.IsStructuredBrief && !isUser)
                {
                    RenderStructuredBrief(scrollView, currentMessage);
                    continue;
                }

                scrollView.Box([isUser ? "ml-auto" : "mr-auto", "max-w-[75%] mb-4"], content: bubbleWrapper =>
                {
                    // Premium glass-morphism bubbles with gradients
                    var bubbleStyle = isUser
                        ? $"rounded-3xl rounded-br-lg {LearningApp.Styles.GetAccentGradient(theme)} px-5 py-4 shadow-lg"
                        : "rounded-3xl rounded-bl-lg bg-white/70 backdrop-blur-md border border-white/40 px-5 py-4 shadow-sm";

                    bubbleWrapper.Box([
                        bubbleStyle,
                        "motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-200ms"
                    ], content: msgView =>
                    {
                        msgView.Column(["gap-2"], content: col =>
                        {
                            col.Text([Text.Body, isUser ? "text-white" : "text-[#1a1a1a]"], message.Content.Value);

                            if (message.ShowTranslation.Value && message.TranslationValue.Value != null)
                            {
                                col.Box([isUser ? "bg-white/15 rounded-xl px-3 py-2 mt-2" : "bg-gray-100/60 rounded-xl px-3 py-2 mt-2"], content: transView =>
                                {
                                    transView.Column(["gap-1"], content: transCol =>
                                    {
                                        transCol.Text([Text.Caption, isUser ? "text-white/90" : "text-[#4b5563]"],
                                            message.TranslationValue.Value.TranslatedMessage);

                                        if (!string.IsNullOrEmpty(message.TranslationValue.Value.TransliteratedMessage))
                                        {
                                            transCol.Text([Text.Caption, isUser ? "text-white/70" : "text-[#9ca3af]", "italic"],
                                                message.TranslationValue.Value.TransliteratedMessage);
                                        }
                                    });
                                });
                            }

                            // Action buttons - refined pill-style buttons
                            col.Row(["gap-2 mt-2"], content: actionsRow =>
                            {
                                var translateLabel = message.IsTranslating.Value ? "⏳" : (message.ShowTranslation.Value ? "🔽 Hide" : "🌐 Translate");
                                var translateStyle = isUser
                                    ? "text-xs font-medium px-3 py-1.5 rounded-full bg-white/20 hover:bg-white/30 text-white/90 hover:text-white transition-all duration-200"
                                    : "text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200";

                                actionsRow.Button([translateStyle],
                                    label: translateLabel,
                                    onClick: async () =>
                                    {
                                        await TranslateMessage(currentMessage);
                                    });

                                if (!isUser)
                                {
                                    actionsRow.Button(["text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200"],
                                        label: "📖 Details",
                                        onClick: async () =>
                                        {
                                            await ShowDetailedTranslation(messageContent);
                                        });

                                    var audioLabel = outer.GetAudioButtonLabel(currentMessage.Id);
                                    var audioState = outer.GetMessageAudioState(currentMessage.Id);
                                    var audioStyle = audioState == AudioPlaybackState.Playing
                                        ? $"text-xs font-semibold px-3 py-1.5 rounded-full {LearningApp.Styles.GetAccentGradient(theme)} text-white shadow-sm transition-all duration-200"
                                        : "text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200";

                                    actionsRow.Button([audioStyle],
                                        label: audioState == AudioPlaybackState.Playing ? $"⏸ {audioLabel}" : $"🔊 {audioLabel}",
                                        onClick: async () =>
                                        {
                                            await outer.PlayMessageAudioAsync(currentMessage.Id, currentMessage.Content.Value);
                                        });
                                }
                            });
                        });
                    });
                });
            }
        });
    }

    private void RenderStructuredBrief(UIView scrollView, ChatMessage message)
    {
        var content = message.Content.Value;
        var sections = content.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries);
        var theme = outer.SelectedTheme.Value;
        var accentText = LearningApp.Styles.GetAccentText(theme);

        scrollView.Box([LearningApp.Styles.StructuredBriefCard, "mb-4",
            "motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-200ms"
        ], content: briefView =>
        {
            briefView.Column(["gap-5"], content: col =>
            {
                foreach (var section in sections)
                {
                    var trimmedSection = section.Trim();

                    if (string.IsNullOrEmpty(trimmedSection))
                    {
                        continue;
                    }

                    // Title (starts with **)
                    if (trimmedSection.StartsWith("**") && trimmedSection.Contains("**"))
                    {
                        var title = trimmedSection.Replace("**", "");
                        col.Text([Text.H2, "text-[#1a1a1a] font-semibold"], title);
                    }
                    // Scenario (starts with 📖)
                    else if (trimmedSection.StartsWith("📖"))
                    {
                        col.Box([$"bg-white/60 py-4 pl-5 pr-4 rounded-2xl border-l-4 {LearningApp.Styles.GetAccentBorder(theme)}"], content: scenarioBox =>
                        {
                            scenarioBox.Text(["text-base leading-relaxed text-[#1a1a1a]"], trimmedSection.Substring(2).Trim());
                        });
                    }
                    // Roles section (contains 🤖 or 👤)
                    else if (trimmedSection.Contains("🤖") || trimmedSection.Contains("👤"))
                    {
                        col.Box(["bg-white/50 py-4 px-5 rounded-2xl"], content: rolesBox =>
                        {
                            var roleLines = trimmedSection.Split('\n');
                            rolesBox.Column(["gap-3"], content: rolesCol =>
                            {
                                foreach (var roleLine in roleLines)
                                {
                                    if (!string.IsNullOrWhiteSpace(roleLine))
                                    {
                                        rolesCol.Text(["text-sm text-[#6b7280] leading-relaxed"], roleLine.Trim());
                                    }
                                }
                            });
                        });
                    }
                    // Goals section (contains 🎯)
                    else if (trimmedSection.Contains("🎯"))
                    {
                        col.Box([LearningApp.Styles.GetGoalsCard(theme), LearningApp.Styles.GetGoalsBorder(theme), "py-4 pl-5 pr-4 rounded-2xl shadow-sm"], content: goalsBox =>
                        {
                            var lines = trimmedSection.Split('\n');
                            goalsBox.Column(["gap-3"], content: goalsCol =>
                            {
                                foreach (var line in lines)
                                {
                                    var trimmedLine = line.Trim();

                                    if (trimmedLine.StartsWith("🎯"))
                                    {
                                        goalsCol.Text(["text-base font-semibold", LearningApp.Styles.GetGoalsTitle(theme)], trimmedLine);
                                    }
                                    else if (trimmedLine.StartsWith("•"))
                                    {
                                        goalsCol.Text(["text-sm leading-relaxed pl-1", LearningApp.Styles.GetGoalsText(theme)], trimmedLine);
                                    }
                                }
                            });
                        });
                    }
                    // Other content
                    else
                    {
                        col.Text(["text-base leading-relaxed text-[#1a1a1a]"], trimmedSection);
                    }
                }

                // Actions row - refined pill-style buttons
                col.Row(["gap-2 mt-4 pt-4 border-t border-gray-200/40"], content: actionsRow =>
                {
                    var translateLabel = message.IsTranslating.Value ? "⏳" : (message.ShowTranslation.Value ? "🔽 Hide" : "🌐 Translate");

                    actionsRow.Button(["text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200"],
                        label: translateLabel,
                        onClick: async () =>
                        {
                            await TranslateMessage(message);
                        });

                    actionsRow.Button(["text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200"],
                        label: "📖 Details",
                        onClick: async () =>
                        {
                            await ShowDetailedTranslation(message.Content.Value);
                        });

                    var audioLabel = outer.GetAudioButtonLabel(message.Id);
                    var audioState = outer.GetMessageAudioState(message.Id);
                    var audioStyle = audioState == AudioPlaybackState.Playing
                        ? $"text-xs font-semibold px-3 py-1.5 rounded-full {LearningApp.Styles.GetAccentGradient(theme)} text-white shadow-sm transition-all duration-200"
                        : "text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100/80 hover:bg-gray-200/90 text-[#6b7280] hover:text-[#1a1a1a] transition-all duration-200";

                    actionsRow.Button([audioStyle],
                        label: audioState == AudioPlaybackState.Playing ? $"⏸ {audioLabel}" : $"🔊 {audioLabel}",
                        onClick: async () =>
                        {
                            await outer.PlayMessageAudioAsync(message.Id, message.Content.Value);
                        });
                });

                // Translation display
                if (message.ShowTranslation.Value && message.TranslationValue.Value != null)
                {
                    col.Box(["bg-white/60 p-4 rounded-2xl"], content: transView =>
                    {
                        transView.Column(["gap-2"], content: transCol =>
                        {
                            transCol.Text([$"text-sm {accentText} leading-relaxed"],
                                message.TranslationValue.Value.TranslatedMessage);

                            if (!string.IsNullOrEmpty(message.TranslationValue.Value.TransliteratedMessage))
                            {
                                transCol.Text(["text-sm text-[#6b7280] italic"],
                                    message.TranslationValue.Value.TransliteratedMessage);
                            }
                        });
                    });
                }
            });
        });
    }

    private void RenderDetailedTranslationPanel(UIView view)
    {
        var theme = outer.SelectedTheme.Value;

        view.Box([LearningApp.Styles.GlassCardStrong, "p-4 rounded-2xl mt-2"], content: panelView =>
        {
            panelView.Column(["gap-3"], content: col =>
            {
                col.Row(["justify-between items-center"], content: headerRow =>
                {
                    headerRow.Text([Text.Body, "font-semibold text-[#1a1a1a]"], "Word Details");
                    headerRow.Button([LearningApp.Styles.GhostXs, "text-[#6b7280] hover:text-[#1a1a1a]"], label: "Close", onClick: async () =>
                    {
                        _tokenizedWords.Value = [];
                        _detailedTranslationWord.Value = null;
                        _detailedTranslationResult.Value = null;
                        await Task.CompletedTask;
                    });
                });

                col.Row(["flex-wrap gap-2"], content: wordsRow =>
                {
                    foreach (var word in _tokenizedWords.Value)
                    {
                        var currentWord = word;
                        var isSelected = _detailedTranslationWord.Value == word;
                        var wordStyle = isSelected
                            ? $"{LearningApp.Styles.GetAccentGradient(theme)} text-white shadow-md"
                            : "bg-white/60 hover:bg-white/80 text-[#1a1a1a] border border-gray-200";

                        wordsRow.Button([$"px-3 py-1.5 rounded-xl text-sm cursor-pointer transition-all {wordStyle}"],
                            label: word,
                            onClick: async () =>
                            {
                                var context = string.Join(" ", _tokenizedWords.Value);
                                await GetWordDetails(currentWord, context);
                            });
                    }
                });

                if (_isLoadingDetailedTranslation.Value)
                {
                    col.Row(["items-center gap-2"], content: loadingRow =>
                    {
                        loadingRow.Icon([Icon.Default, $"w-4 h-4 {LearningApp.Styles.GetAccentText(theme)} animate-spin"], name: "loader");
                        loadingRow.Text([Text.Caption, "text-[#6b7280]"], "Loading...");
                    });
                }
                else if (_detailedTranslationResult.Value != null)
                {
                    var entry = _detailedTranslationResult.Value.WordEntry;

                    col.Box(["bg-white/50 rounded-2xl p-4"], content: detailsView =>
                    {
                        detailsView.Column(["gap-2"], content: detailsCol =>
                        {
                            detailsCol.Row(["gap-2 items-baseline flex-wrap"], content: wordRow =>
                            {
                                wordRow.Text([Text.H3, "text-[#1a1a1a]"], entry.WordTranslation.OriginalWord);

                                if (!string.IsNullOrEmpty(entry.WordTranslation.Transliteration))
                                {
                                    wordRow.Text([Text.Body, "text-[#6b7280] italic"], $"({entry.WordTranslation.Transliteration})");
                                }

                                wordRow.Text([Text.Body, LearningApp.Styles.GetAccentText(theme), "font-medium"], entry.WordTranslation.Translation);
                            });

                            detailsCol.Row(["gap-4 flex-wrap"], content: infoRow =>
                            {
                                if (!string.IsNullOrEmpty(entry.WordInformation.PartOfSpeech))
                                {
                                    infoRow.Text([Text.Caption, "text-[#6b7280]"], $"Part of speech: {entry.WordInformation.PartOfSpeech}");
                                }

                                if (!string.IsNullOrEmpty(entry.WordInformation.BasicForm))
                                {
                                    infoRow.Text([Text.Caption, "text-[#6b7280]"], $"Dictionary form: {entry.WordInformation.BasicForm}");
                                }
                            });

                            if (!string.IsNullOrEmpty(entry.Explanation))
                            {
                                detailsCol.Text([Text.Caption, "text-[#1a1a1a]"], entry.Explanation);
                            }

                            if (entry.Examples.Count > 0)
                            {
                                detailsCol.Column(["gap-1 mt-2"], content: examplesCol =>
                                {
                                    examplesCol.Text([Text.Caption, "font-semibold text-[#1a1a1a]"], "Examples:");
                                    foreach (var example in entry.Examples.Take(2))
                                    {
                                        examplesCol.Text([Text.Caption, "text-[#1a1a1a]"], $"• {example.Sentence}");
                                        examplesCol.Text([Text.Caption, "text-[#6b7280] ml-3"], example.Translation);
                                    }
                                });
                            }
                        });
                    });
                }
            });
        });
    }

    private void RenderInputArea(UIView view)
    {
        var theme = outer.SelectedTheme.Value;
        var isDisabled = _isGeneratingFeedback.Value || _hasConversationEnded;

        view.Column(["gap-2 md:gap-3"], content: inputColumn =>
        {
            // Hint button - subtle, centered
            if (!_hasConversationEnded)
            {
                inputColumn.Row(["justify-center"], content: hintRow =>
                {
                    hintRow.Button(["text-xs md:text-sm font-medium text-[#6b7280] hover:text-[#1a1a1a] px-3 py-1.5 rounded-lg hover:bg-white/50 transition-colors"],
                        label: _isGeneratingHint.Value ? "Generating hint..." : "💡 Need a hint?",
                        disabled: _isGeneratingHint.Value,
                        onClick: async () =>
                        {
                            await RequestHint();
                        });
                });
            }

            // Clean input card
            inputColumn.Box([LearningApp.Styles.GlassCardStrong, "px-3 py-2.5 md:px-4 md:py-3 rounded-2xl"], content: inputCard =>
            {
                inputCard.Row(["gap-2 md:gap-3 items-center"], content: inputView =>
                {
                    // Microphone button - round and large for easy mobile press
                    var micStyle = outer.IsRecording.Value
                        ? "w-12 h-12 md:w-14 md:h-14 rounded-full bg-red-500 text-white flex items-center justify-center animate-pulse shadow-lg text-xl"
                        : "w-12 h-12 md:w-14 md:h-14 rounded-full bg-white/80 hover:bg-white text-[#6b7280] hover:text-[#1a1a1a] flex items-center justify-center transition-all duration-200 border border-gray-200/50 shadow-sm text-xl";

                    inputView.CaptureButton(
                        [micStyle],
                        kind: MediaCaptureKind.Audio,
                        label: "🎤",
                        captureMode: MediaCaptureButtonMode.Hold,
                        disabled: _hasConversationEnded,
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
                            _audioInputUsed = true;
                            await Task.CompletedTask;
                        },
                        onCaptureStop: async e =>
                        {
                            outer.IsRecording.Value = false;
                            await Task.CompletedTask;
                        });

                    inputView.TextField(["flex-1 bg-transparent border-0 outline-none px-2 py-1.5 md:px-3 md:py-2 text-[#1a1a1a] placeholder-[#9ca3af] text-[15px]"],
                        value: outer.InputText.Value,
                        placeholder: _hasConversationEnded ? "Harjoitus valmis!" : "Kirjoita vastauksesi...",
                        disabled: _hasConversationEnded,
                        onValueChange: value =>
                        {
                            outer.InputText.Value = value;
                            return Task.CompletedTask;
                        },
                        onSubmit: async _ =>
                        {
                            if (_hasConversationEnded)
                            {
                                return;
                            }

                            var text = outer.InputText.Value;

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                outer.InputText.Value = "";
                                outer.AddChatMessage(ChatRole.User, text);
                                await GenerateFeedbackForResponse(text);
                                await outer.ProcessUserMessageAsync(text);
                            }
                        });

                    // Send button - rightmost, themed gradient
                    var sendButtonStyle = isDisabled
                        ? "bg-gray-200 text-gray-400 px-4 md:px-5 py-2 md:py-2.5 rounded-xl font-semibold text-sm md:text-base cursor-not-allowed"
                        : $"{LearningApp.Styles.GetAccentGradient(theme)} hover:opacity-90 text-white px-4 md:px-5 py-2 md:py-2.5 rounded-xl font-semibold text-sm md:text-base transition-opacity shadow-sm";

                    inputView.Button([sendButtonStyle],
                        label: _isGeneratingFeedback.Value ? "..." : "Lähetä",
                        disabled: isDisabled,
                        onClick: async () =>
                        {
                            if (_hasConversationEnded)
                            {
                                return;
                            }

                            var text = outer.InputText.Value;

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                outer.InputText.Value = "";
                                outer.AddChatMessage(ChatRole.User, text);
                                await GenerateFeedbackForResponse(text);
                                await outer.ProcessUserMessageAsync(text);
                            }
                        });
                });
            });
        });
    }

    private void RenderReport(UIView contentView, Report report)
    {
        var theme = outer.SelectedTheme.Value;

        contentView.Column(["flex-1 gap-5 p-4"], content: view =>
        {
            // Header card with score
            view.Box([LearningApp.Styles.GlassCardStrong, "p-6 rounded-3xl text-center", LearningApp.Styles.SlideUp], content: headerCard =>
            {
                headerCard.Column(["gap-4 items-center"], content: col =>
                {
                    col.Text([Text.H1, "text-[#1a1a1a]"], "Exercise Complete!");
                    col.Text([Text.H2, LearningApp.Styles.GetAccentText(theme), "font-bold text-4xl"], $"{report.OverallScore}/100");

                    var scoreColor = report.OverallScore >= 80 ? "text-green-600" :
                                     report.OverallScore >= 60 ? "text-amber-600" : "text-red-500";
                    col.Text([Text.Body, scoreColor, "font-medium"], report.OverallScore >= 80 ? "Excellent!" :
                                                      report.OverallScore >= 60 ? "Good job!" : "Keep practicing!");
                });
            });

            // Score cards row
            view.Row(["gap-3 flex-wrap"], content: scoresRow =>
            {
                RenderScoreCard(scoresRow, "Task", report.ScoreDimensions.TaskFulfillment);
                RenderScoreCard(scoresRow, "Structure", report.ScoreDimensions.OrganizationAndStructure);
                RenderScoreCard(scoresRow, "Accuracy", report.ScoreDimensions.LinguisticResourceAndAccuracy);
            });

            // Feedback card
            view.Box([LearningApp.Styles.GlassCard, "p-5 rounded-2xl"], content: feedbackCard =>
            {
                feedbackCard.Column(["gap-2"], content: col =>
                {
                    col.Text([Text.Body, "font-semibold text-[#1a1a1a]"], "Feedback");
                    col.Text([Text.Body, "text-[#1a1a1a] leading-relaxed"], report.Feedback);
                });
            });

            // Key takeaways card
            if (!string.IsNullOrEmpty(report.KeyTakeaways))
            {
                view.Box([LearningApp.Styles.GetGoalsCard(theme), LearningApp.Styles.GetGoalsBorder(theme), "p-5 rounded-2xl shadow-sm"], content: takeawaysCard =>
                {
                    takeawaysCard.Column(["gap-2"], content: col =>
                    {
                        col.Text([Text.Body, "font-semibold", LearningApp.Styles.GetGoalsTitle(theme)], "💡 Key Takeaways");
                        col.Text([Text.Body, "leading-relaxed", LearningApp.Styles.GetGoalsText(theme)], report.KeyTakeaways);
                    });
                });
            }

            view.Text([Text.Caption, "text-[#6b7280] text-center"],
                $"Time taken: {report.TimeTaken / 60}m {report.TimeTaken % 60}s");

            // Action buttons
            view.Row(["gap-4 justify-center mt-4"], content: buttonsRow =>
            {
                buttonsRow.Button(["bg-white/60 hover:bg-white/80 text-[#1a1a1a] px-6 py-3 rounded-xl font-medium border border-gray-200 transition-colors"],
                    label: "Back to Menu",
                    onClick: async () =>
                    {
                        await outer.States.StateMachine.FireAsync(Trigger.ReturnToMainMenu);
                    });

                buttonsRow.Button([$"{LearningApp.Styles.GetAccentGradient(theme)} text-white px-6 py-3 rounded-xl font-medium shadow-lg hover:opacity-90 transition-opacity"],
                    label: "Try Again",
                    onClick: async () =>
                    {
                        await EnterAsync();
                    });
            });
        });
    }

    private void RenderScoreCard(UIView row, string label, float score)
    {
        var scoreColor = GetScoreColor(score);

        row.Box([LearningApp.Styles.GlassCard, "flex-1 min-w-[100px] p-4 rounded-2xl text-center"], content: card =>
        {
            card.Column(["gap-1 items-center"], content: col =>
            {
                col.Text([Text.Caption, "text-[#6b7280]"], label);
                col.Text([Text.H3, scoreColor, "font-semibold"], $"{score:F1}/3");
            });
        });
    }
}
