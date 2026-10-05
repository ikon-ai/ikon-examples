namespace Ikon.App.Patterns.Examples;

// Generated holder for parallax-library-guide.md — one class per guide SECTION, because a section is one
// reader's file: two of them may each declare a `UI` or a `Main` without either being wrong.
// Each class carries only the placeholder names its own fences use and do not declare.

// The records the guide invents for its examples.
file sealed record TodoItem(string Text = "", bool Done = false, int Priority = 0);

file sealed record ExistingPreset(string Name = "", bool IsPublic = false);

file sealed record Listing(string Slug, string Id);

file sealed class ListingStore
{
    public Task<IReadOnlyList<Listing>> GetListingsAsync() => Task.FromResult<IReadOnlyList<Listing>>([]);

    public Task<IReadOnlyList<Listing>> GetPublishedArticlesAsync() => Task.FromResult<IReadOnlyList<Listing>>([]);
}

file sealed class PxReactiveUiUpdatesExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:px-reactive-ui-updates
    private readonly Reactive<int> _count = new(0);
    private readonly Reactive<string> _message = new("Hello");

    // When _count.Value changes, only UI that reads _count.Value re-renders
    // When _message.Value changes, only UI that reads _message.Value re-renders
    #endregion

}

file sealed class PxThemedComponentsAndCrosswindStylingExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task SaveAsync() => Task.CompletedTask;

    public async Task PxThemedComponentsAndCrosswindStyling(UIView view)
    {
        #region example:px-themed-components-and-crosswind-styling
        view.Button(text: "Save", onClick: SaveAsync);                  // fully themed as-is
        view.Button([Button.PrimaryMd, "w-full"], text: "Save", onClick: SaveAsync);
        view.Button(["default", "w-full"], text: "Save", onClick: SaveAsync);   // same: Button's default IS PrimaryMd
        #endregion
    }
}

file sealed class PxSettingUpAUi2Examples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:px-setting-up-a-ui-2
    private UI UI { get; } = new(app, new IkonTheme
    {
        ["primary"] = "amber-400",
        ["background"] = "zinc-950",
    });
    #endregion
}

file sealed class PxSettingUpAUiExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:px-setting-up-a-ui
    private UI UI { get; } = new(app, new IkonTheme());

    private readonly Reactive<int> _counter = new(0);

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["items-center gap-4 p-6"], content: view =>
            {
                view.Heading("Counter App", style: [Text.H2]);
                view.Text([Text.Body], text: $"Count: {_counter.Value}");
                view.Button([Button.PrimaryMd], text: "Increment",
                    onClick: async () => _counter.Value++);
            });
        });
    }
    #endregion

}

file sealed class PxLightDarkSwitchingWithUsethemeExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region example:px-light-dark-switching-with-usetheme
    private ThemeControl _theme = null!;

    public async Task Main()
    {
        _theme = UI.UseTheme();   // call once, before clients join

        UI.Root([Page.Default], content: view =>
        {
            view.Button(
                icon: _theme.Current.Value == Theme.Dark ? "sun" : "moon",
                text: "Toggle theme",
                onClick: _theme.ToggleAsync);
        });
    }
    #endregion

}

file sealed class PxWhenAnActionHandlerThrowsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region example:px-when-an-action-handler-throws
    private readonly ClientReactive<string?> _actionFailed = new(null);

    public async Task Main()
    {
        // Runs inside the failed handler's client scope, so the ClientReactive write lands on the
        // person who clicked. Show your own line — the exception's text is for the log.
        UI.ActionFailedAsync += args =>
        {
            _actionFailed.Value = "That change could not be saved — please try again.";
            return Task.CompletedTask;
        };

        UI.Root([Page.Default], content: view =>
        {
            view.Toast(open: _actionFailed.Value != null,
                onOpenChange: async open => _actionFailed.Value = open ? _actionFailed.Value : null,
                title: "Something went wrong", description: _actionFailed.Value ?? "");
        });
    }
    #endregion
}

file sealed class PxSharedPerClientPerUserPerMount2Examples(IApp<SessionIdentity, ClientParameters> app)
{
    private static IEnumerable<string> LoadCart(string userId) => [];

    #region example:px-shared-per-client-per-user-per-mount-2
    private readonly ClientReactive<string> _welcome =
        ClientReactive.Create(sessionId => $"Welcome, session {sessionId}!");

    private readonly UserReactiveList<string> _cart =
        new(userId => LoadCart(userId));
    #endregion
}

file sealed class PxSharedPerClientPerUserPerMountExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static IEnumerable<string> LoadCart(string userId) => [];

    #region example:px-shared-per-client-per-user-per-mount
    private readonly Reactive<int> _sharedCounter = new(0);
    private readonly ClientReactive<string> _draft = new("");
    private readonly UserReactive<string> _language = new("en");
    #endregion

}

file sealed class PxReactiveCollectionsReactivelistAndReactivedictioExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly TodoItem item = new();
    private static void Render(TodoItem todo) { }

    #region example:px-reactive-collections-reactivelist-and-reactivedictionary
    private readonly ReactiveList<TodoItem> _todos = new();
    private readonly ReactiveDictionary<string, int> _scores = new();
    #endregion

    public async Task PxReactiveCollectionsReactivelistAndReactivedictionary2(UIView view)
    {
        #region example:px-reactive-collections-reactivelist-and-reactivedictionary-2
        _todos.Add(item);                    // also: AddRange, Insert, Remove, RemoveAt,
        _todos.RemoveAll(t => t.Done);       // RemoveAll, Clear, ReplaceAll, Sort
        _todos.Update(list => list.OrderBy(t => t.Priority));  // whole-list transform, one notification

        _scores["anna"] = 10;                // add-or-replace, one notification
        _scores.Update(map => map["anna"]++); // atomic read-modify-write under the lock

        foreach (var todo in _todos) { Render(todo); } // enumerate the reactive directly
        #endregion
    }
}

file sealed class PxBackgroundWorkTheForMethodsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ClientReactive<string> _draft = new("");
    private readonly ClientReactiveList<TodoItem> _items = new();
    private readonly TodoItem item = new();
    private static Task<string> LoadDraftAsync() => Task.FromResult("");

    public async Task PxBackgroundWorkTheForMethods(UIView view)
    {
        #region example:px-background-work-the-for-methods
        var clientSessionId = ReactiveScope.ClientId;   // capture inside the callback

        _ = Task.Run(async () =>
        {
            var draft = await LoadDraftAsync();
            _draft.SetFor(clientSessionId, draft);      // scalar: SetFor / ValueFor / UpdateFor
            _items.AddFor(clientSessionId, item);       // list: AddFor / RemoveFor / ClearFor / UpdateFor
        });
        #endregion
    }
}

file sealed class PxTheBusyStatusPatternExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<IReadOnlyList<string>> _entries = new([]);
    private static Task<IReadOnlyList<string>> LoadEntriesAsync() => Task.FromResult<IReadOnlyList<string>>([]);

    #region example:px-the-busy-status-pattern
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _status = new(null);

    private async Task RefreshAsync()
    {
        await _busy.RunAsync(_status, async () =>
        {
            _entries.Value = await LoadEntriesAsync();
        });
    }
    #endregion

}

file sealed class PxTwoWayBindingExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly UserReactive<bool> _subscribed = new(false);
    private readonly ClientReactive<string> _name = new("");

    public async Task PxTwoWayBinding(UIView view)
    {
        #region example:px-two-way-binding
        view.TextField(["flex-1"], label: "Name", bind: _name);
        view.Switch(bind: _subscribed, label: "Subscribe to newsletter");
        #endregion
    }
}

file sealed class PxAppChromeAndSemantictoneExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Exception ex = new("example");
    private UI UI { get; } = new(app, new IkonTheme());

    #region example:px-app-chrome-and-semantictone-2
    private readonly Toasts _toasts = new();
    #endregion

    public async Task PxAppChromeAndSemantictone(UIView view)
    {
        #region example:px-app-chrome-and-semantictone
        view.Badge("Live", SemanticTone.Success);
        view.Alert("Import failed", SemanticTone.Error, description: "The file is not valid CSV");
        view.StatCard("Revenue", "$12,400", delta: "+8%", trend: StatTrend.Up, icon: "trending-up",
            iconTone: SemanticTone.Success);
        #endregion
    }

    public async Task PxAppChromeAndSemantictone3(UIView view)
    {
        #region example:px-app-chrome-and-semantictone-3
        // In UI.Root, mount exactly once:
        view.ToastHost(_toasts);

        // From any handler:
        _toasts.Success("Saved");
        _toasts.Error("Upload failed", ex.Message);
        #endregion
    }
}

file sealed class PxAiDisclosureExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    public async Task PxAiDisclosure(UIView view)
    {
        #region example:px-ai-disclosure
        // Under a chat composer
        view.AiDisclosure(AiDisclosureKind.Interaction);

        // Above a page of generated copy, with the accuracy caveat that is not itself a disclosure
        view.AiDisclosure(AiDisclosureKind.GeneratedContent, AiDisclosureVariant.Banner,
            note: "AI can make mistakes. Check anything important.");

        // Over a generated image
        view.AiDisclosure(AiDisclosureKind.GeneratedContent, AiDisclosureVariant.Pill,
            style: ["default", "absolute bottom-2 end-2"]);
        #endregion
    }
}

file sealed class PxFormsAndDialogsWithFormstateExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ExistingPreset existing = new();
    private static Task SavePresetAsync(PresetDraft draft) => Task.CompletedTask;

    #region example:px-forms-and-dialogs-with-formstate
    private sealed record PresetDraft(string Name = "", bool Public = false);
    private readonly FormState<PresetDraft> _preset = new(() => new PresetDraft());
    #endregion

    public async Task PxFormsAndDialogsWithFormstate2(UIView view)
    {
        #region example:px-forms-and-dialogs-with-formstate-2
        // Open on a fresh draft, or on a copy of the record being edited:
        _preset.Show();
        _preset.Show(new PresetDraft(existing.Name, existing.IsPublic));

        // In the UI:
        view.FormDialog(_preset, title: "New preset", content: form =>
        {
            form.FormField(_preset, "Name", content: f =>
                f.TextField(value: _preset.Draft.Name,
                    onValueChange: v => { _preset.Edit(d => d with { Name = v }); return Task.CompletedTask; }));
            form.FormError(_preset);
            form.FormSubmit(_preset, "Save", SavePresetAsync,
                validate: d => string.IsNullOrWhiteSpace(d.Name) ? [new FormFieldError("Name", "Required")] : []);
        });
        #endregion
    }
}

file sealed class PxStylingWithCrosswindExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task SubmitAsync() => Task.CompletedTask;

    public async Task PxStylingWithCrosswind(UIView view)
    {
        #region example:px-styling-with-crosswind
        view.Button([Button.PrimaryMd, "mt-4 self-center"], text: "Submit", onClick: SubmitAsync);
        view.Box(["bg-card border border-secondary p-6 rounded-2xl"], content: v => { });
        view.Text([Text.Caption], text: "Updated just now");
        #endregion
    }
}

file sealed class PxMergeSemanticsTheDefaultMarkerExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ClientReactive<string> _name = new("");

    public async Task PxMergeSemanticsTheDefaultMarker(UIView view)
    {
        #region example:px-merge-semantics-the-default-marker
        view.TextField(bind: _name);                        // fully themed input
        view.TextField(["default", "w-full"], bind: _name); // themed input, full width  ← what you usually want
        view.TextField(["w-full"], bind: _name);            // an unstyled box that is full width
        #endregion
    }

    public async Task PxMergeSemanticsTheDefaultMarker2(UIView view)
    {
        #region example:px-merge-semantics-the-default-marker-2
        view.DatePicker();                                        // fully themed trigger + popover + calendar
        view.DatePicker(triggerStyle: ["default", "w-full"]);     // themed trigger, full width; popover untouched
        view.DatePicker(triggerStyle: ["w-full"]);                // an unstyled trigger; popover still themed
        #endregion
    }

    public async Task PxMergeSemanticsTheDefaultMarker3(UIView view)
    {
        #region example:px-merge-semantics-the-default-marker-3
        view.Column(["gap-4"]);                    // flex flex-col gap-4 — the flex base is not droppable
        view.ScrollArea(viewportStyle: ["px-8"]);  // h-full w-full px-8 — the viewport still fills, and scrolls
        #endregion
        PatternDemoNote.RenderCaption(view, "An empty gap-4 column and an empty scroll area above: each keeps its base classes (flex flex-col, h-full w-full) under the classes the call adds");
    }
}

file sealed class PxDefaultStylingAndAutoComposedIndicatorsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<bool> _done = new(false);
    private readonly Reactive<bool> _on = new(false);
    private readonly Reactive<string> _text = new("");

    public async Task PxDefaultStylingAndAutoComposedIndicators(UIView view)
    {
        #region example:px-default-styling-and-auto-composed-indicators
        view.Checkbox(bind: _done);
        view.Switch(bind: _on);
        view.TextField(bind: _text);
        view.Button(text: "Submit", onClick: async () => { });
        #endregion
    }
}

file sealed class PxIconButtonsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task RefreshAsync() => Task.CompletedTask;

    public async Task PxIconButtons(UIView view)
    {
        #region example:px-icon-buttons
        view.Button([Button.GhostMd, Button.IconSm],   // h-8 w-8 p-0 min-h-0 — last wins
            icon: "refresh-cw",
            tooltip: "Refresh",
            onClick: RefreshAsync);
        #endregion
    }
}

file sealed class PxTooltipsAndNamingControlsExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task DeleteAsync() => Task.CompletedTask;

    public async Task PxTooltipsAndNamingControls(UIView view)
    {
        #region example:px-tooltips-and-naming-controls
        view.Button([Button.GhostMd, Button.Icon],
            icon: "trash-2",
            tooltip: "Delete",    // the hover bubble, and the aria-label since there is no text:
            onClick: DeleteAsync);
        #endregion
    }
}

file sealed class PxScrollareaAndAutoScrollExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ReactiveList<string> _messages = new(
    [
        "Maya: Are we still on for the design review at 3?",
        "Leo: Yes, I moved it to the small meeting room.",
        "Maya: Great. I'll bring the updated onboarding flow.",
        "Leo: Could you include the empty states too? Those were the open question.",
        "Maya: Already in there, plus the error copy from support.",
    ]);
    private readonly UIView anchor = null!;
    private readonly int version = 1;

    public async Task PxScrollareaAndAutoScroll(UIView view)
    {
        #region example:px-scrollarea-and-auto-scroll
        view.ScrollArea(
            rootStyle: ["h-[400px]"],
            autoScroll: true,
            autoScrollKey: _messages,
            content: view =>
            {
                foreach (var msg in _messages)
                {
                    view.Text([Text.Body], text: msg);
                }
            });
        #endregion
    }

    public async Task PxScrollareaAndAutoScroll2(UIView view)
    {
        #region example:px-scrollarea-and-auto-scroll-2
        anchor.FocusHint(new FocusHintProps { Priority = FocusPriority.Assertive },
            key: $"scroll-{version}");
        #endregion
        PatternDemoNote.RenderCaption(view, "An invisible focus hint is mounted here: each new key makes the client scroll it into view and announce it assertively");
    }
}

file sealed class PxScrollingInsideAFlexParentExamples(IApp<SessionIdentity, ClientParameters> app)
{

    public async Task PxScrollingInsideAFlexParent(UIView view)
    {
        #region example:px-scrolling-inside-a-flex-parent
        view.Column(["h-[82vh] flex flex-col"], content: dialog =>
        {
            dialog.Row(["items-center px-5 py-4 border-b"], content: header => { });

            dialog.ScrollArea(
                rootStyle: ["flex-1"],              // min-h-0 is injected automatically
                scrollbars: ScrollAreaScrollbars.Vertical,
                content: body => { });

            dialog.Row(["items-center px-3 py-2 border-t"], content: composer => { });
        });
        #endregion
        PatternDemoNote.RenderCaption(view, "An empty dialog-shaped column above: a bordered header row, a scroll area that takes the remaining height, and a bordered composer row");
    }

    public async Task PxScrollingInsideAFlexParent2(UIView view)
    {
        #region example:px-scrolling-inside-a-flex-parent-2
        view.ScrollColumn(
            style: ["h-[82vh] w-full sm:max-w-[560px] rounded-2xl bg-card"],
            header: h => h.Row(["px-5 py-4 border-b"], content: title => { }),
            footer: f => f.Row(["p-3 border-t"], content: composer => { }),
            content: body => body.Column(["gap-3"], content: messages => { }));
        #endregion
        PatternDemoNote.RenderCaption(view, "The same shape built with ScrollColumn: a fixed header and footer around a body that scrolls, all left empty here");
    }

    public async Task PxScrollingInsideAFlexParent3(UIView view)
    {
        #region example:px-scrolling-inside-a-flex-parent-3
        view.Column(["flex-1 min-h-0 overflow-y-auto", Scrollbar.Thin], content: rows => { });
        #endregion
        PatternDemoNote.RenderCaption(view, "An empty column above that scrolls vertically with a thin scrollbar once rows fill it");
    }

    public async Task PxScrollingInsideAFlexParent4(UIView view)
    {
        #region example:px-scrolling-inside-a-flex-parent-4
        view.Row(["overflow-x-auto gap-2", Scrollbar.Thin], content: chips => { });
        #endregion
        PatternDemoNote.RenderCaption(view, "An empty row above that scrolls sideways with a thin scrollbar once chips overflow it");
    }
}

file sealed class PxPanzoomViewingSomethingLargerThanTheScreenExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<double> _scale = new(1);

    public async Task PxPanzoomViewingSomethingLargerThanTheScreen(UIView view)
    {
        #region example:px-panzoom-viewing-something-larger-than-the-screen
        view.PanZoom(
            ["h-96 w-full rounded-lg border border-secondary bg-secondary"],
            scale: _scale.Value,
            minScale: 0.25,
            maxScale: 4,
            onScaleChange: async scale => _scale.Value = scale,
            content: canvas =>
            {
                canvas.Box(["w-[1600px] p-6 flex flex-wrap gap-4"], content: sheet => { /* the large thing */ });
            });
        #endregion
        PatternDemoNote.RenderCaption(view, "A pan-and-zoom surface above: drag to pan and pinch or scroll to zoom between 25% and 400% over a 1600px-wide sheet the example leaves empty");
    }
}

file sealed class PxExampleInteractiveFormExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task StoreAsync(string name, bool subscribed) => Task.CompletedTask;
    private UI UI { get; } = new(app, new IkonTheme());

    #region example:px-example-interactive-form
    private readonly ClientReactive<string> _name = new("");
    private readonly ClientReactive<bool> _subscribed = new(false);
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _status = new(null);

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["gap-4 max-w-md p-8"], content: view =>
            {
                view.TextField(label: "Name", placeholder: "Your name", bind: _name);

                view.Switch(bind: _subscribed, label: "Subscribe to newsletter");

                view.Button([Button.PrimaryMd], text: "Save",
                    disabled: _busy.Value,
                    onClick: SaveAsync);

                if (_status.Value is { } status)
                {
                    view.Alert("Save failed", SemanticTone.Error, description: status);
                }
            });
        });
    }

    private async Task SaveAsync()
    {
        await _busy.RunAsync(_status, async () =>
        {
            await StoreAsync(_name.Value, _subscribed.Value);
        });
    }
    #endregion

}

file sealed class PxBootSnapshotAndPrivacyExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task SignOutAsync() => Task.CompletedTask;

    public async Task PxBootSnapshotAndPrivacy(UIView view)
    {
        #region example:px-boot-snapshot-and-privacy
        // Live: real content. Snapshot: real content too (opted out of skeletonization).
        view.SnapshotReveal(v =>
        {
            v.Image(["h-8"], src: "/logo.svg", alt: "Acme");
            v.Text([Text.H1], text: "Welcome to Acme");
        });

        // Live: real content. Snapshot: nothing (omit entirely — e.g. a control that is dead before connect).
        view.SnapshotHide(v => v.Button(text: "Sign out", onClick: SignOutAsync));

        // Live: nothing. Snapshot: snapshot-only filler, rendered as authored (not skeletonized).
        view.SnapshotOnly(v => v.Text([Text.Caption], text: "Loading your dashboard…"));
        #endregion
    }
}

file sealed class PxPublicPagesOptingAWholePageOutOfSkeletonizationExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:px-public-pages-opting-a-whole-page-out-of-skeletonization
    private static void RenderGuestPage(UIView view, Action<UIView> content)
    {
        // The whole guest page is public marketing content — safe to reveal in the snapshot. Never
        // route per-user data through this wrapper.
        view.SnapshotReveal(v => v.Column(["min-h-screen"], content: content));
    }
    #endregion

}

file sealed class PxHandBuiltSkeletonsExamples(IApp<SessionIdentity, ClientParameters> app)
{

    public async Task PxHandBuiltSkeletons(UIView view)
    {
        #region example:px-hand-built-skeletons
        view.Skeleton(["w-1/3"], size: SkeletonSize.Xl);
        view.Skeleton(["w-10 h-10 shrink-0"], shape: SkeletonShape.Circle);
        #endregion
        PatternDemoNote.RenderCaption(view, "Two hand-built skeletons above: a one-third-width bar and a round avatar placeholder");
    }

    public async Task PxHandBuiltSkeletons2(UIView view)
    {
        #region example:px-hand-built-skeletons-2
        if (view.IsSnapshot) { /* snapshot-only branch */ }
        #endregion
    }
}

file sealed class PxPerRouteSnapshotsAndSeoExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ListingStore store = new();

    public async Task PxPerRouteSnapshotsAndSeo(UIView view)
    {
        #region example:px-per-route-snapshots-and-seo
        app.OnSnapshotRoutes(async () => (await store.GetListingsAsync()).Select(l => $"/listing/{l.Id}"));
        #endregion
    }
}

file sealed class PxHowToUseItExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly ListingStore store = new();

    public async Task PxHowToUseIt(UIView view)
    {
        #region example:px-how-to-use-it
        app.OnSnapshotRoutes(async () =>
            (await store.GetPublishedArticlesAsync()).Select(a => $"/blog/{a.Slug}"));
        #endregion
    }
}

file sealed class PxSeedRulesAndSnapshotVariantsGuestseedsSignedinseExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static void RenderAdminPanelSkeleton(UIView view) => view.Text(text: "Admin panel skeleton");
    private static void RenderDashboardSkeleton(UIView view) => view.Text(text: "Dashboard skeleton");
    private static void RenderExperienceSkeleton(UIView view) => view.Text(text: "Experience skeleton");
    private static void RenderWelcomeSkeleton(UIView view) => view.Text(text: "Welcome skeleton");

    public async Task PxSeedRulesAndSnapshotVariantsGuestseedsSignedinseeds(UIView view)
    {
        #region example:px-seed-rules-and-snapshot-variants-guestseeds-signedinseeds
        if (view.IsSnapshot)
        {
            switch (view.SnapshotVariant)
            {
                case "admin":      RenderAdminPanelSkeleton(view); break;
                case "dashboard":  RenderDashboardSkeleton(view);  break;
                case "experience": RenderExperienceSkeleton(view); break;
                default:           RenderWelcomeSkeleton(view);    break;   // "welcome" + route captures
            }

            return;
        }
        #endregion
        PatternDemoNote.RenderCaption(view, "Outside a boot-snapshot capture this branch draws nothing; a capture draws the skeleton of the variant it asks for");
    }
}

file sealed class PxOpenAsGuestTheDefaultExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<bool> _isGuest = new(false);
    private static void RenderApp(UIView view) => view.Text(text: "The signed-in product");
    private static void RenderLanding(UIView view) => view.Text(text: "The public landing page");
    private UI UI { get; } = new(app, new IkonTheme());

    public async Task PxOpenAsGuestTheDefault(UIView view)
    {
        #region example:px-open-as-guest-the-default
        UI.Root([Page.Default], content: view =>
        {
            if (_isGuest.Value || view.IsSnapshot)   // _isGuest: ClientReactive set from Context.IsAnonymous at join
            {
                RenderLanding(view);                 // public marketing page, wrapped in SnapshotReveal
                return;
            }
            RenderApp(view);                         // the signed-in product
        });
        #endregion
    }

    public async Task PxOpenAsGuestTheDefault2(UIView view)
    {
        #region example:px-open-as-guest-the-default-2
        view.Button([Button.PrimaryMd], text: "Sign in with Google",
            onClick: async () => await ClientFunctions.LoginAsync("google"));
        #endregion
    }

    public void PxInAppFeedback(UIView view)
    {
        #region example:px-in-app-feedback
        if (view.CanOpenFeedback())
        {
            view.Button([Button.OutlineMd], text: "Send feedback",
                onClick: async () => await ClientFunctions.OpenFeedbackAsync());
        }
        #endregion

        if (!view.CanOpenFeedback())
        {
            PatternDemoNote.RenderCaption(view, "This client cannot open the feedback sheet, so the Send feedback button is hidden");
        }
    }
}
