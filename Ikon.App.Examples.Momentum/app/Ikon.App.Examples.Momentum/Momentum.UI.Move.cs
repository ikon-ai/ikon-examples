public partial class MomentumApp
{
    private void RenderMove(UIView view)
    {
        _ = _frame.Value;

        PendingPublication? pending;
        TrackRecorder? recorder;
        RecordingState state;
        bool simulated;

        lock (_sessionLock)
        {
            pending = _pending;
            recorder = _recorder;
            state = _state;
            simulated = _simulated;
        }

        if (pending != null)
        {
            RenderPending(view, pending);
            return;
        }

        if (recorder is { HasFix: true } && state != RecordingState.Idle)
        {
            RenderLive(view, recorder.Snapshot(_liveHighlights, _coachCue, simulated, state));
            return;
        }

        // Recording, but nothing has arrived yet. Falling through to the idle screen here is what made
        // a started outing look like a button that did nothing: the rider saw the marketing hero and a
        // Start button again while their phone was quietly hunting for satellites.
        if (recorder != null && state != RecordingState.Idle)
        {
            RenderAcquiring(view, simulated);
            return;
        }

        RenderIdle(view, state);
    }

    #region Idle

    /// <summary>
    /// The landing screen: a name and a way to start, and nothing else.
    ///
    /// It used to open on a headline, a paragraph and three cards explaining the app. Someone about to
    /// go running is standing outside holding their phone — they are not reading, and every line
    /// between them and the button is a line in the way. What the app can do is worth saying once
    /// somewhere else; here it needs to be one tap.
    /// </summary>
    /// <summary>
    /// The landing screen. Deliberately almost empty: the only action lives in the tab bar, where it
    /// cannot scroll out of reach, so repeating it here would just be the same button twice.
    /// </summary>
    private void RenderIdle(UIView view, RecordingState state)
    {
        view.Column(["flex-1 items-center justify-center gap-3 py-20"], content: view =>
        {
            view.Icon("circle-dot", ["text-[#e62e7d] opacity-70"], size: IconSize.Lg);
            view.Text("Tap to start", ["font-mono text-[10px] uppercase tracking-[0.22em] text-muted-foreground"]);

            if (state == RecordingState.Idle && _coachCue.Length > 0)
            {
                view.Text(_coachCue, ["text-[12.5px] text-[#f2da00] text-center max-w-[42ch] pt-2"]);
            }
        });
    }

    #endregion

    /// <summary>
    /// The gap between pressing Start and the first fix. Outdoors a cold GPS takes a few seconds;
    /// indoors it can take a minute and may never come at all, so this screen says what is happening,
    /// counts, and offers a way out.
    /// </summary>
    private void RenderAcquiring(UIView view, bool simulated)
    {
        var kind = _plannedKind.Value;
        int waited = _sessionStartedAt == DateTime.MinValue ? 0 : (int)(DateTime.UtcNow - _sessionStartedAt).TotalSeconds;

        view.Column(["gap-7 pt-4"], content: view =>
        {
            KindChip(view, kind, Momentum.ProfileOf(kind).Label + (simulated ? " · simulated" : ""));

            view.Row(["items-center gap-3.5"], content: view =>
            {
                view.Spinner(["text-[#e62e7d]"], size: SpinnerSize.Sm);
                view.Heading(simulated ? "Starting the outing" : "Looking for satellites",
                    [Brand.Title, "text-[30px] md:text-[38px]"]);
            });

            view.Text(
                simulated ? "Spinning up the route" : "Seconds outside, longer indoors",
                ["text-[14px] text-muted-foreground"]);

            view.Row(["items-baseline gap-2"], content: view =>
            {
                view.Text($"{waited}", [Brand.Numeral, "text-[26px]"]);
                Kicker(view, "seconds waiting");
            });

            if (_coachCue.Length > 0)
            {
                view.Row([Brand.Panel, "items-start gap-3 p-3.5"], content: view =>
                {
                    view.Icon("triangle-alert", ["text-[#f2da00] shrink-0 mt-0.5"], size: IconSize.Sm);
                    view.Text(_coachCue, ["text-[13px] text-[#f7f7f7] leading-relaxed"]);
                });
            }
            else if (!simulated && waited > 25)
            {
                view.Row([Brand.Panel, "items-start gap-3 p-3.5"], content: view =>
                {
                    view.Icon("triangle-alert", ["text-[#f2da00] shrink-0 mt-0.5"], size: IconSize.Sm);
                    view.Text(
                        "Still nothing. Check that Momentum has Location set to Always in Settings, and step outside if you can.",
                        ["text-[13px] text-[#f7f7f7] leading-relaxed"]);
                });
            }

            view.Row(["gap-3 pt-1"], content: view =>
            {
                PillButton(view, "Cancel", FinishFromUiAsync, icon: "x");
            });
        });
    }

    #region Live

    private void RenderLive(UIView view, LiveFrame frame)
    {
        var profile = Momentum.ProfileOf(frame.Kind);
        bool heroIsSpeed = frame.Kind == ActivityKind.Car;

        view.Column(["gap-4"], content: view =>
        {
            view.Row(["items-start justify-between gap-4"], content: view =>
            {
                view.Column(["gap-2"], content: view =>
                {
                    KindChip(view, frame.Kind, Momentum.KindLabel(frame.Kind, frame.DistanceM, frame.MovingSeconds) + (frame.Simulated ? " · simulated" : ""));

                    view.Row(["items-baseline gap-2"], content: view =>
                    {
                        view.Text(
                            heroIsSpeed ? $"{frame.SpeedMps * 3.6:0}" : $"{frame.DistanceM / 1000:0.00}",
                            [Brand.Hero, "text-[64px] md:text-[104px]"]);
                        view.Text(heroIsSpeed ? "km/h" : "km", ["font-mono text-[12px] text-muted-foreground tracking-[0.16em]"]);
                    });
                });

                view.Column(["gap-2 items-end"], content: view =>
                {
                    view.Text(Momentum.FormatDuration(frame.MovingSeconds), [Brand.Numeral, "text-[30px] md:text-[42px]"]);
                    Kicker(view, "moving");

                    if (frame.ElapsedSeconds - frame.MovingSeconds > 5)
                    {
                        view.Text($"+{Momentum.FormatDuration(frame.ElapsedSeconds - frame.MovingSeconds)} stopped",
                            ["font-mono text-[10px] text-muted-foreground tracking-[0.08em]"]);
                    }
                });
            });

            if (frame.State == RecordingState.AutoPaused)
            {
                view.Row(["items-center gap-2 px-3 py-1.5 rounded-md bg-[#242427] ring-1 ring-white/[0.06] self-start"], content: view =>
                {
                    view.Icon("pause", ["text-[#f2da00]"], size: IconSize.Sm);
                    view.Text("Auto-paused", ["text-[12.5px] text-muted-foreground"]);
                });
            }

            view.Row(["items-center gap-3 pt-0.5"], content: view =>
            {
                view.Button(
                    ["inline-flex items-center gap-1.5 h-8 px-3 rounded-full bg-transparent border border-border text-muted-foreground text-[11px]"],
                    text: frame.State == RecordingState.Paused ? "Resume" : "Pause",
                    icon: frame.State == RecordingState.Paused ? "play" : "pause",
                    onClick: () => { TogglePause(); return Task.CompletedTask; });

                view.Text($"±{frame.AccuracyM:0} m", ["font-mono text-[10px] text-muted-foreground tracking-[0.1em]"]);
            });

            RenderLiveMap(view, frame);

            view.Grid(["grid-cols-2 md:grid-cols-4 gap-x-7"], content: view =>
            {
                Tile(view, heroIsSpeed ? "Distance" : "Right now",
                    heroIsSpeed ? $"{frame.DistanceM / 1000:0.00}" : Momentum.FormatRate(frame.Kind, frame.SpeedMps).Split(' ')[0],
                    heroIsSpeed ? "km" : profile.ShowsSpeed ? "km/h" : "/km");
                Tile(view, "Average", Momentum.FormatRate(frame.Kind, frame.AvgSpeedMps).Split(' ')[0], profile.ShowsSpeed ? "km/h" : "/km");
                Tile(view, "Climbed", $"{frame.AscentM:0}", "m");

                // Cadence only appears once the accelerometer has found a rhythm — a browser and a
                // pocketed phone that is not being carried both legitimately have none.
                double cadence = _cadence.PerMinute;

                if (cadence > 0)
                {
                    Tile(view, "Cadence", $"{cadence:0}", "/min");
                }
                else
                {
                    Tile(view, "Gradient", $"{frame.GradePct:0.0}", "%");
                }
            });

            if (frame.CoachCue.Length > 0)
            {
                view.Row(["items-start gap-2"], content: view =>
                {
                    view.Icon("volume-2", ["text-[#e62e7d] shrink-0 mt-[3px]"], size: IconSize.Sm);
                    view.Text(frame.CoachCue, ["text-[13px] text-muted-foreground leading-snug"]);
                });
            }

            if (frame.LiveHighlights.Count > 0)
            {
                view.Column(["gap-2.5"], content: view =>
                {
                    Kicker(view, $"Found · {frame.LiveHighlights.Count}");

                    foreach (var highlight in frame.LiveHighlights.Take(3))
                    {
                        HighlightCard(view, highlight);
                    }
                });
            }

        });
    }

    private void RenderLiveMap(UIView view, LiveFrame frame)
    {
        string[] frameStyle = ["h-[190px] md:h-[360px] w-full rounded-lg overflow-hidden ring-1 ring-white/[0.06] bg-[#161618]"];

        if (frame.Track.Count < 2)
        {
            view.Row([.. frameStyle, "items-center justify-center gap-2.5"], content: view =>
            {
                view.Spinner(["text-muted-foreground"], size: SpinnerSize.Sm);
                Kicker(view, "Waiting for a fix");
            });

            return;
        }

        var markers = new List<MapMarker>
        {
            new() { Id = "start", Lat = frame.Track[0].Lat, Lon = frame.Track[0].Lon, Type = "start" },
        };

        if (frame.Position is { } here)
        {
            markers.Add(new MapMarker { Id = "here", Lat = here.Lat, Lon = here.Lon, Type = "here" });
        }

        // A following map never fits itself to the track, so it needs a street-level zoom of its own —
        // the fitted default would leave a rider looking at the whole city.
        view.MomentumMap(frame.Track, markers, center: frame.Position, zoom: 15, follow: true, style: frameStyle);
    }

    #endregion

    #region The publish gate

    /// <summary>
    /// What the AI proposes, held until the rider agrees. Every number on this screen was measured; the
    /// title, the two sentences and the order of the reel are the model's, and all three are editable
    /// before anything reaches the feed.
    /// </summary>
    private void RenderPending(UIView view, PendingPublication pending)
    {
        var activity = pending.Activity;
        var kept = pending.Highlights.Where(h => !pending.Dropped.Contains(h.Id)).ToList();
        var track = pending.Points.Select(p => p.Point).ToList();

        view.Column(["gap-7"], content: view =>
        {
            view.Column(["gap-2"], content: view =>
            {
                Kicker(view, "Nothing is published yet");
                view.Heading("Here is what happened.", [Brand.Title, "text-[32px] md:text-[42px]"]);
            });

            view.Grid(["grid-cols-1 lg:grid-cols-[1.15fr_1fr] gap-7"], content: view =>
            {
                view.Column(["gap-5"], content: view =>
                {
                    view.Column(["gap-2"], content: view =>
                    {
                        Kicker(view, "Title");
                        view.TextField([Input.Default, "text-[15px]"],
                            value: _titleDraft.Value,
                            onValueChange: async value => _titleDraft.Value = value,
                            placeholder: activity.Title);
                    });

                    if (activity.Story.Length > 0)
                    {
                        view.Text(activity.Story, ["text-[14px] text-muted-foreground leading-relaxed"]);
                    }
                    else
                    {
                        view.Row(["items-center gap-2.5"], content: view =>
                        {
                            view.Spinner(["text-muted-foreground"], size: SpinnerSize.Sm);
                            Kicker(view, "Writing it up");
                        });
                    }

                    view.Grid(["grid-cols-2 md:grid-cols-4 gap-x-6"], content: view =>
                    {
                        Tile(view, "Distance", $"{activity.DistanceM / 1000:0.00}", "km");
                        Tile(view, "Moving", Momentum.FormatDuration(activity.MovingSeconds), "");
                        Tile(view, "Climbed", $"{activity.AscentM:0}", "m");
                        Tile(view, "Momentum", $"{MomentumScoreOf(kept):0}", "/ 100");
                    });

                    view.Box(["h-[220px] w-full rounded-lg overflow-hidden ring-1 ring-white/[0.06] bg-[#161618]"], content: view =>
                    {
                        view.RouteTrace(track, ["h-full w-full p-3"]);
                    });
                });

                view.Column(["gap-2.5"], content: view =>
                {
                    Kicker(view, $"The reel · {kept.Count} of {pending.Highlights.Count}");

                    foreach (var highlight in pending.Highlights)
                    {
                        bool dropped = pending.Dropped.Contains(highlight.Id);
                        HighlightCard(view, highlight, dropped, () =>
                        {
                            ToggleDropped(highlight.Id);
                            return Task.CompletedTask;
                        });
                    }
                });
            });

            view.Row(["gap-3 flex-wrap items-center border-t border-border pt-5"], content: view =>
            {
                PillButton(view, "Publish to my feed", PublishPendingAsync, accent: true, icon: "check");
                PillButton(view, "Discard", DiscardPendingAsync, icon: "trash-2");
                view.Text("Nothing has been saved yet.", ["text-[12px] text-muted-foreground"]);
            });
        });
    }

    private static double MomentumScoreOf(IReadOnlyList<Highlight> highlights) => MomentumScore(highlights);

    #endregion

    #region The start sheet

    /// <summary>
    /// A Parallax dialog rather than a hand-rolled overlay. A `fixed` box holding an `absolute`
    /// backdrop beside a flow child does not survive the Flutter renderer — the backdrop drew and the
    /// sheet itself came out with no size, so a phone got a dimmed screen and nothing to tap. Dialog
    /// owns the overlay, the dismissal and the stacking on both renderers.
    /// </summary>
    private void RenderStartSheet(UIView view)
    {
        if (!_startSheetOpen.Value)
        {
            return;
        }

        view.Dialog(
            open: true,
            onOpenChange: async isOpen =>
            {
                if (!isOpen)
                {
                    _startSheetOpen.Value = false;
                }
            },
            overlayStyle: ["fixed inset-0 bg-black/70"],
            contentStyle: ["default", "bg-card ring-1 ring-white/[0.08] w-full max-w-[520px] p-6"],
            content: view =>
            {
                view.Column(["gap-5 pt-1"], content: view =>
                {
                    view.Grid(["grid-cols-2 gap-2.5"], content: view =>
                    {
                        foreach (var kind in (ActivityKind[])[ActivityKind.Foot, ActivityKind.Bike, ActivityKind.Horse, ActivityKind.Car])
                        {
                            KindOption(view, kind);
                        }
                    });

                    view.Column(["gap-2"], content: view =>
                    {
                        // Tucked away and off by default. It is a development aid — replaying a canned
                        // route — and it sat in the middle of the one choice that matters, where the
                        // only thing it can do is get picked by accident.
                        view.Row(["items-center justify-end gap-2"], content: view =>
                        {
                            view.Text("Simulate", ["font-mono text-[9.5px] uppercase tracking-[0.14em] text-muted-foreground"]);
                            view.Switch(["default", "scale-[0.8]"],
                                value: _useSimulator.Value,
                                ariaLabel: "Simulate the outing with a canned route",
                                onValueChange: async value => _useSimulator.Value = value);
                        });

                        if (_useSimulator.Value)
                        {
                            // Bounded: turning the simulator on used to grow the sheet until Go fell
                            // off the bottom of the phone.
                            view.ScrollArea(["max-h-[132px]"], content: view =>
                            {
                                view.Column(["gap-1.5"], content: view =>
                                {
                                    foreach (var route in Routes.ForKind(_plannedKind.Value))
                                    {
                                        RouteOption(view, route);
                                    }
                                });
                            });
                        }
                    });

                    if (_useSimulator.Value)
                    {
                        view.Button([
                                "flex items-center justify-center gap-2 w-full h-14 rounded-full bg-[#e62e7d] text-white",
                                "font-heading font-semibold text-[16px] tracking-[-0.01em] active:scale-[0.98] transition-transform duration-150",
                            ],
                            text: "Go",
                            icon: "play",
                            onClick: StartPlannedAsync);
                    }
                });
            });
    }

    private void KindOption(UIView view, ActivityKind kind)
    {
        bool active = _plannedKind.Value == kind;
        var profile = Momentum.ProfileOf(kind);

        view.Button([
                "flex flex-col items-center justify-center gap-3 py-9 rounded-2xl ring-1 transition-colors duration-150",
                active ? "bg-[#242427] ring-[#db176e] text-[#f7f7f7]" : "bg-transparent ring-white/[0.06] text-muted-foreground hover:text-[#f7f7f7]"
            ],
            key: kind.ToString(),
            ariaLabel: profile.Label,
            onClick: async () =>
            {
                _plannedKind.Value = kind;
                _plannedRouteId.Value = Routes.DefaultFor(kind).Id;

                // One tap is the whole interaction. Choosing a kind and then confirming it asked for a
                // second decision that had already been made — and it is asked of someone standing
                // outside about to start, which is the worst moment to add a step.
                //
                // The simulator is the exception: it replays one of several canned routes, so there
                // genuinely is a second choice to make and the confirm stays.
                if (!_useSimulator.Value)
                {
                    await StartPlannedAsync();
                }
            },
            content: view =>
            {
                // The symbol carries it; the word is only there for anyone the symbol does not.
                view.Icon(profile.Icon, [active ? "text-[#e62e7d]" : ""], size: IconSize.Xl);
                view.Text(profile.Label, ["font-mono text-[11px] uppercase tracking-[0.14em]"]);
            });
    }

    private void RouteOption(UIView view, Route route)
    {
        bool active = _plannedRouteId.Value == route.Id;

        view.Box([
                "flex items-center justify-between gap-3 px-3.5 py-2.5 rounded-md ring-1 cursor-pointer transition-colors duration-150",
                active ? "bg-[#242427] ring-[#db176e]" : "bg-transparent ring-white/[0.06] hover:ring-white/[0.14]"
            ],
            key: route.Id,
            onClick: () => { _plannedRouteId.Value = route.Id; },
            ariaLabel: route.Name,
            content: view =>
            {
                view.Column(["gap-0.5 min-w-0"], content: view =>
                {
                    view.Text(route.Name, ["text-[13.5px] text-[#f7f7f7] truncate"]);
                    view.Text(route.Where, ["font-mono text-[10px] uppercase tracking-[0.14em] text-muted-foreground"]);
                });

                view.Text($"{route.TotalMeters / 1000:0.0} km", [Brand.Mono, "text-[12px] shrink-0"]);
            });
    }

    private async Task StartPlannedAsync()
    {
        var kind = _plannedKind.Value;
        string preset = kind switch
        {
            ActivityKind.Foot => "run",
            ActivityKind.Car => "spirited",
            _ => "",
        };

        await StartAsync(kind, _useSimulator.Value, _plannedRouteId.Value, preset);
    }

    #endregion
}
