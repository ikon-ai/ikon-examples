namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: guided-tour — see docs/patterns/guided-tour.md.
// The demo opens the tour on a button instead of on first visit, so the gallery shows it on demand;
// the region is what an app copies.
internal sealed class GuidedTour : IPatternDemo
{
    public string Slug => "guided-tour";
    public string Title => "Guided tour and what's new";
    public string Category => "Layout";

    public void RenderDemo(IView view)
    {
        view.Row(["gap-2"], content: row =>
        {
            row.Button([Button.PrimaryMd], text: "Take the tour", props: TourTarget.Props("tour-button"),
                onClick: async () => await StartTourAsync());
            row.Button([Button.OutlineMd], text: "What's new", onClick: async () => await OpenWhatsNewAsync());
        });
        RenderGuide(view);
    }

    private sealed record Step(string Id, string Title, string Text, string Icon, Func<bool> Applies, string? Target = null);

    private readonly ReactiveList<string> _projects = new();
    private static string CurrentVersion => "1.4.0";

    #region example:pattern-guided-tour
    // What a person has already seen is kept per user across sessions; what is open stays per tab.
    private readonly PersistentUserReactiveHashSet<string> _seenGuides = new();
    private readonly ClientReactive<string?> _openGuide = new(null);
    private readonly ClientReactive<int> _guideStep = new(0);

    // Each step says who it applies to, so someone with nothing yet is never sent to an empty page.
    // A step with a target points at the control marked with TourTarget.Props of that name. Any
    // Target makes the whole tour web-only, so leave targets out of a tour a Flutter client shows.
    private Step[] TourSteps =>
    [
        new("welcome", "Welcome", "A one-minute look at where things are.", "sparkles", () => true),
        new("first-project", "Start a project", "Create one from the Projects page.", "folder-plus", () => _projects.Count == 0),
        new("projects", "Your projects", "Open one to see its files and history.", "folder-open", () => _projects.Count > 0),
        new("help", "Help is here", "This button opens the tour again.", "life-buoy", () => true, Target: "tour-button"),
    ];

    private static readonly Step[] WhatsNew =
    [
        new("export", "Export to PDF", "Any project prints to a PDF from its menu.", "file-down", () => true),
    ];

    // On arrival: the tour once per person, else this version's highlights once. A join handler already
    // waits for the user's stored values; LoadForAsync is needed only when this runs outside one.
    private async Task OnSignedInAsync(string userId)
    {
        await _seenGuides.LoadForAsync(userId);

        if (!_seenGuides.Contains("tour"))
        {
            _seenGuides.Add("tour");
            _guideStep.Value = 0;
            _openGuide.Value = "tour";
        }
        else if (!_seenGuides.Contains($"whats-new:{CurrentVersion}"))
        {
            _seenGuides.Add($"whats-new:{CurrentVersion}");
            _guideStep.Value = 0;
            _openGuide.Value = "whats-new";
        }
    }

    private async Task StartTourAsync()
    {
        _guideStep.Value = 0;
        _openGuide.Value = "tour";
        await Task.CompletedTask;
    }

    private async Task OpenWhatsNewAsync()
    {
        _guideStep.Value = 0;
        _openGuide.Value = "whats-new";
        await Task.CompletedTask;
    }

    private void RenderGuide(IView view)
    {
        if (_openGuide.Value is not { } guide)
        {
            return;
        }

        var steps = (guide == "tour" ? TourSteps : WhatsNew)
            .Where(step => step.Applies())
            .Select(step => new StepDialogStep(step.Title, step.Text, Icon: step.Icon, Target: step.Target, TargetSide: Side.Bottom))
            .ToList();

        view.StepDialog(steps, _guideStep.Value,
            open: true,
            onOpenChange: async open => { if (!open) { _openGuide.Value = null; } },
            onIndexChange: async index => _guideStep.Value = index,
            onFinish: async () => _openGuide.Value = null,
            onSkip: guide == "tour" ? async () => _openGuide.Value = null : null,
            // The words are English unless the app passes its own; translate all six for a translated app.
            labels: new StepDialogLabels(Back: "Back", Next: "Next", Done: "Done", Skip: "Skip", StepOf: "{0} of {1}", Close: "Close"));
    }
    #endregion

    // Outside the example: the gallery has no sign-in, so the arrival hook is referenced here to keep
    // the warnings-as-errors build from flagging it.
    private void KeepPatternMembersReferenced() => _ = OnSignedInAsync("demo-user");
}
