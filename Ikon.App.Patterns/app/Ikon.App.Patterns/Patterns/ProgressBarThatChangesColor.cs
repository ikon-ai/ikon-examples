namespace Ikon.App.Patterns.Patterns;

// Pattern: progress-bar-that-changes-color — see docs/patterns/progress-bar-that-changes-color.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ProgressBarThatChangesColor : IPatternDemo
{
    public string Slug => "progress-bar-that-changes-color";
    public string Title => "Progress bar that animates and recolors";
    public string Category => "Status & feedback";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-progress-bar-that-changes-color
    private const int TotalSteps = 10;

    private readonly Reactive<int> _step = new(3);

    // The fill colour is a function of progress, not a stored field -- one source of truth, and no
    // way for the colour to disagree with the bar.
    private static string VariantFor(double percent) => percent switch
    {
        >= 100 => Progress.Variant.Success,
        >= 60 => Progress.Variant.Default,
        >= 30 => Progress.Variant.Warning,
        _ => Progress.Variant.Error,
    };

    private void Render(IView view)
    {
        var percent = 100.0 * _step.Value / TotalSteps;

        view.Column(["gap-3"], content: col =>
        {
            // ComposeIndicator builds the fill class list: base recipe, then the variant, then
            // caller overrides LAST so they win. The transition is what makes the width glide
            // instead of jumping -- Progress animates nothing on its own.
            col.Progress(
                value: percent,
                max: 100,
                indicatorStyle: [Progress.ComposeIndicator(
                    variant: VariantFor(percent),
                    indeterminate: false,
                    "transition-all duration-500 ease-out")]);

            col.Text(["text-muted-foreground text-sm"], text: $"Step {_step.Value} of {TotalSteps}");

            col.Row(["gap-2"], content: row =>
            {
                row.Button(
                    disabled: _step.Value == 0,
                    onClick: () => _step.Value--,
                    content: v => v.Text(text: "Back"));

                row.Button(
                    disabled: _step.Value == TotalSteps,
                    onClick: () => _step.Value++,
                    content: v => v.Text(text: "Next"));
            });
        });
    }
    #endregion
}
