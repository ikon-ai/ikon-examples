public partial class MomentumApp
{
    private static readonly (MomentumSection Section, string Label, string Icon)[] Tabs =
    [
        (MomentumSection.Move, "Move", "circle-dot"),
        (MomentumSection.Feed, "Feed", "layout-list"),
        (MomentumSection.You, "You", "user"),
    ];

    private void BuildUi()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["h-screen w-full bg-background text-foreground font-sans antialiased"], content: view =>
            {
                RenderHeader(view);

                view.ScrollArea(["flex-1 min-h-0"],
                    viewportStyle: ["px-5 py-6 md:px-10 md:py-9 max-w-[1180px] mx-auto w-full"],
                    content: view =>
                    {
                        switch (_section.Value)
                        {
                            case MomentumSection.Move:
                                RenderMove(view);
                                break;
                            case MomentumSection.Feed:
                                RenderFeed(view);
                                break;
                            case MomentumSection.Activity:
                                RenderActivity(view);
                                break;
                            case MomentumSection.Admin:
                                RenderAdmin(view);
                                break;
                            default:
                                RenderYou(view);
                                break;
                        }
                    });

                RenderTabBar(view);
            });

            RenderStartSheet(view);
        });
    }

    private void RenderHeader(UIView view)
    {
        view.Row(["items-center justify-between px-5 md:px-10 h-14 border-b border-border shrink-0 bg-background"], content: view =>
        {
            view.Row(["items-center gap-9"], content: view =>
            {
                view.Button(["bg-transparent p-0"], onClick: () => GoTo(MomentumSection.Move), ariaLabel: "Momentum home", content: view =>
                {
                    view.Text("MOMENTUM", ["font-heading font-semibold text-[13px] tracking-[0.28em] text-[#f7f7f7]"]);
                });

                // A native client is a phone: the rail does not fit beside the wordmark there, and
                // Flutter does not evaluate the `md:` breakpoint that hides it on a narrow browser, so
                // it has to be told outright. Phones navigate from the bar at the bottom instead.
                view.Row([_isNativeClient.Value ? "hidden" : "hidden md:flex", "items-center gap-6"], content: view =>
                {
                    foreach (var (section, label, _) in Tabs)
                    {
                        bool active = _section.Value == section || (section == MomentumSection.Feed && _section.Value == MomentumSection.Activity);
                        view.Button(
                            ["bg-transparent px-0 py-1 text-[13px] border-b transition-colors duration-150",
                             active ? "text-[#f7f7f7] border-[#db176e]" : "text-muted-foreground border-transparent hover:text-[#f7f7f7]"],
                            text: label,
                            onClick: () => GoTo(section));
                    }
                });
            });

            RenderLivePill(view);
        });
    }

    /// <summary>
    /// The recording indicator, in the chrome on every screen. It is the pixel-wave motif rather than a
    /// spinner, and it uses the reward yellow the brand reserves for a live signal.
    /// </summary>
    private void RenderLivePill(UIView view)
    {
        _ = _frame.Value;
        var state = State;

        if (state == RecordingState.Idle)
        {
            view.Text("READY", [Brand.Kicker]);
            return;
        }

        // Tappable, because Move is no longer a tab: this is how someone reading the feed mid-outing
        // gets back to their numbers.
        view.Box(["flex items-center gap-2.5 cursor-pointer"],
            ariaLabel: "Back to the outing",
            onClick: () => GoTo(MomentumSection.Move),
            content: view =>
        {

            bool live = state == RecordingState.Recording;

            view.Row(["items-center gap-[3px]"], content: view =>
            {
                for (int cell = 0; cell < 3; cell++)
                {
                    view.Box([
                        "size-[5px] rounded-[1px]",
                        live ? "bg-[#f2da00]" : "bg-[#6e6e74]",
                        live
                            ? $"motion-[0:opacity-16,50:opacity-100,100:opacity-16] motion-duration-1300ms motion-delay-{cell * 60}ms motion-loop motion-ease-ease-in-out"
                            : ""
                    ], key: $"live-cell-{cell}");
                }
            });

            view.Text(
                state switch
                {
                    RecordingState.Recording => "RECORDING",
                    RecordingState.AutoPaused => "AUTO-PAUSED",
                    _ => "PAUSED",
                },
                ["font-mono text-[10px] uppercase tracking-[0.22em]", live ? "text-[#f2da00]" : "text-muted-foreground"]);
        });
    }

    /// <summary>
    /// The tab bar, with the one action that matters in the middle of it.
    ///
    /// Go and Stop share the slot deliberately. It is the only control anyone needs while moving, it
    /// is where a thumb already is, and putting it here means it can never end up below the fold —
    /// which is exactly what happened when it lived in the page and the page was a scroll.
    /// </summary>
    private void RenderTabBar(UIView view)
    {
        ActionState action;

        lock (_sessionLock)
        {
            action = _state == RecordingState.Idle
                ? ActionState.Idle
                // Stop belongs on the screen showing what would be stopped. Somewhere else in the app
                // the same button is the way back to the outing — which is both the thing a rider
                // actually wants from there, and one fewer way to end a ride by accident.
                : _section.Value != MomentumSection.Move
                    ? ActionState.Return
                    : _state is RecordingState.Paused or RecordingState.AutoPaused
                        ? ActionState.Held
                        // Started, but nothing has arrived yet. Outdoors that is a few seconds;
                        // indoors it can be a minute and may never come.
                        : _recorder is { HasFix: true } ? ActionState.Recording : ActionState.Acquiring;
        }

        view.Row([
                _isNativeClient.Value ? "flex" : "md:hidden",
                "border-t border-border bg-background shrink-0 items-center justify-around px-2 pt-2 pb-[max(0.5rem,env(safe-area-inset-bottom))]"
            ], content: view =>
        {
            // Feed, action, You — one either side, so the action is genuinely in the middle rather
            // than second of four. Move is not a tab: the action is what takes you there, and while
            // an outing is running the status in the header does.
            Tab(view, Tabs[1]);
            PrimaryAction(view, action);
            Tab(view, Tabs[2]);
        });
    }

    private void Tab(UIView view, (MomentumSection Section, string Label, string Icon) tab)
    {
        bool active = _section.Value == tab.Section
            || (tab.Section == MomentumSection.Feed && _section.Value == MomentumSection.Activity);

        view.Button(
            ["bg-transparent flex flex-col items-center gap-1 px-3 py-1", active ? "text-[#f7f7f7]" : "text-muted-foreground"],
            onClick: () => GoTo(tab.Section),
            ariaLabel: tab.Label,
            content: view =>
            {
                view.Icon(tab.Icon, [active ? "text-[#e62e7d]" : "text-muted-foreground"], size: IconSize.Sm);
                view.Text(tab.Label, ["font-mono text-[9px] uppercase tracking-[0.18em]"]);
            });
    }

    /// <summary>What the one button is currently for.</summary>
    private enum ActionState
    {
        Idle,

        /// <summary>Started, but no fix has landed yet — so nothing is being recorded.</summary>
        Acquiring,

        Recording,

        /// <summary>Paused by the rider, or by itself.</summary>
        Held,

        /// <summary>Recording, but the rider is looking at another tab.</summary>
        Return,
    }

    /// <summary>
    /// The one control that matters, and the readout for whether the outing is actually running.
    ///
    /// It used to be white for everything that was not idle, which made the most important question —
    /// did tracking start? — the one thing it could not answer. Pressing Go and seeing a solid stop
    /// button says the same whether fixes are pouring in or the phone has been indoors failing to see
    /// a satellite for a minute.
    /// </summary>
    private void PrimaryAction(UIView view, ActionState action)
    {
        (string fill, string ink, string icon) = action switch
        {
            ActionState.Idle => ("bg-[#e62e7d]", "text-white", "play"),
            ActionState.Acquiring => ("bg-transparent ring-2 ring-[#f2da00] motion-[0:opacity-45,50:opacity-100,100:opacity-45] motion-duration-1200ms motion-loop motion-ease-ease-in-out", "text-[#f2da00]", "satellite-dish"),
            ActionState.Held => ("bg-[#f2da00]", "text-[#0b0b0d]", "square"),
            ActionState.Return => ("bg-[#e62e7d]", "text-white", "circle-dot"),
            _ => ("bg-[#f7f7f7]", "text-[#0b0b0d]", "square"),
        };

        view.Button([
                "flex items-center justify-center w-[68px] h-[68px] -mt-6 rounded-full shadow-lg border-4 border-background",
                "transition-transform duration-150 active:scale-[0.94]",
                fill,
            ],
            ariaLabel: action switch
            {
                ActionState.Idle => "Start an outing",
                ActionState.Acquiring => "Waiting for a fix — tap to stop",
                ActionState.Held => "Paused — tap to stop",
                ActionState.Return => "Back to the outing",
                _ => "Stop recording",
            },
            onClick: async () =>
            {
                if (action == ActionState.Idle)
                {
                    _section.Value = MomentumSection.Move;
                    _startSheetOpen.Value = true;
                }
                else if (action == ActionState.Return)
                {
                    GoTo(MomentumSection.Move);
                }
                else
                {
                    await FinishFromUiAsync();
                }
            },
            content: view =>
            {
                view.Icon(icon, [ink], size: IconSize.Lg);
            });
    }

    private void GoTo(MomentumSection section)
    {
        _openActivityId.Value = null;
        _focusedHighlightId.Value = null;
        _section.Value = section;
    }

    #region Shared pieces

    private static void Kicker(UIView view, string text, string? extra = null)
    {
        view.Text(text, [Brand.Kicker, extra ?? ""]);
    }

    private static void SectionTitle(UIView view, string kicker, string title)
    {
        view.Column(["gap-1.5 mb-7"], content: view =>
        {
            Kicker(view, kicker);
            view.Heading(title, [Brand.Title, "text-[34px] md:text-[46px]"]);
        });
    }

    /// <summary>A metric readout: wide tracked label, tight heavy number, small unit.</summary>
    private static void Tile(UIView view, string label, string value, string unit, string? extra = null)
    {
        view.Column(["gap-1.5 py-3.5 border-t border-border", extra ?? ""], content: view =>
        {
            Kicker(view, label);
            view.Row(["items-baseline gap-1.5"], content: view =>
            {
                view.Text(value, [Brand.Numeral, "text-[28px] md:text-[34px]"]);

                if (unit.Length > 0)
                {
                    view.Text(unit, ["font-mono text-[10px] text-muted-foreground tracking-[0.1em]"]);
                }
            });
        });
    }

    private static void PillButton(UIView view, string text, Func<Task> onClick, bool accent = false, string? icon = null, bool disabled = false)
    {
        view.Button(
            ["inline-flex items-center gap-2 h-11 px-6 rounded-full font-medium text-[13px] transition-colors duration-150 border active:scale-[.98]",
             accent
                ? "bg-[#db176e] text-white border-transparent hover:bg-[#f5277f]"
                : "bg-transparent text-[#f7f7f7] border-border hover:border-[#db176e]",
             disabled ? "opacity-40" : ""],
            text: text,
            icon: icon,
            disabled: disabled,
            onClick: onClick);
    }

    /// <summary>
    /// One highlight card. The medal colour is the whole visual payload — gold is the reward yellow the
    /// brand reserves, and everything else on the card stays monochrome so it reads.
    /// </summary>
    private void HighlightCard(UIView view, Highlight highlight, bool dropped = false, Func<Task>? onToggle = null, bool selectable = false)
    {
        string medal = Brand.MedalHex(highlight.Tier);
        bool focused = _focusedHighlightId.Value == highlight.Id;

        view.Box([
                "flex items-start gap-3.5 p-3.5 rounded-lg ring-1 transition-colors duration-150",
                dropped ? "bg-transparent ring-white/[0.04] opacity-45" : "bg-card ring-white/[0.06]",
                focused ? "ring-[#db176e]" : "",
                selectable ? "cursor-pointer hover:ring-white/[0.14]" : ""
            ],
            key: highlight.Id,
            onClick: selectable ? () => { _focusedHighlightId.Value = focused ? null : highlight.Id; } : null,
            ariaLabel: selectable ? $"Focus {highlight.Title}" : null,
            content: view =>
            {
                view.Box([$"shrink-0 size-9 rounded-md grid place-items-center bg-[{medal}]/10 ring-1 ring-[{medal}]/30"], content: view =>
                {
                    view.Icon(highlight.Icon, [$"text-[{medal}]"], size: IconSize.Sm);
                });

                view.Column(["gap-1 min-w-0 flex-1"], content: view =>
                {
                    view.Row(["items-center gap-2 flex-wrap"], content: view =>
                    {
                        view.Text(highlight.Title, ["font-heading font-semibold text-[14px] text-[#f7f7f7] tracking-[-0.01em]"]);

                        if (highlight.Tier != MedalTier.None)
                        {
                            view.Text(Brand.MedalLabel(highlight.Tier).ToUpperInvariant(),
                                [$"font-mono text-[9px] uppercase tracking-[0.18em] text-[{medal}]"]);
                        }
                    });

                    view.Text(highlight.Detail, ["text-[12.5px] text-muted-foreground leading-relaxed"]);
                });

                if (onToggle != null)
                {
                    view.Button(
                        ["bg-transparent shrink-0 p-1.5 rounded-md text-muted-foreground hover:text-[#f7f7f7]"],
                        onClick: onToggle,
                        ariaLabel: dropped ? $"Put {highlight.Title} back in the reel" : $"Drop {highlight.Title} from the reel",
                        content: v => v.Icon(dropped ? "plus" : "x", size: IconSize.Sm));
                }
            });
    }

    private static void KindChip(UIView view, ActivityKind kind, string label)
    {
        view.Row(["items-center gap-1.5"], content: view =>
        {
            view.Icon(Momentum.ProfileOf(kind).Icon, ["text-[#e62e7d]"], size: IconSize.Sm);
            view.Text(label.ToUpperInvariant(), [Brand.Kicker]);
        });
    }

    private static string LocalWhen(DateTime utc) => utc.ToLocalTime().ToString("ddd d MMM · HH:mm");

    #endregion
}
