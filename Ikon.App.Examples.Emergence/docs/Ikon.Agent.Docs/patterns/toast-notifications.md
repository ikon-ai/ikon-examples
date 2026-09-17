<!-- mined-from: Sentinel -->
# Toast Notifications — Auto-dismissing tone-coded pill
<!-- checked-against: 1aefcc371a5092a4 -->
A bottom-right pill that appears for ~3 seconds with an icon, message, tone-keyed colors (success/error/warn/info), and a manual close button. Fired imperatively from any handler via `ShowToast(text, tone)`. State lives in a single `ClientReactive<(string, string, DateTime)?>` so each client sees their own toast.

## When to use

Confirming success of any side-effect the user can't immediately verify visually — "Webhook sent", "Camera renamed", "Settings saved", "Test alert delivered". Avoid for errors that require action: those go in an inline alert or banner. The 3-second window means the toast must be self-contained — no buttons that need clicking.

## Snippet

```csharp
private const int ToastLifetimeMs = 3000;

private readonly ClientReactive<(string Text, string Tone, DateTime At)?> _toast =
    new(initialValue: ((string, string, DateTime)?)null);

/// <summary>
/// Called from a handler, where the client scope is active. The delayed clear is background
/// work off that handler's stack, so it names the session captured here instead of relying on
/// an ambient scope; matching on the timestamp keeps it from wiping a newer toast shown inside
/// the window.
/// </summary>
private void ShowToast(string text, string tone = "success")
{
    var shownAt = DateTime.UtcNow;
    _toast.Value = (text, tone, shownAt);
    _ = ClearLaterAsync(ReactiveScope.ClientId, shownAt);
}

private async Task ClearLaterAsync(int clientSessionId, DateTime shownAt)
{
    await Task.Delay(ToastLifetimeMs);
    _toast.UpdateFor(clientSessionId, current => current is { At: var at } && at == shownAt ? null : current);
}

private void RenderToast(UIView view)
{
    if (_toast.Value is not { } t)
    {
        return;
    }

    var ageMs = (DateTime.UtcNow - t.At).TotalMilliseconds;

    if (ageMs > ToastLifetimeMs)
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

- `ShowToast` schedules the clear that makes the toast vanish without another reactive update. That continuation is background work, so it does not touch `_toast.Value` — it writes through `UpdateFor` with the `ReactiveScope.ClientId` captured while the handler's scope was active, and only when the toast it was scheduled for is still the one showing.
- The render method also early-returns when the toast is older than `ToastLifetimeMs`, so a render triggered by any other state change drops a stale toast on its own. The age check is the safety net, not the mechanism.
- Tone-keyed tuples (`bg, ring, accent, icon`) keep the styling decision in one switch — adding a new tone is a single line.
- `ClientReactive<...>` is correct here: a toast on operator A's screen should not appear on operator B's screen.
- The motion class fades + slides up on entry; manual close is provided so users who need the message gone faster don't have to wait.

## See also

- `busy-flag-loading` — confirm async ops by clearing the busy flag and firing `ShowToast("Saved")` in the `finally` block
