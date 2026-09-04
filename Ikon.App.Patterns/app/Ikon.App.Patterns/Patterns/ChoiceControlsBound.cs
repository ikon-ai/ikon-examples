namespace Ikon.App.Patterns.Patterns;

// Pattern: choice-controls-bound — see docs/patterns/choice-controls-bound.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ChoiceControlsBound : IPatternDemo
{
    public string Slug => "choice-controls-bound";
    public string Title => "Choice controls bound to state";
    public string Category => "Forms & input";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-choice-controls-bound
    private readonly UserReactive<string> _plan = new("standard");
    private readonly UserReactive<bool> _notify = new(true);
    private readonly UserReactive<double> _volume = new(0.5);
    private readonly UserReactive<string> _startDate = new("");
    private readonly UserReactive<string> _startTime = new("09:00");

    private void Render(IView view)
    {
        view.Column(["gap-4"], content: col =>
        {
            // bind: is the two-way form -- it reads .Value and writes back on change. With bind:
            // set, value: is ignored; onValueChange still fires after the write-back.
            col.RadioGroup(label: "Plan", bind: _plan, content: group =>
            {
                group.RadioGroupItem(value: "standard", content: v => v.Text(text: "Standard"));
                group.RadioGroupItem(value: "pro", content: v => v.Text(text: "Pro"));
            });

            // Toggle takes NEITHER bind: nor formValue: -- it is value: + onValueChange only.
            // (Checkbox and Switch take both; the three are not interchangeable.)
            col.Toggle(
                label: "Email me",
                value: _notify.Value,
                onValueChange: async on => _notify.Value = on);

            // Slider has TWO overloads and mixing their shapes is the classic CS1503. On this,
            // the canonical style-first one, value: is a LIST of thumb positions -- so `value: 0.5`
            // does not compile. bind: lives here and takes a Reactive<double>.
            col.Slider(["w-full"], label: "Volume", min: 0, max: 1, step: 0.05, bind: _volume);

            // The scalar convenience overload is value-FIRST: the one exception to the
            // style-array-first rule.
            col.Slider(_volume.Value, style: ["w-full"], min: 0, max: 1, step: 0.05,
                onValueChange: async v => _volume.Value = v);

            // Date and time are ISO strings, not DateTime, and carry no bind: -- pair value: with
            // onValueChange.
            col.DatePicker(
                label: "Starts",
                value: _startDate.Value,
                onValueChange: async iso => _startDate.Value = iso);

            col.TimePicker(
                label: "At",
                value: _startTime.Value,
                granularity: TimeGranularity.Minute,
                onValueChange: async time => _startTime.Value = time);
        });
    }
    #endregion
}
