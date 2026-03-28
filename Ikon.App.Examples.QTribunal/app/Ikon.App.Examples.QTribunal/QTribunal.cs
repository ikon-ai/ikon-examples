using System.Threading.Channels;

return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParams;

[App]
public partial class QTribunal(IApp<SessionIdentity, ClientParams> app)
{
    private const int TotalRounds = 3;
    private const int MaxActionsPerRound = 5;

    private UI UI { get; } = new(app, new Theme());

    // Game state
    private readonly Reactive<GamePhase> _phase = new(GamePhase.Intro);
    private readonly Reactive<int> _currentRound = new(0);
    private readonly Reactive<int> _actionsRemaining = new(MaxActionsPerRound);
    private readonly Reactive<float> _proximity = new(0f);
    private readonly Reactive<float> _lastImageProximity = new(-1f);
    private readonly Reactive<bool> _isProcessing = new(false);

    // Scene image
    private readonly Reactive<byte[]?> _sceneImageData = new(null);
    private readonly Reactive<string?> _sceneImageMime = new(null);

    // Transcript
    private readonly List<TranscriptEntry> _transcript = [];
    private readonly Reactive<int> _transcriptVersion = new(0);

    // Input
    private readonly Reactive<string> _inputText = new("");

    // Round tracking
    private Accusation? _currentAccusation;
    private readonly List<RoundResult> _roundResults = [];

    // Message queue
    private readonly Channel<string> _commandQueue = Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _appCts = new();
    private bool _commandProcessorStarted;

    public async Task Main()
    {
        app.StoppingAsync += async _ =>
        {
            await _appCts.CancelAsync();
            _commandQueue.Writer.TryComplete();
            _appCts.Dispose();
        };

        UI.Root(style: [Styles.Root], content: view =>
        {
            if (_phase.Value == GamePhase.FinalVerdict)
            {
                RenderFinalVerdict(view);
                return;
            }

            RenderHeader(view);

            if (_sceneImageData.Value != null && _sceneImageMime.Value != null)
            {
                RenderSceneImage(view);
            }
            else if (_phase.Value == GamePhase.Teleportation)
            {
                RenderTeleportPlaceholder(view);
            }

            RenderProximityBar(view);
            RenderTranscript(view);

            if (_phase.Value == GamePhase.Investigation || _phase.Value == GamePhase.Proposal || _phase.Value == GamePhase.Intro)
            {
                RenderInput(view);
            }
        });
    }

    private void RenderHeader(UIView view)
    {
        view.Row(style: [Styles.Header.Bar], content: view =>
        {
            view.Text(style: [Styles.Header.Title, Styles.Header.TitleMotion], text: "THE Q TRIBUNAL");

            view.Row(style: ["gap-4 items-center"], content: view =>
            {
                if (_currentRound.Value > 0)
                {
                    view.Text(style: [Styles.Header.Stats], text: $"Round {_currentRound.Value}/{TotalRounds}");
                }

                if (_phase.Value == GamePhase.Investigation)
                {
                    view.Text(style: [Styles.Header.Stats], text: $"Actions: {_actionsRemaining.Value}");
                }

                if (_roundResults.Count > 0)
                {
                    var totalScore = _roundResults.Sum(r => r.Accuracy);
                    view.Text(style: [Styles.Header.Stats], text: "Score: ");
                    view.Text(style: [Styles.Header.Score], text: $"{totalScore:F1}");
                }
            });
        });
    }

    private void RenderSceneImage(UIView view)
    {
        view.Box(style: [Styles.Scene.Container], content: view =>
        {
            view.Box(style: [Styles.Scene.ImageWrapper], content: view =>
            {
                view.Image(
                    style: [Styles.Scene.Image, Styles.Scene.ImageMotion],
                    data: _sceneImageData.Value,
                    mimeType: _sceneImageMime.Value,
                    alt: "Trial scene"
                );
            });
        });
    }

    private void RenderTeleportPlaceholder(UIView view)
    {
        view.Box(style: [Styles.Scene.Container], content: view =>
        {
            view.Box(style: [Styles.Scene.ImageWrapper], content: view =>
            {
                view.Box(style: [Styles.Scene.Placeholder], content: view =>
                {
                    view.Text(style: [Styles.Scene.PlaceholderText], text: "[ TELEPORTING... ]");
                });
            });
        });
    }

    private void RenderProximityBar(UIView view)
    {
        if (_phase.Value != GamePhase.Investigation && _phase.Value != GamePhase.Proposal)
        {
            return;
        }

        var pct = Math.Clamp(_proximity.Value * 100, 0, 100);
        var color = _proximity.Value switch
        {
            < 0.3f => "bg-red-500",
            < 0.6f => "bg-amber-500",
            _ => "bg-emerald-500"
        };

        view.Box(style: [Styles.Scene.ProximityBar], content: view =>
        {
            view.Box(style: [$"{color} h-full transition-all duration-500", $"w-[{pct:F0}%]"]);
        });
    }

    private void RenderTranscript(UIView view)
    {
        var _ = _transcriptVersion.Value;

        view.Box(style: [Styles.Transcript.Container], content: view =>
        {
            view.ScrollArea(
                autoScroll: true,
                autoScrollKey: _transcriptVersion.Value.ToString(),
                rootStyle: [ScrollArea.Root, "h-full"],
                content: scrollView =>
                {
                    foreach (var entry in _transcript)
                    {
                        RenderTranscriptEntry(scrollView, entry);
                    }
                });
        });
    }

    private void RenderTranscriptEntry(UIView view, TranscriptEntry entry)
    {
        var (entryStyle, speakerStyle, textStyle) = entry.Role switch
        {
            TranscriptRole.Q => (Styles.Transcript.QEntry, Styles.Transcript.QSpeaker, Styles.Transcript.QText),
            TranscriptRole.Player => (Styles.Transcript.PlayerEntry, Styles.Transcript.PlayerSpeaker, Styles.Transcript.PlayerText),
            TranscriptRole.Narrator => (Styles.Transcript.NarratorEntry, Styles.Transcript.NarratorSpeaker, Styles.Transcript.NarratorText),
            TranscriptRole.Witness => (Styles.Transcript.WitnessEntry, Styles.Transcript.WitnessSpeaker, Styles.Transcript.WitnessText),
            _ => (Styles.Transcript.SystemEntry, Styles.Transcript.SystemSpeaker, Styles.Transcript.SystemText)
        };

        var textMotion = entry.Role == TranscriptRole.Q ? Styles.Transcript.QTextMotion : "";

        view.Box(style: [Styles.Transcript.EntryBase, entryStyle, Styles.Transcript.EntryMotion], content: view =>
        {
            view.Text(style: [speakerStyle], text: entry.Speaker);
            view.Text(style: [textStyle, textMotion], text: entry.Text);
        });
    }

    private void RenderInput(UIView view)
    {
        var isIntro = _phase.Value == GamePhase.Intro;
        var isProposal = _phase.Value == GamePhase.Proposal;
        var placeholder = isIntro
            ? "Type 'begin' to start the trial..."
            : isProposal
                ? "propose [your theory about the hidden law]"
                : "examine, ask, reflect, look, or propose...";

        view.Column(style: [Styles.Input.Container], content: view =>
        {
            view.Row(style: [Styles.Input.Row], content: view =>
            {
                view.TextField(
                    style: [Styles.Input.Field],
                    value: _inputText.Value,
                    placeholder: placeholder,
                    onValueChange: value =>
                    {
                        _inputText.Value = value;
                        return Task.CompletedTask;
                    },
                    onSubmit: async _ =>
                    {
                        SubmitCommand();
                    });

                var canSend = !_isProcessing.Value && !string.IsNullOrWhiteSpace(_inputText.Value);
                view.Button(
                    style: [canSend ? Styles.Input.SendButton : Styles.Input.SendButtonDisabled],
                    label: _isProcessing.Value ? "..." : "Send",
                    disabled: !canSend,
                    onClick: async () =>
                    {
                        SubmitCommand();
                    });
            });

            if (_phase.Value == GamePhase.Investigation)
            {
                view.Row(style: [Styles.Input.HintRow], content: view =>
                {
                    string[] hints = ["examine", "ask", "reflect", "look", "propose"];
                    foreach (var hint in hints)
                    {
                        var h = hint;
                        view.Button(
                            style: [Styles.Input.HintButton],
                            label: hint,
                            onClick: async () =>
                            {
                                _inputText.Value = h + " ";
                            });
                    }
                });
            }
        });
    }

    private void RenderFinalVerdict(UIView view)
    {
        var totalScore = _roundResults.Sum(r => r.Accuracy);
        var maxScore = TotalRounds;

        view.Box(style: [Styles.FinalVerdict.Container], content: view =>
        {
            view.Column(style: [Styles.FinalVerdict.Card], content: view =>
            {
                view.Text(style: [Styles.FinalVerdict.Title, Styles.Header.TitleMotion], text: "FINAL VERDICT");

                foreach (var round in _roundResults)
                {
                    view.Row(style: [Styles.FinalVerdict.RoundRow], content: view =>
                    {
                        view.Column(content: view =>
                        {
                            view.Text(style: [Styles.FinalVerdict.RoundLabel], text: $"Round {round.RoundNumber}: {round.Crime}");
                            view.Text(style: ["text-xs text-gray-500 mt-0.5"], text: $"Law: {round.HiddenLaw}");
                        });

                        var scoreColor = round.Accuracy >= 0.7f ? "text-emerald-400" : round.Accuracy >= 0.4f ? "text-amber-400" : "text-red-400";
                        view.Text(style: [Styles.FinalVerdict.RoundScore, scoreColor], text: $"{round.Accuracy:F1}");
                    });
                }

                view.Row(style: [Styles.FinalVerdict.TotalRow], content: view =>
                {
                    view.Text(style: [Styles.FinalVerdict.TotalLabel], text: "Total Score");
                    view.Text(style: [Styles.FinalVerdict.TotalScore], text: $"{totalScore:F1} / {maxScore}");
                });

                view.Button(
                    style: [Styles.FinalVerdict.PlayAgain],
                    label: "Face Trial Again",
                    onClick: async () =>
                    {
                        ResetGame();
                    });
            });
        });

        // Final Q quote in transcript area
        view.Box(style: ["px-6 py-4"], content: view =>
        {
            var verdictText = totalScore >= 2.0f
                ? "Perhaps... perhaps there is hope for your species after all. The tribunal is adjourned -- for now."
                : totalScore >= 1.0f
                    ? "Mediocrity. The universe expected more from a species that dares to call itself sentient."
                    : "Disappointing. Truly, profoundly disappointing. Humanity's trial shall continue... indefinitely.";

            view.Text(style: [Styles.Transcript.QText, Styles.Transcript.QTextMotion, "text-center"], text: $"Q: \"{verdictText}\"");
        });
    }

    private void SubmitCommand()
    {
        var text = _inputText.Value.Trim();

        if (string.IsNullOrWhiteSpace(text) || _isProcessing.Value)
        {
            return;
        }

        _inputText.Value = "";
        _commandQueue.Writer.TryWrite(text);

        if (!_commandProcessorStarted)
        {
            _commandProcessorStarted = true;
            ProcessCommandQueueAsync().RunParallel();
        }
    }

    private async Task ProcessCommandQueueAsync()
    {
        var ct = _appCts.Token;

        try
        {
            await foreach (var commandText in _commandQueue.Reader.ReadAllAsync(ct))
            {
                _isProcessing.Value = true;

                try
                {
                    await HandleCommandAsync(commandText, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AddTranscript(TranscriptRole.System, "System", $"Error: {ex.Message}");
                }
                finally
                {
                    _isProcessing.Value = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async Task HandleCommandAsync(string commandText, CancellationToken ct)
    {
        switch (_phase.Value)
        {
            case GamePhase.Intro:
                if (commandText.Equals("begin", StringComparison.OrdinalIgnoreCase))
                {
                    await StartNewRoundAsync(ct);
                }
                else
                {
                    AddTranscript(TranscriptRole.System, "System", "Type 'begin' to start the trial");
                }
                break;

            case GamePhase.Investigation:
            {
                var parsed = ParseCommand(commandText);

                if (parsed == null)
                {
                    AddTranscript(TranscriptRole.System, "System", "Use: examine [target], ask [witness] [question], reflect [thought], look [direction], or propose [theory]");
                    break;
                }

                if (parsed.Type == CommandType.Propose)
                {
                    _phase.Value = GamePhase.Proposal;
                    await HandleProposalAsync(parsed.Argument, ct);
                    break;
                }

                if (_actionsRemaining.Value <= 0)
                {
                    AddTranscript(TranscriptRole.Q, "Q", "Time's up! You must propose your theory now. Use: propose [your theory]");
                    _phase.Value = GamePhase.Proposal;
                    break;
                }

                AddTranscript(TranscriptRole.Player, "You", $"> {commandText}");
                _actionsRemaining.Value--;

                var response = await ProcessInvestigationAsync(
                    _currentAccusation!,
                    parsed,
                    _transcript,
                    _proximity.Value,
                    ct
                );

                _proximity.Value = Math.Clamp(response.ProximityToTruth, 0f, 1f);
                AddTranscript(TranscriptRole.Narrator, "Narrator", response.Narration);

                if (!string.IsNullOrWhiteSpace(response.QComment))
                {
                    AddTranscript(TranscriptRole.Q, "Q", response.QComment);
                }

                await MaybeRegenerateImageAsync(ct);

                if (_actionsRemaining.Value == 0)
                {
                    AddTranscript(TranscriptRole.Q, "Q", "Your time for investigation has expired. Now -- what is your theory? Use: propose [your theory]");
                    _phase.Value = GamePhase.Proposal;
                }
                break;
            }

            case GamePhase.Proposal:
            {
                var parsed = ParseCommand(commandText);

                if (parsed == null || parsed.Type != CommandType.Propose)
                {
                    // Treat any input as a proposal in Proposal phase
                    var theory = commandText.StartsWith("propose ", StringComparison.OrdinalIgnoreCase)
                        ? commandText[8..].Trim()
                        : commandText;
                    await HandleProposalAsync(theory, ct);
                }
                else
                {
                    await HandleProposalAsync(parsed.Argument, ct);
                }
                break;
            }
        }
    }

    private async Task StartNewRoundAsync(CancellationToken ct)
    {
        _currentRound.Value++;
        _actionsRemaining.Value = MaxActionsPerRound;
        _proximity.Value = 0f;
        _lastImageProximity.Value = -1f;
        _sceneImageData.Value = null;
        _sceneImageMime.Value = null;
        _phase.Value = GamePhase.Accusation;

        if (_currentRound.Value == 1)
        {
            AddTranscript(TranscriptRole.Q, "Q", "Humanity! You have been summoned before the Continuum. Your species stands accused of crimes against the universal order. Let the trial... begin.");
        }

        AddTranscript(TranscriptRole.System, "System", $"--- Round {_currentRound.Value} of {TotalRounds} ---");

        _currentAccusation = await GenerateAccusationAsync(_currentRound.Value, _roundResults, ct);
        AddTranscript(TranscriptRole.Q, "Q", _currentAccusation.QSpeech);
        AddTranscript(TranscriptRole.System, "System", $"The charge: {_currentAccusation.Crime}");

        _phase.Value = GamePhase.Teleportation;
        AddTranscript(TranscriptRole.Narrator, "Narrator", $"A flash of light -- you materialize in {_currentAccusation.Scene.LocationName}.");
        AddTranscript(TranscriptRole.Narrator, "Narrator", _currentAccusation.Scene.Description);

        // Generate scene image
        var imageResult = await GenerateSceneImageAsync(_currentAccusation.Scene.ImagePrompt, 0f, ct);

        if (imageResult != null)
        {
            _sceneImageData.Value = imageResult.Value.Data;
            _sceneImageMime.Value = imageResult.Value.MimeType;
            _lastImageProximity.Value = 0f;
        }

        _phase.Value = GamePhase.Investigation;

        var witnessNames = string.Join(", ", _currentAccusation.Scene.Witnesses.Select(w => w.Name));
        AddTranscript(TranscriptRole.System, "System", $"Witnesses present: {witnessNames}");
        AddTranscript(TranscriptRole.System, "System", $"You have {MaxActionsPerRound} actions. Use: examine, ask, reflect, look, or propose");
    }

    private async Task HandleProposalAsync(string theory, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(theory))
        {
            AddTranscript(TranscriptRole.System, "System", "You must provide a theory. Use: propose [your theory about the hidden universal law]");
            return;
        }

        AddTranscript(TranscriptRole.Player, "You", $"\"I propose that the hidden law is: {theory}\"");
        _phase.Value = GamePhase.Judgment;

        var judgment = await JudgeProposalAsync(_currentAccusation!, theory, ct);

        AddTranscript(TranscriptRole.Q, "Q", judgment.QSpeech);
        AddTranscript(TranscriptRole.Narrator, "Narrator", judgment.Explanation);

        var scoreText = judgment.Correct ? "CORRECT" : "INCORRECT";
        AddTranscript(TranscriptRole.System, "System", $"Verdict: {scoreText} (Accuracy: {judgment.Accuracy:P0})");
        AddTranscript(TranscriptRole.System, "System", $"The hidden law was: {_currentAccusation!.HiddenLaw}");

        _roundResults.Add(new RoundResult
        {
            RoundNumber = _currentRound.Value,
            Crime = _currentAccusation.Crime,
            HiddenLaw = _currentAccusation.HiddenLaw,
            PlayerTheory = theory,
            Accuracy = judgment.Accuracy,
            Correct = judgment.Correct
        });

        if (_currentRound.Value >= TotalRounds)
        {
            _phase.Value = GamePhase.FinalVerdict;
        }
        else
        {
            AddTranscript(TranscriptRole.Q, "Q", "The trial continues. Prepare yourself for the next charge...");
            await StartNewRoundAsync(ct);
        }
    }

    private async Task MaybeRegenerateImageAsync(CancellationToken ct)
    {
        if (_currentAccusation == null)
        {
            return;
        }

        var currentThreshold = _proximity.Value switch
        {
            >= 0.6f => 0.6f,
            >= 0.3f => 0.3f,
            _ => 0f
        };

        var lastThreshold = _lastImageProximity.Value switch
        {
            >= 0.6f => 0.6f,
            >= 0.3f => 0.3f,
            _ => 0f
        };

        if (currentThreshold <= lastThreshold)
        {
            return;
        }

        var imageResult = await GenerateSceneImageAsync(_currentAccusation.Scene.ImagePrompt, _proximity.Value, ct);

        if (imageResult != null)
        {
            _sceneImageData.Value = imageResult.Value.Data;
            _sceneImageMime.Value = imageResult.Value.MimeType;
            _lastImageProximity.Value = _proximity.Value;
        }
    }

    private void AddTranscript(TranscriptRole role, string speaker, string text)
    {
        _transcript.Add(new TranscriptEntry(role, speaker, text));
        _transcriptVersion.Value++;
    }

    private void ResetGame()
    {
        _phase.Value = GamePhase.Intro;
        _currentRound.Value = 0;
        _actionsRemaining.Value = MaxActionsPerRound;
        _proximity.Value = 0f;
        _lastImageProximity.Value = -1f;
        _sceneImageData.Value = null;
        _sceneImageMime.Value = null;
        _currentAccusation = null;
        _roundResults.Clear();
        _transcript.Clear();
        _transcriptVersion.Value++;

        AddTranscript(TranscriptRole.Q, "Q", "Ah, you dare to face the tribunal again? How... delightfully human of you.");
        AddTranscript(TranscriptRole.System, "System", "Type 'begin' to start a new trial");
    }

    private static ParsedCommand? ParseCommand(string input)
    {
        var trimmed = input.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        var verb = spaceIndex > 0 ? trimmed[..spaceIndex].ToLowerInvariant() : trimmed.ToLowerInvariant();
        var argument = spaceIndex > 0 ? trimmed[(spaceIndex + 1)..].Trim() : "";

        return verb switch
        {
            "examine" or "x" => new ParsedCommand(CommandType.Examine, string.IsNullOrEmpty(argument) ? "surroundings" : argument),
            "ask" or "a" => new ParsedCommand(CommandType.Ask, string.IsNullOrEmpty(argument) ? "the nearest witness" : argument),
            "reflect" or "r" => new ParsedCommand(CommandType.Reflect, string.IsNullOrEmpty(argument) ? "on what I've seen" : argument),
            "look" or "l" => new ParsedCommand(CommandType.Look, string.IsNullOrEmpty(argument) ? "around" : argument),
            "propose" or "p" => new ParsedCommand(CommandType.Propose, argument),
            _ => null
        };
    }
}
