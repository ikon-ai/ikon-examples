<!-- mined-from: Sentinel -->
# Toast Notifications — Auto-dismissing tone-coded pill

A bottom-right pill that appears for ~3 seconds with an icon, message, tone-keyed colors (success/error/warn/info), and a manual close button. Fired imperatively from any handler via `ShowToast(text, tone)`. State lives in a single `ClientReactive<(string, string, DateTime)?>` so each client sees their own toast.

## When to use

Confirming success of any side-effect the user can't immediately verify visually — "Webhook sent", "Camera renamed", "Settings saved", "Test alert delivered". Avoid for errors that require action: those go in an inline alert or banner. The 3-second window means the toast must be self-contained — no buttons that need clicking.

## Snippet

```csharp
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
```

## Notes

- The render method early-returns when `_toast.Value` is null OR when the toast is older than 3000ms — no background timer needed. The reactive system re-renders on every state change anyway, and the auto-fade happens because the next render after the 3s mark will return early.
- For cases where the toast must vanish even without another reactive update, schedule a single `Task.Delay(3000)` after `ShowToast` that nulls `_toast.Value`. The age-check is the safety net.
- Tone-keyed tuples (`bg, ring, accent, icon`) keep the styling decision in one switch — adding a new tone is a single line.
- `ClientReactive<...>` is correct here: a toast on operator A's screen should not appear on operator B's screen.
- The motion class fades + slides up on entry; manual close is provided so users who need the message gone faster don't have to wait.

## See also

- `busy-flag-loading` — confirm async ops by clearing the busy flag and firing `ShowToast("Saved")` in the `finally` block
