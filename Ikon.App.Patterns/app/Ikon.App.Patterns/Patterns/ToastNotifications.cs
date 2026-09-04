namespace Ikon.App.Patterns.Patterns;

// Pattern: toast-notifications — see docs/patterns/toast-notifications.md.
// Fully self-contained: the toast state, the imperative ShowToast, the auto-fading RenderToast,
// and the top-level mount are the whole pattern.
internal sealed class ToastNotifications : IPatternDemo
{
    public string Slug => "toast-notifications";
    public string Title => "Toast notifications";
    public string Category => "Feedback";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-toast-notifications
    private readonly ClientReactive<(string Text, string Tone, DateTime At)?> _toast =
        new(initialValue: ((string, string, DateTime)?)null);

    private void ShowToast(string text, string tone = "success")
    {
        _toast.Value = (text, tone, DateTime.UtcNow);
    }

    private void RenderToast(UIView view)
    {
        if (_toast.Value is not { } t)
        {
            return;
        }

        var ageMs = (DateTime.UtcNow - t.At).TotalMilliseconds;

        if (ageMs > 3000)
        {
            return;
        }

        var (bg, ring, accent, icon) = t.Tone switch
        {
            "error" => ("bg-rose-500/15", "ring-rose-500/40", "text-rose-200", "alert-circle"),
            "warn"  => ("bg-amber-500/15", "ring-amber-500/40", "text-amber-200", "alert-triangle"),
            "info"  => ("bg-zinc-800",     "ring-zinc-700",     "text-zinc-200",  "info"),
            _       => ("bg-emerald-500/15","ring-emerald-500/40","text-emerald-200","check-circle")
        };

        view.Box(["fixed bottom-6 right-6 z-[60] motion-[0:opacity-0+translate-y-2,30:opacity-100+translate-y-0] motion-duration-300ms"], content: outer =>
        {
            outer.Row([$"items-center gap-2 px-3 py-2 rounded-md backdrop-blur-md ring-1 shadow-xl shadow-black/40 {bg} {ring}"], content: pill =>
            {
                pill.Icon([$"w-4 h-4 {accent}"], name: icon);
                pill.Text([$"text-sm font-medium {accent}"], t.Text);
                pill.Button([$"ml-2 px-1 py-0.5 rounded {accent} hover:bg-black/30"],
                    "✕",
                    onClick: async () => _toast.Value = null);
            });
        });
    }

    // Mount once at the top level:
    private void Render(IView view)
    {
        view.Column(["h-screen w-full"], content: view =>
        {
            // ... main UI ...
            RenderToast(view);
        });
    }
    #endregion
}
