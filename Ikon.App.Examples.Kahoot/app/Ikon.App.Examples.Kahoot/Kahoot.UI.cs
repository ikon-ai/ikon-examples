public partial class Kahoot
{
    private static readonly string[] AnswerColors = ["bg-[#e21b3c]", "bg-[#1368ce]", "bg-[#26890c]", "bg-[#d89e00]"];
    private static readonly string[] AnswerIcons = ["triangle", "diamond", "circle", "square"];

    private const string AuroraGradient = "bg-[radial-gradient(ellipse_at_top_left,rgba(59,130,246,0.25)_0%,transparent_50%),radial-gradient(ellipse_at_top_right,rgba(139,92,246,0.2)_0%,transparent_45%),radial-gradient(ellipse_at_bottom,rgba(6,182,212,0.2)_0%,transparent_50%)]";

    private void RenderUI(UIView view)
    {
        var clientId = ReactiveScope.ClientId;
        var client = app.Clients[clientId];

        if (!IsValidSessionId(client?.Parameters.Id))
        {
            RenderCreateSession(view);
            return;
        }

        if (IsHost())
        {
            RenderHostView(view);
        }
        else
        {
            RenderPlayerView(view);
        }
    }

    private void RenderCreateSession(UIView view)
    {
        view.Column(style: ["w-full h-screen bg-black text-white relative overflow-hidden", AuroraGradient], content: view =>
        {
            view.Column(style: ["flex-1 items-center justify-center gap-6"], content: view =>
            {
                view.Text(
                    style: [
                        "text-7xl font-bold text-purple-400 drop-shadow-[0_0_20px_rgba(168,85,247,0.5)]",
                        "motion-[0:brightness-100,40:brightness-[1.8],100:brightness-100]",
                        "motion-duration-1500ms motion-stagger-200ms motion-per-letter-loop"
                    ],
                    text: "Ikon Kahoot"
                );

                view.Text(
                    style: ["text-xl text-gray-400 mt-2"],
                    text: "Test your Ikon platform knowledge!"
                );

                view.Button(
                    style: ["px-8 py-4 text-xl rounded-full border border-white/30 bg-transparent text-white hover:bg-white/10 mt-4"],
                    href: GetCreateSessionUrl(),
                    content: v =>
                    {
                        v.Row(style: ["items-center gap-3"], content: v =>
                        {
                            v.Icon(["w-5 h-5"], name: "play");
                            v.Text(text: "Start Game!");
                        });
                    }
                );
            });
        });
    }

    // ==================== HOST VIEW ====================

    private void RenderHostView(UIView view)
    {
        view.Column(style: ["w-full h-screen bg-black text-white relative overflow-hidden", AuroraGradient], content: view =>
        {
            switch (_gameStage.Value)
            {
                case GameStage.Lobby:
                    RenderHostLobby(view);
                    break;
                case GameStage.CountdownToStart:
                    RenderHostCountdownToStart(view);
                    break;
                case GameStage.Question:
                    RenderHostQuestion(view);
                    break;
                case GameStage.Feedback:
                    RenderHostFeedback(view);
                    break;
                case GameStage.Leaderboard:
                    RenderHostLeaderboard(view);
                    break;
                case GameStage.GameOver:
                    RenderHostGameOver(view);
                    break;
            }
        });
    }

    private void RenderHostLobby(UIView view)
    {
        var playerCount = _players.Value.Count;
        var isFull = playerCount >= MaxPlayers;

        view.Column(style: ["flex-1 items-center gap-8 p-8 min-h-0"], content: view =>
        {
            view.Text(
                style: [
                    "text-6xl font-bold text-purple-400 drop-shadow-[0_0_15px_rgba(168,85,247,0.5)] shrink-0",
                    "motion-[0:brightness-100,40:brightness-[1.8],100:brightness-100]",
                    "motion-duration-2000ms motion-stagger-150ms motion-per-letter-loop"
                ],
                text: "Ikon Kahoot"
            );

            view.Row(style: ["gap-12 items-start flex-1 min-h-0"], content: view =>
            {
                if (!isFull)
                {
                    view.Column(style: ["items-center gap-3 shrink-0"], content: view =>
                    {
                        var joinUrl = GetJoinUrl();
                        view.QR(
                            style: ["w-64 h-64 bg-white p-3 rounded-2xl"],
                            value: joinUrl,
                            size: 400
                        );

                        view.Text(style: ["text-2xl font-bold text-white mt-2"], text: "Scan to join!");
                        view.Button(
                            style: [Button.GhostMd, "text-sm text-gray-400 hover:text-white"],
                            label: "Open player view",
                            href: GetJoinUrl(),
                            target: "_blank"
                        );
                    });
                }
                else
                {
                    view.Column(style: ["items-center gap-2 shrink-0 justify-center"], content: view =>
                    {
                        view.Icon(["w-12 h-12 text-yellow-400"], name: "users");
                        view.Text(style: ["text-2xl font-bold text-yellow-400"], text: "Lobby Full");
                        view.Text(style: ["text-sm text-gray-400"], text: $"{MaxPlayers} players max");
                    });
                }

                if (playerCount > 0)
                {
                    view.Column(style: ["gap-3 min-w-[200px] min-h-0 flex-1"], content: view =>
                    {
                        view.Text(style: ["text-xl font-semibold text-white shrink-0"], text: $"Players ({playerCount})");

                        view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: view =>
                        {
                            view.Column(style: ["gap-3"], content: view =>
                            {
                                foreach (var player in _players.Value)
                                {
                                    view.Row(style: [
                                        "px-4 py-2 rounded-xl bg-white/10 backdrop-blur-sm",
                                        "motion-[0:opacity-0_translate-x-4,100:opacity-100_translate-x-0]",
                                        "motion-duration-300ms motion-ease-out motion-fill-both"
                                    ], content: view =>
                                    {
                                        view.Icon(["w-5 h-5 text-purple-400"], name: "user");
                                        view.Text(style: ["text-lg text-white ml-2"], text: player.Name);
                                    });
                                }
                            });
                        });
                    });
                }
            });

            if (playerCount > 0)
            {
                view.Button(
                    style: ["px-10 py-4 text-2xl rounded-full bg-purple-600 hover:bg-purple-500 text-white font-bold shrink-0"],
                    label: "Start Game!",
                    onClick: async () => await StartGameAsync()
                );
            }
        });
    }

    private void RenderHostCountdownToStart(UIView view)
    {
        view.Column(style: ["flex-1 items-center justify-center"], content: view =>
        {
            view.Text(
                style: [
                    "text-[200px] font-black text-white",
                    "motion-[0:scale-[3]_opacity-0,100:scale-[1]_opacity-100]",
                    "motion-duration-800ms motion-ease-[cubic-bezier(0.34,1.56,0.64,1)] motion-fill-forwards"
                ],
                text: _countdown.Value.ToString()
            );
        });
    }

    private void RenderHostQuestion(UIView view)
    {
        view.Column(style: ["flex-1 p-8 gap-6"], content: view =>
        {
            view.Row(style: ["justify-between items-center"], content: view =>
            {
                view.Text(
                    style: ["text-lg text-gray-400"],
                    text: $"Question {_questionNumber.Value} / {_totalQuestions.Value}"
                );

                var answeredCount = _playerAnswers.Count;
                var totalPlayers = _players.Value.Count;
                view.Text(
                    style: ["text-lg text-gray-400"],
                    text: $"{answeredCount} / {totalPlayers} answered"
                );
            });

            if (_currentQuestion.Value != null)
            {
                view.Column(style: ["flex-1 items-center justify-center gap-8"], content: view =>
                {
                    if (!string.IsNullOrEmpty(_currentQuestion.Value.Category))
                    {
                        view.Text(
                            style: ["text-sm font-medium text-purple-400 uppercase tracking-widest"],
                            text: _currentQuestion.Value.Category
                        );
                    }

                    view.Text(
                        style: [
                            "text-4xl font-bold text-white text-center max-w-4xl",
                            "motion-[0:opacity-0_translate-y-4,100:opacity-100_translate-y-0]",
                            "motion-duration-400ms motion-ease-out motion-fill-both"
                        ],
                        text: _currentQuestion.Value.Question
                    );

                    RenderHostCountdownBar(view);

                    view.Row(style: ["w-full max-w-5xl gap-4"], content: view =>
                    {
                        RenderHostAnswerGrid(view);
                    });
                });
            }
        });
    }

    private void RenderHostCountdownBar(UIView view)
    {
        var durationMs = _countdownSeconds.Value * 1000;

        view.Row(style: ["w-full max-w-3xl"], content: view =>
        {
            view.Row(style: ["w-full h-4 rounded-full border border-white/20 overflow-hidden relative"], content: view =>
            {
                view.Row(style: [
                    "h-full w-full rounded-full",
                    "motion-[0:translate-x-0_bg-[#7DD23A],50:translate-x-[-50%]_bg-[#FF7D00],80:-translate-x-[-80%]_bg-[#FF3241],100:-translate-x-[100%]_bg-[#FF3241]]",
                    $"motion-duration-{durationMs}ms motion-ease-linear motion-fill-forwards"
                ]);
            });
        });
    }

    private void RenderHostAnswerGrid(UIView view)
    {
        if (_currentQuestion.Value == null)
        {
            return;
        }

        view.Column(style: ["w-full gap-4"], content: view =>
        {
            view.Row(style: ["w-full gap-4"], content: view =>
            {
                for (int i = 0; i < Math.Min(2, _currentQuestion.Value.AnswerOptions.Length); i++)
                {
                    RenderHostAnswerBlock(view, i);
                }
            });

            view.Row(style: ["w-full gap-4"], content: view =>
            {
                for (int i = 2; i < Math.Min(4, _currentQuestion.Value.AnswerOptions.Length); i++)
                {
                    RenderHostAnswerBlock(view, i);
                }
            });
        });
    }

    private void RenderHostAnswerBlock(UIView view, int index)
    {
        if (_currentQuestion.Value == null)
        {
            return;
        }

        var option = _currentQuestion.Value.AnswerOptions[index];
        var color = AnswerColors[index];
        var iconName = AnswerIcons[index];
        var staggerDelay = index * 100;

        view.Row(style: [
            $"flex-1 {color} rounded-xl p-6 items-center gap-4 min-h-[80px]",
            "motion-[0:opacity-0_translate-y-6,100:opacity-100_translate-y-0]",
            $"motion-duration-400ms motion-delay-{staggerDelay}ms motion-ease-out motion-fill-both"
        ], content: view =>
        {
            view.Icon(["w-6 h-6 text-white/80"], name: iconName);
            view.Text(style: ["text-2xl font-bold text-white"], text: option);
        });
    }

    private void RenderHostFeedback(UIView view)
    {
        view.Column(style: ["flex-1 p-8 gap-6"], content: view =>
        {
            view.Row(style: ["justify-between items-center"], content: view =>
            {
                view.Text(style: ["text-lg text-gray-400"], text: $"Question {_questionNumber.Value} / {_totalQuestions.Value}");
            });

            if (_currentQuestion.Value != null)
            {
                view.Column(style: ["flex-1 items-center justify-center gap-8"], content: view =>
                {
                    view.Text(
                        style: ["text-3xl font-bold text-white text-center max-w-4xl"],
                        text: _currentQuestion.Value.Question
                    );

                    view.Row(style: ["w-full max-w-5xl gap-4"], content: view =>
                    {
                        view.Column(style: ["w-full gap-4"], content: view =>
                        {
                            view.Row(style: ["w-full gap-4"], content: view =>
                            {
                                for (int i = 0; i < Math.Min(2, _currentQuestion.Value.AnswerOptions.Length); i++)
                                {
                                    RenderHostFeedbackAnswerBlock(view, i);
                                }
                            });

                            view.Row(style: ["w-full gap-4"], content: view =>
                            {
                                for (int i = 2; i < Math.Min(4, _currentQuestion.Value.AnswerOptions.Length); i++)
                                {
                                    RenderHostFeedbackAnswerBlock(view, i);
                                }
                            });
                        });
                    });

                    if (!string.IsNullOrEmpty(_currentQuestion.Value.Explanation))
                    {
                        view.Text(
                            style: [
                                "text-xl text-gray-300 text-center max-w-2xl mt-4",
                                "motion-[0:opacity-0,100:opacity-100]",
                                "motion-duration-500ms motion-delay-300ms motion-fill-both"
                            ],
                            text: _currentQuestion.Value.Explanation
                        );
                    }
                });
            }
        });
    }

    private void RenderHostFeedbackAnswerBlock(UIView view, int index)
    {
        if (_currentQuestion.Value == null)
        {
            return;
        }

        var option = _currentQuestion.Value.AnswerOptions[index];
        var isCorrect = index == _currentQuestion.Value.CorrectIndex;
        var baseColor = AnswerColors[index];
        var iconName = AnswerIcons[index];
        var opacity = isCorrect ? "" : "opacity-40";

        view.Row(style: [
            $"flex-1 {baseColor} {opacity} rounded-xl p-6 items-center gap-4 min-h-[80px] relative"
        ], content: view =>
        {
            view.Icon(["w-6 h-6 text-white/80"], name: iconName);
            view.Text(style: ["text-2xl font-bold text-white"], text: option);

            if (isCorrect)
            {
                view.Icon(
                    style: ["absolute right-4 w-8 h-8 text-white"],
                    name: "check-circle"
                );
            }
        });
    }

    private void RenderHostLeaderboard(UIView view)
    {
        var sortedPlayers = _players.Value.OrderByDescending(p => p.Score).ToList();

        view.Column(style: ["flex-1 items-center gap-6 p-8 min-h-0"], content: view =>
        {
            view.Text(
                style: ["text-4xl font-bold text-white shrink-0"],
                text: "Leaderboard"
            );

            view.ScrollArea(rootStyle: ["flex-1 w-full min-h-0"], viewportStyle: ["w-full"], content: view =>
            {
                view.Column(style: ["w-full max-w-2xl gap-3 mx-auto"], content: view =>
                {
                    for (int i = 0; i < Math.Min(sortedPlayers.Count, 5); i++)
                    {
                        var player = sortedPlayers[i];
                        var isTop = i == 0;
                        var bgColor = i switch
                        {
                            0 => "bg-gradient-to-r from-yellow-500/30 to-yellow-600/10 border border-yellow-500/50",
                            1 => "bg-gradient-to-r from-gray-400/20 to-gray-500/10 border border-gray-400/40",
                            2 => "bg-gradient-to-r from-amber-700/20 to-amber-800/10 border border-amber-700/40",
                            _ => "bg-white/5 border border-white/10"
                        };
                        var textSize = isTop ? "text-2xl" : "text-xl";
                        var staggerDelay = i * 100;

                        view.Row(style: [
                            $"{bgColor} rounded-xl px-6 py-4 items-center",
                            "motion-[0:opacity-0_translate-x-[-20px],100:opacity-100_translate-x-0]",
                            $"motion-duration-400ms motion-delay-{staggerDelay}ms motion-ease-out motion-fill-both"
                        ], content: view =>
                        {
                            view.Text(style: [$"{textSize} font-bold text-white/60 w-12"], text: $"#{i + 1}");
                            view.Text(style: [$"{textSize} font-bold text-white flex-1"], text: player.Name);
                            view.Text(style: [$"{textSize} font-bold text-purple-400"], text: $"{player.Score}");
                        });
                    }
                });
            });
        });
    }

    private void RenderHostGameOver(UIView view)
    {
        var sortedPlayers = _players.Value.OrderByDescending(p => p.Score).ToList();

        view.Column(style: ["flex-1 items-center gap-8 p-8 min-h-0"], content: view =>
        {
            view.Text(
                style: [
                    "text-5xl font-black text-transparent bg-clip-text bg-gradient-to-b from-[#FBE457] via-[#E9C13B] to-[#D59E27] shrink-0",
                    "motion-[0:scale-[0.5]_opacity-0,60:scale-[1.1],100:scale-[1]_opacity-100]",
                    "motion-duration-600ms motion-ease-[cubic-bezier(0.34,1.56,0.64,1)] motion-fill-forwards"
                ],
                text: "Game Over!"
            );

            if (sortedPlayers.Count > 0)
            {
                view.Column(style: ["items-center gap-2 shrink-0"], content: view =>
                {
                    view.Icon(["w-16 h-16 text-yellow-400"], name: "trophy");
                    view.Text(style: ["text-3xl font-bold text-white"], text: sortedPlayers[0].Name);
                    view.Text(style: ["text-xl text-purple-400"], text: $"{sortedPlayers[0].Score} points");
                });
            }

            view.ScrollArea(rootStyle: ["flex-1 w-full min-h-0"], viewportStyle: ["w-full"], content: view =>
            {
                view.Column(style: ["w-full max-w-2xl gap-2 mx-auto"], content: view =>
                {
                    for (int i = 0; i < sortedPlayers.Count; i++)
                    {
                        var player = sortedPlayers[i];
                        var bgColor = i switch
                        {
                            0 => "bg-gradient-to-r from-yellow-500/30 to-yellow-600/10 border border-yellow-500/50",
                            1 => "bg-gradient-to-r from-gray-400/20 to-gray-500/10 border border-gray-400/40",
                            2 => "bg-gradient-to-r from-amber-700/20 to-amber-800/10 border border-amber-700/40",
                            _ => "bg-white/5 border border-white/10"
                        };

                        view.Row(style: [$"{bgColor} rounded-xl px-6 py-3 items-center"], content: view =>
                        {
                            view.Text(style: ["text-lg font-bold text-white/60 w-12"], text: $"#{i + 1}");
                            view.Text(style: ["text-lg font-bold text-white flex-1"], text: player.Name);
                            view.Text(style: ["text-sm text-gray-400 mr-4"], text: $"{player.CorrectCount}/{_totalQuestions.Value} correct");
                            view.Text(style: ["text-lg font-bold text-purple-400"], text: $"{player.Score}");
                        });
                    }
                });
            });

            view.Button(
                style: ["px-8 py-3 text-xl rounded-full bg-purple-600 hover:bg-purple-500 text-white font-bold shrink-0"],
                label: "Play Again",
                onClick: async () =>
                {
                    _gameStage.Value = GameStage.Lobby;
                }
            );
        });
    }

    // ==================== PLAYER VIEW ====================

    private void RenderPlayerView(UIView view)
    {
        view.Column(style: ["w-full h-screen bg-black text-white relative overflow-hidden", AuroraGradient], content: view =>
        {
            view.Column(style: ["absolute inset-0 z-10 h-full overflow-y-auto overflow-x-hidden"], content: view =>
            {
                if (!_hasStarted.Value)
                {
                    RenderPlayerReady(view);
                }
                else if (!_hasJoined.Value)
                {
                    RenderPlayerNameEntry(view);
                }
                else
                {
                    switch (_gameStage.Value)
                    {
                        case GameStage.Lobby:
                            RenderPlayerLobbyWaiting(view);
                            break;
                        case GameStage.CountdownToStart:
                            RenderPlayerCountdownToStart(view);
                            break;
                        case GameStage.Question:
                            RenderPlayerQuestion(view);
                            break;
                        case GameStage.Feedback:
                            RenderPlayerFeedback(view);
                            break;
                        case GameStage.Leaderboard:
                            RenderPlayerLeaderboardView(view);
                            break;
                        case GameStage.GameOver:
                            RenderPlayerGameOver(view);
                            break;
                    }
                }
            });
        });
    }

    private void RenderPlayerReady(UIView view)
    {
        view.Column(style: ["flex-1 items-center justify-center gap-4 px-8"], content: view =>
        {
            view.Text(
                style: [
                    "text-4xl font-bold text-purple-400",
                    "motion-[0:brightness-100,40:brightness-[1.8],100:brightness-100]",
                    "motion-duration-2000ms motion-stagger-150ms motion-per-letter-loop"
                ],
                text: "Ikon Kahoot"
            );

            view.Text(style: ["text-lg text-gray-400 mt-2 text-center"], text: "Ready to test your Ikon knowledge?");

            view.ActionButton(
                style: ["w-full max-w-sm py-4 rounded-full bg-purple-600 text-white text-lg font-bold border-none mt-4"],
                action: ActionKind.RequestFullscreen,
                label: "Let's go!",
                onActionComplete: async _ =>
                {
                    _hasStarted.Value = true;
                }
            );
        });
    }

    private void RenderPlayerNameEntry(UIView view)
    {
        view.Column(style: ["flex-1 items-center justify-center gap-4 px-8"], content: view =>
        {
            view.Text(style: ["text-2xl font-bold text-white"], text: "What's your name?");

            view.TextField(
                style: [Input.Default, "w-full max-w-sm text-lg py-3 bg-white/10 border-white/20 text-white placeholder:text-gray-500"],
                value: _playerName.Value,
                placeholder: "Enter your name...",
                props: new Dictionary<string, object?>
                {
                    ["autoFocus"] = true,
                    ["autoComplete"] = "off",
                    ["data-lpignore"] = "true",
                    ["data-1p-ignore"] = "true",
                    ["data-form-type"] = "other"
                },
                onValueChange: value =>
                {
                    _playerName.Value = value;
                    return Task.CompletedTask;
                },
                onSubmit: async _ =>
                {
                    if (!string.IsNullOrWhiteSpace(_playerName.Value) && _playerName.Value.Length >= 2)
                    {
                        await AddOrUpdatePlayerAsync(ReactiveScope.ClientId, _playerName.Value);
                        _hasJoined.Value = true;
                    }
                }
            );

            var nameValid = !string.IsNullOrWhiteSpace(_playerName.Value) && _playerName.Value.Length >= 2;
            view.Button(
                style: [$"w-full max-w-sm py-4 rounded-full bg-purple-600 text-white text-lg font-bold border-none {(nameValid ? "opacity-100" : "opacity-40")}"],
                label: "Join!",
                disabled: !nameValid,
                onClick: async () =>
                {
                    if (nameValid)
                    {
                        await AddOrUpdatePlayerAsync(ReactiveScope.ClientId, _playerName.Value);
                        _hasJoined.Value = true;
                    }
                }
            );
        });
    }

    private void RenderPlayerLobbyWaiting(UIView view)
    {
        view.Column(style: ["flex-1 items-center justify-center gap-4 px-8"], content: view =>
        {
            view.Icon(["w-16 h-16 text-purple-400 animate-pulse"], name: "loader");
            view.Text(style: ["text-2xl font-bold text-white"], text: "You're in!");
            view.Text(style: ["text-lg text-gray-400 text-center"], text: "Waiting for the host to start the game...");

            view.Text(style: ["text-sm text-gray-500 mt-4"], text: $"{_players.Value.Count} player{(_players.Value.Count != 1 ? "s" : "")} joined");
        });
    }

    private void RenderPlayerCountdownToStart(UIView view)
    {
        view.Column(style: ["flex-1 items-center justify-center"], content: view =>
        {
            view.Text(style: ["text-lg text-gray-400 mb-4"], text: "Get ready!");
            view.Text(
                style: [
                    "text-[120px] font-black text-white",
                    "motion-[0:scale-[3]_opacity-0,100:scale-[1]_opacity-100]",
                    "motion-duration-800ms motion-ease-[cubic-bezier(0.34,1.56,0.64,1)] motion-fill-forwards"
                ],
                text: _countdown.Value.ToString()
            );
        });
    }

    private void RenderPlayerQuestion(UIView view)
    {
        view.Column(style: ["flex-1 p-4 gap-4"], content: view =>
        {
            if (_currentQuestion.Value == null)
            {
                view.Text(style: ["text-lg text-gray-300 text-center"], text: "Loading question...");
                return;
            }

            view.Text(
                style: ["text-xs text-gray-400 text-center"],
                text: $"Question {_questionNumber.Value} / {_totalQuestions.Value}"
            );

            view.Text(
                style: [
                    "text-lg font-semibold text-white text-center",
                    "motion-[0:opacity-0_translate-y-2,100:opacity-100_translate-y-0]",
                    "motion-duration-400ms motion-ease-out motion-fill-both"
                ],
                text: _currentQuestion.Value.Question
            );

            RenderPlayerCountdownBar(view);

            RenderPlayerAnswerButtons(view, false);
        });
    }

    private void RenderPlayerCountdownBar(UIView view)
    {
        var durationMs = _countdownSeconds.Value * 1000;
        var offsetMs = _progressBarOffsetMs.Value;
        var delayStyle = offsetMs > 0 ? $"motion-delay-[-{offsetMs}ms]" : "";

        view.Row(style: ["w-full h-3 rounded-full border border-white/20 overflow-hidden relative"], content: view =>
        {
            view.Row(style: [
                "h-full w-full rounded-full",
                "motion-[0:translate-x-0_bg-[#7DD23A],50:translate-x-[-50%]_bg-[#FF7D00],80:-translate-x-[-80%]_bg-[#FF3241],100:-translate-x-[100%]_bg-[#FF3241]]",
                $"motion-duration-{durationMs}ms motion-ease-linear motion-fill-forwards {delayStyle}"
            ]);
        });
    }

    private void RenderPlayerAnswerButtons(UIView view, bool isFeedback)
    {
        if (_currentQuestion.Value == null)
        {
            return;
        }

        view.Column(style: ["flex-1 gap-3 justify-center"], content: view =>
        {
            for (int i = 0; i < _currentQuestion.Value.AnswerOptions.Length; i++)
            {
                var optionIndex = i;
                var option = _currentQuestion.Value.AnswerOptions[i];
                var isSelected = _selectedAnswer.Value == i;
                var isCorrect = i == _currentQuestion.Value.CorrectIndex;
                var didAnswer = _hasAnswered.Value;
                var baseColor = AnswerColors[i];
                var iconName = AnswerIcons[i];
                var staggerDelay = i * 80;

                string buttonOpacity;
                string borderStyle;
                string feedbackIcon = "";

                if (isFeedback)
                {
                    if (isCorrect)
                    {
                        buttonOpacity = "opacity-100";
                        borderStyle = "ring-4 ring-white";
                        feedbackIcon = "check";
                    }
                    else if (isSelected && !isCorrect)
                    {
                        buttonOpacity = "opacity-60";
                        borderStyle = "";
                        feedbackIcon = "x";
                    }
                    else
                    {
                        buttonOpacity = "opacity-30";
                        borderStyle = "";
                    }
                }
                else if (isSelected)
                {
                    buttonOpacity = "opacity-100";
                    borderStyle = "ring-4 ring-white";
                }
                else
                {
                    buttonOpacity = didAnswer ? "opacity-50" : "opacity-100";
                    borderStyle = "";
                }

                view.Row(style: [
                    "relative",
                    "motion-[0:opacity-0_translate-y-4,100:opacity-100_translate-y-0]",
                    $"motion-duration-300ms motion-delay-{staggerDelay}ms motion-ease-out motion-fill-both"
                ], content: view =>
                {
                    view.Button(
                        style: [$"w-full px-4 py-5 rounded-xl text-lg font-bold text-white border-none {baseColor} {buttonOpacity} {borderStyle}"],
                        disabled: didAnswer || isFeedback,
                        onClick: async () =>
                        {
                            _selectedAnswer.Value = optionIndex;
                            _hasAnswered.Value = true;
                            await SubmitAnswer(optionIndex);
                        },
                        content: v =>
                        {
                            v.Row(style: ["items-center gap-3 w-full"], content: v =>
                            {
                                v.Icon(["w-5 h-5 text-white/70"], name: iconName);
                                v.Text(style: ["text-left flex-1"], text: option);
                            });
                        }
                    );

                    if (!string.IsNullOrEmpty(feedbackIcon))
                    {
                        view.Icon(
                            style: ["absolute right-4 top-1/2 -translate-y-1/2 w-6 h-6 text-white"],
                            name: feedbackIcon
                        );
                    }
                });
            }
        });
    }

    private void RenderPlayerFeedback(UIView view)
    {
        view.Column(style: ["flex-1 p-4 gap-4"], content: view =>
        {
            if (_currentQuestion.Value == null)
            {
                return;
            }

            view.Text(
                style: ["text-xs text-gray-400 text-center"],
                text: $"Question {_questionNumber.Value} / {_totalQuestions.Value}"
            );

            var isCorrect = _selectedAnswer.Value == _currentQuestion.Value.CorrectIndex;
            var didAnswer = _hasAnswered.Value;

            string feedbackText;
            string feedbackColor;

            if (!didAnswer)
            {
                feedbackText = "Out of time!";
                feedbackColor = "text-gray-400";
            }
            else if (isCorrect)
            {
                feedbackText = "Correct!";
                feedbackColor = "text-green-400";
            }
            else
            {
                feedbackText = "Wrong!";
                feedbackColor = "text-red-400";
            }

            view.Text(style: [
                $"text-4xl font-black text-center {feedbackColor}",
                "motion-[0:scale-[0.5]_opacity-0,60:scale-[1.15],100:scale-[1]_opacity-100]",
                "motion-duration-400ms motion-ease-[cubic-bezier(0.34,1.56,0.64,1)] motion-fill-forwards"
            ], text: feedbackText);

            if (didAnswer && isCorrect)
            {
                var playerEntry = _playerAnswers.TryGetValue(ReactiveScope.ClientId, out var entry) ? entry : default;
                var elapsed = (playerEntry.Timestamp - _questionStartedAt).TotalSeconds;
                var totalTime = _countdownSeconds.Value;
                var timeRemaining = Math.Max(0, totalTime - elapsed);
                var speedFactor = timeRemaining / totalTime;
                var points = 500 + (int)Math.Floor(speedFactor * 500);

                view.Text(
                    style: ["text-2xl font-bold text-purple-400 text-center"],
                    text: $"+{points} points"
                );
            }

            RenderPlayerAnswerButtons(view, true);
        });
    }

    private void RenderPlayerLeaderboardView(UIView view)
    {
        view.Column(style: ["flex-1 p-4"], content: view =>
        {
            RenderPlayerScoreboard(view);

            view.Column(style: ["flex-1 items-center justify-center gap-2"], content: view =>
            {
                view.Text(style: ["text-lg font-semibold text-white text-center"], text: "Next question coming up...");
            });
        });
    }

    private void RenderPlayerGameOver(UIView view)
    {
        view.Column(style: ["flex-1 p-4 gap-4 min-h-0"], content: view =>
        {
            RenderPlayerScoreboard(view);

            var player = GetCurrentPlayer();

            view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: view =>
            {
                view.Column(style: ["items-center justify-center gap-4 py-4"], content: view =>
                {
                    if (player != null)
                    {
                        var sortedPlayers = _players.Value.OrderByDescending(p => p.Score).ToList();
                        var rank = sortedPlayers.FindIndex(p => p.ClientId == player.ClientId) + 1;

                        var scoreFeedback = rank switch
                        {
                            1 => "Champion!",
                            2 => "Almost there!",
                            3 => "On the podium!",
                            _ => "Well played!"
                        };

                        view.Text(
                            style: ["text-4xl font-bold text-center text-transparent bg-clip-text bg-gradient-to-b from-[#FBE457] via-[#E9C13B] to-[#D59E27]"],
                            text: scoreFeedback
                        );

                        view.Text(style: ["text-white text-center text-sm"], text: "You finished");
                        view.Text(style: ["text-3xl font-bold text-white text-center"], text: $"#{rank}");

                        view.Text(style: ["text-white text-center text-sm mt-2"], text: "Score");
                        view.Text(style: ["text-2xl font-bold text-purple-400 text-center"], text: $"{player.Score} points");
                        view.Text(style: ["text-sm text-gray-400 text-center"], text: $"{player.CorrectCount}/{_totalQuestions.Value} correct");
                    }

                    view.Button(
                        style: ["w-full max-w-sm py-3 rounded-full bg-purple-600 text-white text-lg font-bold border-none mt-4"],
                        label: "Play Again",
                        disabled: _gameStage.Value != GameStage.GameOver,
                        onClick: async () =>
                        {
                            _gameStage.Value = GameStage.Lobby;
                        }
                    );
                });
            });
        });
    }

    private void RenderPlayerScoreboard(UIView view)
    {
        var currentPlayer = GetCurrentPlayer();
        var sortedPlayers = _players.Value.OrderByDescending(p => p.Score).Take(3).ToList();

        if (currentPlayer != null && !sortedPlayers.Any(p => p.ClientId == currentPlayer.ClientId))
        {
            sortedPlayers.Add(currentPlayer);
        }

        if (sortedPlayers.Count == 0)
        {
            return;
        }

        view.Column(style: ["w-full bg-black/30 backdrop-blur-sm rounded-2xl overflow-hidden"], content: view =>
        {
            view.Row(style: ["w-full bg-black/40 justify-center items-center h-8"], content: view =>
            {
                view.Text(style: ["text-sm font-semibold text-white"], text: "Scoreboard");
            });

            view.Column(style: ["p-2 gap-0"], content: view =>
            {
                foreach (var (player, index) in sortedPlayers.Select((p, i) => (p, i)))
                {
                    var isMe = currentPlayer?.ClientId == player.ClientId;
                    var bgColor = isMe
                        ? "bg-gradient-to-r from-purple-500 to-purple-700"
                        : "bg-transparent";
                    var textColor = isMe ? "text-white font-bold" : "text-white";

                    if (index == 3)
                    {
                        view.Row(style: ["h-px bg-white/10 rounded-full mx-2 mb-1"]);
                    }

                    view.Row(style: [$"rounded-full {bgColor} h-[30px] items-center px-3"], content: view =>
                    {
                        view.Text(style: [$"text-sm w-6 {textColor}"], text: $"{index + 1}");
                        view.Text(style: [$"text-sm flex-1 {textColor}"], text: player.Name);
                        view.Text(style: [$"text-sm {textColor}"], text: $"{player.Score}");
                    });
                }
            });
        });
    }
}
