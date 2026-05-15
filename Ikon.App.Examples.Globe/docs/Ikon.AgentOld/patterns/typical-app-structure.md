# Typical App Structure

The skeleton every Ikon AI App starts from. Get this right first; everything else hangs off it.

## When to use

Always — every app file uses this shape. If you're starting from a stub, replace it with this skeleton, then build out.

## Snippet

```csharp
return await App.Run(args);

public sealed record SessionIdentity(string UserId);
public sealed record ClientParameters(string ClientName);

[App]
public class MyApp(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<int> _count = new(0);

    public Task Main()
    {
        UI.Root(["h-screen flex flex-col"], content: view =>
        {
            // ── Header region ───────────────────────────────────────────────
            view.Row(["flex-shrink-0 px-6 py-4 border-b items-center justify-between"], content: view =>
            {
                view.Text([Text.H3, "tracking-wide"], text: "MyApp");
            });

            // ── Main region ─────────────────────────────────────────────────
            view.Column([Layout.Page, "flex-1 min-h-0 gap-4 py-8"], content: view =>
            {
                view.Text([Text.H1], text: "Welcome");
                view.Text([Text.Body, "text-muted-foreground"], text: $"count: {_count.Value}");
                view.Button(
                    style: [Button.Default, "transition-colors duration-150 hover:opacity-90"],
                    onClick: () => _count.Value++,
                    content: v => v.Text(text: "Increment"));
            });
        });
        return Task.CompletedTask;
    }
}
```

## Notes

- Top-level statement `App.Run(args)` first; not `static void Main`.
- `[App]` attribute on the class, primary constructor `(IApp<SessionIdentity, ClientParameters> app)`.
- `SessionIdentity` and `ClientParameters` are `record`s (or `sealed record`s).
- `Reactive<T>` for shared state; `ClientReactive<T>` for per-client; `UserReactive<T>` for per-user. Never plain `int` / `List<T>`.
- UI is built inside `UI.Root([...], content: view => { ... })`. Don't return view trees from methods.
- **Match the plan's STRUCTURE section.** If STRUCTURE names header / sidebar / main, build those regions explicitly — `view.Row` for the header, `view.Column` for the sidebar, etc., each with its own `flex-shrink-0` / `flex-1 min-h-0` discipline so they don't collapse. Don't ship a single centered card if the plan named multiple regions.
- The root `["h-screen flex flex-col"]` + region-by-region children is the standard shape for any multi-region app. For a genuine single-region app, `[Layout.Page, "py-8 gap-4"]` directly inside `UI.Root` is fine.
- GlobalUsings imports every Ikon namespace — explicit `using Ikon.X;` is a CS0234.
- Theme: when STYLING is in the plan, pass `new IkonTheme { ["primary"] = "amber-400", ["background"] = "zinc-950", ["text-primary"] = "zinc-50", ... }` to `UI` — every entry is a single CSS-variable override. Values are Crosswind/Tailwind class names. When STYLING is absent, pass `new IkonTheme()` (no body) for the platform default. `IkonTheme` lives in `Ikon.Parallax` and is auto-imported via `global using`; never write `class IkonTheme : ITheme` or `class Theme : ITheme` in the app source. Never `Theming.Apply(...)`, `Theming.Custom(...)`, or `Theme.Custom(...)` — those factories were retired. See `theme-commitment` for the indexer-key taxonomy + Styling Oracle flow.

## See also

- `theme-commitment` — when STYLING is declared in the plan, pair this skeleton with that pattern.
- `busy-flag-loading` — how to gate async work with a Reactive flag and visible loading state.
- `reactive-state` (top-level guide) — full reactive lifecycle.
- `app-structure` (top-level guide) — host services, partial classes, lifecycle.
