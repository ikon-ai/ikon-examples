// Generated holder for ikon-parallax-library-overview.md — one class per guide SECTION, because a section is one
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

file sealed class DocPxReactiveUiUpdates(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:px-reactive-ui-updates
    private readonly Reactive<int> _count = new(0);
    private readonly Reactive<string> _message = new("Hello");

    // When _count.Value changes, only UI that reads _count.Value re-renders
    // When _message.Value changes, only UI that reads _message.Value re-renders
    #endregion

}

file sealed class DocPxThemedComponentsAndCrosswindStyling(IApp<SessionIdentity, ClientParams> app)
{
    private static Task SaveAsync() => Task.CompletedTask;


    public async Task PxThemedComponentsAndCrosswindStyling(UIView view)
    {
        #region docsnippet:px-themed-components-and-crosswind-styling
        view.Button(text: "Save", onClick: SaveAsync);                  // fully themed as-is
        view.Button([Button.PrimaryMd, "w-full"], text: "Save", onClick: SaveAsync);
        view.Button(["default", "w-full"], text: "Save", onClick: SaveAsync);   // same: Button's default IS PrimaryMd
        #endregion
    }
}

file sealed class DocPxSettingUpAUi2(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:px-setting-up-a-ui-2
    private UI UI { get; } = new(app, new IkonTheme
    {
        ["primary"] = "amber-400",
        ["background"] = "zinc-950",
    });
    #endregion
}

file sealed class DocPxSettingUpAUi(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:px-setting-up-a-ui
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

file sealed class DocPxLightDarkSwitchingWithUsetheme(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:px-light-dark-switching-with-usetheme
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

file sealed class DocPxSharedPerClientPerUserPerMount2(IApp<SessionIdentity, ClientParams> app)
{
    private static IEnumerable<string> LoadCart(string userId) => [];

    #region docsnippet:px-shared-per-client-per-user-per-mount-2
    private readonly ClientReactive<string> _welcome =
        ClientReactive.Create(sessionId => $"Welcome, session {sessionId}!");

    private readonly UserReactiveList<string> _cart =
        new(userId => LoadCart(userId));
    #endregion
}

file sealed class DocPxSharedPerClientPerUserPerMount(IApp<SessionIdentity, ClientParams> app)
{
    private static IEnumerable<string> LoadCart(string userId) => [];

    #region docsnippet:px-shared-per-client-per-user-per-mount
    private readonly Reactive<int> _sharedCounter = new(0);
    private readonly ClientReactive<string> _draft = new("");
    private readonly UserReactive<string> _language = new("en");
    #endregion

}

file sealed class DocPxReactiveCollectionsReactivelistAndReactivedictio(IApp<SessionIdentity, ClientParams> app)
{
    private readonly TodoItem item = new();
    private static void Render(TodoItem todo) { }

    #region docsnippet:px-reactive-collections-reactivelist-and-reactivedictionary
    private readonly ReactiveList<TodoItem> _todos = new();
    private readonly ReactiveDictionary<string, int> _scores = new();
    #endregion

    public async Task PxReactiveCollectionsReactivelistAndReactivedictionary2(UIView view)
    {
        #region docsnippet:px-reactive-collections-reactivelist-and-reactivedictionary-2
        _todos.Add(item);                    // also: AddRange, Insert, Remove, RemoveAt,
        _todos.RemoveAll(t => t.Done);       // RemoveAll, Clear, ReplaceAll, Sort
        _todos.Update(list => list.OrderBy(t => t.Priority));  // whole-list transform, one notification

        _scores["anna"] = 10;                // add-or-replace, one notification
        _scores.Update(map => map["anna"]++); // atomic read-modify-write under the lock

        foreach (var todo in _todos) { Render(todo); } // enumerate the reactive directly
        #endregion
    }
}

file sealed class DocPxBackgroundWorkTheForMethods(IApp<SessionIdentity, ClientParams> app)
{
    private readonly ClientReactive<string> _draft = new("");
    private readonly ClientReactiveList<TodoItem> _items = new();
    private readonly TodoItem item = new();
    private static Task<string> LoadDraftAsync() => Task.FromResult("");


    public async Task PxBackgroundWorkTheForMethods(UIView view)
    {
        #region docsnippet:px-background-work-the-for-methods
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

file sealed class DocPxTheBusyStatusPattern(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Reactive<IReadOnlyList<string>> _entries = new([]);
    private static Task<IReadOnlyList<string>> LoadEntriesAsync() => Task.FromResult<IReadOnlyList<string>>([]);

    #region docsnippet:px-the-busy-status-pattern
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

file sealed class DocPxTwoWayBinding(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UserReactive<bool> _subscribed = new(false);
    private readonly ClientReactive<string> _name = new("");


    public async Task PxTwoWayBinding(UIView view)
    {
        #region docsnippet:px-two-way-binding
        view.TextField(["flex-1"], label: "Name", bind: _name);
        view.Switch(bind: _subscribed, label: "Subscribe to newsletter");
        #endregion
    }
}

file sealed class DocPxAppChromeAndSemantictone(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Exception ex = new("example");
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:px-app-chrome-and-semantictone-2
    private readonly Toasts _toasts = new();
    #endregion

    public async Task PxAppChromeAndSemantictone(UIView view)
    {
        #region docsnippet:px-app-chrome-and-semantictone
        view.Badge("Live", SemanticTone.Success);
        view.Alert("Import failed", SemanticTone.Error, description: "The file is not valid CSV");
        view.StatCard("Revenue", "$12,400", delta: "+8%", trend: StatTrend.Up, icon: "trending-up",
            iconTone: SemanticTone.Success);
        #endregion
    }

    public async Task PxAppChromeAndSemantictone3(UIView view)
    {
        #region docsnippet:px-app-chrome-and-semantictone-3
        // In UI.Root, mount exactly once:
        view.ToastHost(_toasts);

        // From any handler:
        _toasts.Success("Saved");
        _toasts.Error("Upload failed", ex.Message);
        #endregion
    }
}

file sealed class DocPxFormsAndDialogsWithFormstate(IApp<SessionIdentity, ClientParams> app)
{
    private readonly ExistingPreset existing = new();
    private static Task SavePresetAsync(PresetDraft draft) => Task.CompletedTask;

    #region docsnippet:px-forms-and-dialogs-with-formstate
    private sealed record PresetDraft(string Name = "", bool Public = false);
    private readonly FormState<PresetDraft> _preset = new(() => new PresetDraft());
    #endregion

    public async Task PxFormsAndDialogsWithFormstate2(UIView view)
    {
        #region docsnippet:px-forms-and-dialogs-with-formstate-2
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

file sealed class DocPxStylingWithCrosswind(IApp<SessionIdentity, ClientParams> app)
{
    private static Task SubmitAsync() => Task.CompletedTask;


    public async Task PxStylingWithCrosswind(UIView view)
    {
        #region docsnippet:px-styling-with-crosswind
        view.Button([Button.PrimaryMd, "mt-4 self-center"], text: "Submit", onClick: SubmitAsync);
        view.Box(["bg-card border border-secondary p-6 rounded-2xl"], content: v => { });
        view.Text([Text.Caption], text: "Updated just now");
        #endregion
    }
}

file sealed class DocPxMergeSemanticsTheDefaultMarker(IApp<SessionIdentity, ClientParams> app)
{
    private readonly ClientReactive<string> _name = new("");


    public async Task PxMergeSemanticsTheDefaultMarker(UIView view)
    {
        #region docsnippet:px-merge-semantics-the-default-marker
        view.TextField(bind: _name);                        // fully themed input
        view.TextField(["default", "w-full"], bind: _name); // themed input, full width  ← what you usually want
        view.TextField(["w-full"], bind: _name);            // an unstyled box that is full width
        #endregion
    }

    public async Task PxMergeSemanticsTheDefaultMarker2(UIView view)
    {
        #region docsnippet:px-merge-semantics-the-default-marker-2
        view.DatePicker();                                        // fully themed trigger + popover + calendar
        view.DatePicker(triggerStyle: ["default", "w-full"]);     // themed trigger, full width; popover untouched
        view.DatePicker(triggerStyle: ["w-full"]);                // an unstyled trigger; popover still themed
        #endregion
    }

    public async Task PxMergeSemanticsTheDefaultMarker3(UIView view)
    {
        #region docsnippet:px-merge-semantics-the-default-marker-3
        view.Column(["gap-4"]);                    // flex flex-col gap-4 — the flex base is not droppable
        view.ScrollArea(viewportStyle: ["px-8"]);  // h-full w-full px-8 — the viewport still fills, and scrolls
        #endregion
    }
}

file sealed class DocPxDefaultStylingAndAutoComposedIndicators(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Reactive<bool> _done = new(false);
    private readonly Reactive<bool> _on = new(false);
    private readonly Reactive<string> _text = new("");


    public async Task PxDefaultStylingAndAutoComposedIndicators(UIView view)
    {
        #region docsnippet:px-default-styling-and-auto-composed-indicators
        view.Checkbox(bind: _done);
        view.Switch(bind: _on);
        view.TextField(bind: _text);
        view.Button(text: "Submit", onClick: async () => { });
        #endregion
    }
}

file sealed class DocPxIconButtons(IApp<SessionIdentity, ClientParams> app)
{
    private static Task RefreshAsync() => Task.CompletedTask;


    public async Task PxIconButtons(UIView view)
    {
        #region docsnippet:px-icon-buttons
        view.Button([Button.GhostMd, Button.IconSm],   // h-8 w-8 p-0 min-h-0 — last wins
            icon: "refresh-cw",
            tooltip: "Refresh",
            onClick: RefreshAsync);
        #endregion
    }
}

file sealed class DocPxTooltipsAndNamingControls(IApp<SessionIdentity, ClientParams> app)
{
    private static Task DeleteAsync() => Task.CompletedTask;


    public async Task PxTooltipsAndNamingControls(UIView view)
    {
        #region docsnippet:px-tooltips-and-naming-controls
        view.Button([Button.GhostMd, Button.Icon],
            icon: "trash-2",
            text: "Delete",       // the accessible name (aria-label in icon mode)
            tooltip: "Delete",    // the visible hover bubble
            onClick: DeleteAsync);
        #endregion
    }
}

file sealed class DocPxScrollareaAndAutoScroll(IApp<SessionIdentity, ClientParams> app)
{
    private readonly ReactiveList<string> _messages = new();
    private readonly UIView anchor = null!;
    private readonly int version = 1;


    public async Task PxScrollareaAndAutoScroll(UIView view)
    {
        #region docsnippet:px-scrollarea-and-auto-scroll
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
        #region docsnippet:px-scrollarea-and-auto-scroll-2
        anchor.FocusHint(new FocusHintProps { Priority = FocusPriority.Assertive },
            key: $"scroll-{version}");
        #endregion
    }
}

file sealed class DocPxScrollingInsideAFlexParent(IApp<SessionIdentity, ClientParams> app)
{

    public async Task PxScrollingInsideAFlexParent(UIView view)
    {
        #region docsnippet:px-scrolling-inside-a-flex-parent
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
    }

    public async Task PxScrollingInsideAFlexParent2(UIView view)
    {
        #region docsnippet:px-scrolling-inside-a-flex-parent-2
        view.ScrollColumn(
            style: ["h-[82vh] w-full sm:max-w-[560px] rounded-2xl bg-card"],
            header: h => h.Row(["px-5 py-4 border-b"], content: title => { }),
            footer: f => f.Row(["p-3 border-t"], content: composer => { }),
            content: body => body.Column(["gap-3"], content: messages => { }));
        #endregion
    }

    public async Task PxScrollingInsideAFlexParent3(UIView view)
    {
        #region docsnippet:px-scrolling-inside-a-flex-parent-3
        view.Column(["flex-1 min-h-0 overflow-y-auto", Scrollbar.Thin], content: rows => { });
        #endregion
    }

    public async Task PxScrollingInsideAFlexParent4(UIView view)
    {
        #region docsnippet:px-scrolling-inside-a-flex-parent-4
        view.Row(["overflow-x-auto gap-2", Scrollbar.Thin], content: chips => { });
        #endregion
    }
}

file sealed class DocPxPanzoomViewingSomethingLargerThanTheScreen(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Reactive<double> _scale = new(1);


    public async Task PxPanzoomViewingSomethingLargerThanTheScreen(UIView view)
    {
        #region docsnippet:px-panzoom-viewing-something-larger-than-the-screen
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
    }
}

file sealed class DocPxExampleInteractiveForm(IApp<SessionIdentity, ClientParams> app)
{
    private static Task StoreAsync(string name, bool subscribed) => Task.CompletedTask;
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:px-example-interactive-form
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

file sealed class DocPxBootSnapshotAndPrivacy(IApp<SessionIdentity, ClientParams> app)
{
    private static Task SignOutAsync() => Task.CompletedTask;


    public async Task PxBootSnapshotAndPrivacy(UIView view)
    {
        #region docsnippet:px-boot-snapshot-and-privacy
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

file sealed class DocPxPublicPagesOptingAWholePageOutOfSkeletonization(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:px-public-pages-opting-a-whole-page-out-of-skeletonization
    private static void RenderGuestPage(UIView view, Action<UIView> content)
    {
        // The whole guest page is public marketing content — safe to reveal in the snapshot. Never
        // route per-user data through this wrapper.
        view.SnapshotReveal(v => v.Column(["min-h-screen"], content: content));
    }
    #endregion

}

file sealed class DocPxHandBuiltSkeletons(IApp<SessionIdentity, ClientParams> app)
{

    public async Task PxHandBuiltSkeletons(UIView view)
    {
        #region docsnippet:px-hand-built-skeletons
        view.Skeleton(["w-1/3"], size: SkeletonSize.Xl);
        view.Skeleton(shape: SkeletonShape.Circle, size: SkeletonSize.Lg);
        #endregion
    }

    public async Task PxHandBuiltSkeletons2(UIView view)
    {
        #region docsnippet:px-hand-built-skeletons-2
        if (view.IsSnapshot) { /* snapshot-only branch */ }
        #endregion
    }
}

file sealed class DocPxPerRouteSnapshotsAndSeo(IApp<SessionIdentity, ClientParams> app)
{
    private readonly ListingStore store = new();


    public async Task PxPerRouteSnapshotsAndSeo(UIView view)
    {
        #region docsnippet:px-per-route-snapshots-and-seo
        app.OnSnapshotRoutes(async () => (await store.GetListingsAsync()).Select(l => $"/listing/{l.Id}"));
        #endregion
    }
}

file sealed class DocPxHowToUseIt(IApp<SessionIdentity, ClientParams> app)
{
    private readonly ListingStore store = new();


    public async Task PxHowToUseIt(UIView view)
    {
        #region docsnippet:px-how-to-use-it
        app.OnSnapshotRoutes(async () =>
            (await store.GetPublishedArticlesAsync()).Select(a => $"/blog/{a.Slug}"));
        #endregion
    }
}

file sealed class DocPxSeedRulesAndSnapshotVariantsGuestseedsSignedinse(IApp<SessionIdentity, ClientParams> app)
{
    private static void RenderAdminPanelSkeleton(UIView view) { }
    private static void RenderDashboardSkeleton(UIView view) { }
    private static void RenderExperienceSkeleton(UIView view) { }
    private static void RenderWelcomeSkeleton(UIView view) { }


    public async Task PxSeedRulesAndSnapshotVariantsGuestseedsSignedinseeds(UIView view)
    {
        #region docsnippet:px-seed-rules-and-snapshot-variants-guestseeds-signedinseeds
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
    }
}

file sealed class DocPxOpenAsGuestTheDefault(IApp<SessionIdentity, ClientParams> app)
{
    private readonly Reactive<bool> _isGuest = new(false);
    private static void RenderApp(UIView view) { }
    private static void RenderLanding(UIView view) { }
    private UI UI { get; } = new(app, new IkonTheme());


    public async Task PxOpenAsGuestTheDefault(UIView view)
    {
        #region docsnippet:px-open-as-guest-the-default
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
        #region docsnippet:px-open-as-guest-the-default-2
        view.Button([Button.PrimaryMd], text: "Sign in with Google",
            onClick: async () => await ClientFunctions.LoginAsync("google"));
        #endregion
    }
}
