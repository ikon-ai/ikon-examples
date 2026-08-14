# App Structure

## Minimal App Template

```csharp
return await App.Run(args);

public record SessionIdentity(string? UserId);
public record ClientParameters(string Name = "Ikon");

[App]
public class MyApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());
    private Audio Audio { get; } = new(app);

    // Shared state — same value for all clients; list mutations notify on their own
    private readonly ReactiveList<string> _messages = new();

    // Per-client state — each client has its own value
    private readonly ClientReactive<string> _input = new("");

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            // h-screen keeps the app within the viewport — never let the page grow beyond the screen
            view.Column(["h-screen"], content: view =>
            {
                view.Text([Text.H2, "p-4 flex-shrink-0"], "Hello!");

                // ScrollArea for unbounded content (chat, lists, feeds)
                view.ScrollArea(rootStyle: ["flex-1 min-h-0 px-4"], content: view =>
                {
                    foreach (var msg in _messages)
                    {
                        view.Text([Text.Body], msg);
                    }
                });

                view.Row(["p-4 gap-2 flex-shrink-0"], content: view =>
                {
                    view.TextField(bind: _input, style: ["flex-1"], placeholder: "Type a message...",
                        onSubmit: async submitted =>
                        {
                            // Use the `submitted` parameter, NOT `_input.Value`. onValueChange is a separate
                            // round-trip and may not have landed yet when onSubmit fires (fast typists hit Enter
                            // before the last keystroke arrives). Reading the bound reactive can give stale text.
                            if (!string.IsNullOrWhiteSpace(submitted))
                            {
                                _messages.Add(submitted);
                                _input.Value = "";
                            }
                        },
                        clearOnSubmit: true);
                    view.Button([Button.PrimaryMd], text: "Send", onClick: async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(_input.Value))
                        {
                            _messages.Add(_input.Value);
                            _input.Value = "";
                        }
                    });
                });
            });
        });
    }
}
```

## Viewport Layout

Apps must fit within the browser viewport — never let the page grow beyond the screen and rely on the browser scrollbar. The root layout should use `h-screen` (or `min-h-screen` with `overflow-hidden`) so the app fills exactly the viewport.

Use `ScrollArea` for content that can grow unboundedly (chat messages, lists, logs, feeds). The surrounding container must have a bounded height (e.g. `flex-1 min-h-0`) so the scroll area knows when to scroll.

```csharp
// WRONG — page grows forever, browser scrollbar appears
view.Column(["min-h-screen"], content: view =>
{
    RenderHeader(view);
    foreach (var msg in _messages) { RenderMessage(view, msg); }  // unbounded
    RenderInput(view);
});

// CORRECT — fixed viewport, chat area scrolls internally
view.Column(["h-screen"], content: view =>
{
    RenderHeader(view);                                           // flex-shrink-0
    view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: view =>
    {
        foreach (var msg in _messages) { RenderMessage(view, msg); }
    });
    RenderInput(view);                                            // flex-shrink-0
});
```

### Auto-scrolling live content

For `ScrollArea` showing live-updating content (chat, logs, streaming), add polite auto-scroll:

    view.ScrollArea(
        autoScroll: true,
        autoScrollKey: messages.Count,
        rootStyle: ["flex-1 min-h-0"],
        content: view => { ... });

`autoScroll: true` scrolls to bottom when `autoScrollKey` changes, but respects user scroll position — if the user scrolled up, it won't force them down. Do not use on static scroll areas.

## App File Structure

Key elements:

- `return await App.Run(args);` - Required entry point; it is the first *statement* in the file, but any `using` directives must appear ABOVE it (a `using` placed after it is CS1529). Prefer putting shared namespaces in `GlobalUsings.cs` so the app file needs no local usings.
- `[App]` attribute - Mandatory, must appear exactly once. Marks the class whose `Main()` method will be executed. Do NOT explicitly implement `: IApp<>` — the platform discovers the `[App]` class by reflection and passes its own `IApp` implementation in through the primary constructor
- `IApp<SessionIdentity, ClientParameters> app` - Must specify types for SessionIdentity and ClientParameters. Passed as a primary constructor parameter, not implemented as an interface
- `private UI UI { get; } = new(app, new IkonTheme());` - Initialize UI with app and theme. For brand customization, use the indexer: `new IkonTheme { ["primary"] = "amber-400", ["background"] = "zinc-950" }` — every entry is one CSS-variable override expressed in Crosswind tokens. **Never redefine `IkonTheme` as a class in your app source** — it is provided by `Ikon.Parallax.Theming` and auto-imported via `global using`.
- `Reactive<T>` - Reactive state that triggers UI updates when changed
- `public async Task Main()` - App entry point. Declares UI shape and returns quickly. All work code goes in callbacks

### Partial Class Pattern

For larger apps, split into partial classes by feature area:

```
MyApp.cs          - Main class, reactive state, Main() entry
MyApp.UI.cs       - Render methods
MyApp.Audio.cs    - Audio handlers
MyApp.Video.cs    - Video handlers
MyApp.Chat.cs     - Chat feature
```

All files use `public partial class MyApp` and share the same constructor-injected fields.

### GlobalUsings.cs

Reduce `using` clutter with a GlobalUsings file:

```csharp
global using Ikon.AI.Emergence;
global using Ikon.AI.ImageGeneration;
global using Ikon.AI.Kernel;
global using Ikon.AI.LLM;
global using Ikon.AI.SpeechGeneration;
global using Ikon.AI.SpeechRecognition;
global using Ikon.AI.WebSearching;
global using Ikon.AI.WebScraping;
global using Ikon.App;
global using Ikon.Common.Core.Reactive;
global using Ikon.Common.Core.Scope;
global using Ikon.Common.Core;
global using Ikon.Common;
global using Ikon.Parallax.Components.Standard;
global using Ikon.Parallax.Theming;
global using Ikon.Parallax;
global using Ikon.Server;
```

## SessionIdentity & ClientParameters

### SessionIdentity - Controls App Instance Creation

SessionIdentity properties determine which users share the same app instance:

- All property values are hashed together to calculate sessionId
- If sessionId matches a running app instance, user connects to it; otherwise a new instance is created
- `UserId` is special: cloud fills it in for a client that connects and signs in
- Other properties get values from URL query params (property name = query param key)
- Available in app via `app.SessionIdentity`
- **An identity field can be absent, and its declared nullability decides what you get.** An instance
  started by an HTTP endpoint, a webhook, or a cron tick has no connecting client, so nothing supplies
  `UserId`. The binder honours the annotation you wrote: `string?` → `null`, `string` → `""`, `int?` →
  `null`, `int` → `0`. Nothing ever hands a non-nullable field a null.
- **Prefer `string?` for identity fields.** It is the only spelling that keeps "absent" distinguishable
  from a real value — with plain `string` an absent field and an empty one look identical. Guard with
  `string.IsNullOrEmpty(app.SessionIdentity.UserId)` before using it in `Main()`, in an `[HttpX]`
  handler, or anywhere else a machine-triggered run can reach.

Examples:

- `SessionIdentity(string? UserId)` - each user gets their own app instance
- `SessionIdentity()` (empty) - everyone connects to same app instance
- `SessionIdentity(string Mode)` - `?mode=value1` users share one app instance, `?mode=value2` users share another

### ClientParameters - Per-Client Data

- URL query params auto-mapped if property names and types match
- Each connected client has their own ClientParameters
- Available via `app.Clients[clientId].Parameters`

### URL Format

- `?ikon-session=[sessionIdentityHash]` - joins that exact live session, bypassing SessionIdentity calculation. This is the link `app.GlobalState.SessionUrl` publishes. If no live session has that hash the connect fails — it never falls back to starting a fresh one.
- Query params (`?key=value`) - mapped to SessionIdentity and ClientParameters

### Anti-patterns — DO NOT use

- `IApp<NoSession, NoClient>` / `IApp<None, None>` / `IApp<,>` — sentinel "no session/client" types **do not exist**. Always declare concrete `public record SessionIdentity(...)` and `public record ClientParameters(...)` (use empty `()` if you don't need any fields; both MUST be `public` or you get CS0051).
- Implementing `IApp` as an interface (`class MyApp : IApp<S,C>`) — wrong. The `[App]` attribute only marks the class for discovery; you only declare a primary constructor `(IApp<S,C> app)` parameter and the host's own `IApp` implementation is passed in.

## Common hallucinations the C# compiler will reject

When in doubt, prefer the canonical name. These are the recurring wrong names the compiler has rejected — assume the right answer is the one in the right column.

| Wrong (don't write) | Right (real API) | Notes |
|---|---|---|
| `Theme.Custom(b => b...)` | `new IkonTheme { ["primary"] = "amber-400", ... }` | Fluent builder retired; `IkonTheme` is an indexer-keyed class — every entry sets one CSS variable. |
| `Theming.Custom(...)` | `new IkonTheme { ... }` | Same. |
| `Theming.Apply(...)` | `new IkonTheme { ... }` | Factory retired; the indexer is the only configurable surface. |
| `new IkonTheme { Brand = "...", Background = "..." }` | `new IkonTheme { ["primary"] = "...", ["background"] = "..." }` | No named init properties — every override is an indexer entry. |
| `IApp<NoSession, NoClient>` | `IApp<SessionIdentity, ClientParameters>` with concrete records above | `NoSession` / `NoClient` types do not exist. Always declare `public record SessionIdentity()` / `public record ClientParameters()` — `public` is required (CS0051 otherwise); use empty `()` if you have nothing. |
| `Audio.Speech` (property) | `await Audio.SpeakAsync(text)` | No `Speech` property on `Audio`. `SpeakAsync` generates and plays speech in one call; the manual chain (for custom mixing/config) is `var gen = new SpeechGenerator(model); await foreach (var chunk in gen.GenerateSpeechAsync(cfg)) Audio.SendSpeech(chunk);`. |
| `app.PlayAudioAsync(bytes, mime)` | `ClientFunctions.PlaySoundAsync(bytes, mime)` | Audio routes live on the static `ClientFunctions`, not `IApp`. |
| `Button.Sm` / `Button.Md` / `Button.Lg` (bare size) | `Button.PrimarySm` / `Button.PrimaryMd` / `Button.PrimaryLg` (or another variant) | Bare size constants don't exist on Button — pick a variant + size. `Button.Primary` / `Button.Secondary` / `Button.Ghost` / `Button.Default` do exist as Md-sized aliases. |
| `Layout.Container` | `Layout.Page` | Doesn't exist. |
| `Icon.Size.Sm` | `Icon.Sm` | Flattened — no `.Size` segment. |
| `IView` (parameter type) | `IView` or `UIView` (both work) | `IView` is a global alias for `UIView` (in `GlobalUsings.cs`); there is no separate `IView` interface, but the alias makes `IView` resolve to `UIView`. |
| `pass.User(...)` | `pass.Command = $"..."` | `Command` is a property. |
| `EmergePass<T>.User` method | `pass.Command` / `pass.SystemPrompt` properties | All pass-config is property assignment. |
| `Theme.Builder` | `new IkonTheme { ... }` | Builder pattern was removed entirely. |

If a compiler error names one of these on the right-hand column, the fix is mechanical: replace the wrong name with the right name. Do not call `guide()` to re-confirm — the compiler is authoritative.

## Host Services & Lifecycle

### Lifecycle Events

```csharp
// Use the friendly extension helpers — NOT raw `app.StartingAsync += ...`.
// The raw events take AsyncEventHandler<TEventArgs> (one-arg); subscribing to
// them and typing the arg invents non-existent types like AppStartingEventArgs.
app.OnStarting(async () => { /* app starting */ });
app.OnStopping(async () => { /* app stopping, cleanup */ });
app.OnClientJoined(async ctx =>
{
    // ctx IS the Context: ctx.ClientSessionId (alias of ctx.SessionId), ctx.UserId,
    // ctx.Theme, ctx.Timezone, ctx.ClientType, ctx.InitialPath, ctx.InitialUrl, ctx.ViewportWidth
    var client = app.Clients[ctx.ClientSessionId];
});
app.OnClientLeft(async ctx => { /* cleanup client state */ });

// For a periodic background loop (live clock, polling, game tick), start it
// inside OnStarting and cancel it in OnStopping — there is no app.BackgroundWork
// "start a task" API (BackgroundWork only ref-counts idle-shutdown prevention):
var clockCts = new CancellationTokenSource();
app.OnStarting(async () =>
{
    _ = Task.Run(async () =>
    {
        while (!clockCts.Token.IsCancellationRequested)
        {
            _now.Value = DateTime.Now;
            await Task.Delay(1000, clockCts.Token);
        }
    }, clockCts.Token);
});
app.OnStopping(async () => clockCts.Cancel());
// The loop's CancellationTokenSource is a plain field with a field initializer
// (`private readonly CancellationTokenSource _clockCts = new();`) — do NOT
// declare the loop as a `readonly ClientReactiveEffect`/effect field and assign
// it inside Main(): Main() is a normal method, not a constructor, so assigning a
// `readonly` field there is CS0191/CS8618. A game tick / timer is just the
// Task.Run loop above started from OnStarting (or directly in Main), not a
// readonly effect object.
```

### Navigation

```csharp
// Listen for path changes
app.Navigation.PathChangedAsync += async args =>
{
    var path = args.Path.TrimStart('/');
    _activeTab.Value = path;
};

// Change path programmatically
await app.Navigation.SetPathAsync($"/{tab}");
await app.Navigation.SetPathAsync(args.ClientSessionId, $"/{tab}", replace: true);
```

**Which host the client came in on: `ctx.InitialUrl`.** `InitialPath` is path + query, so it cannot
tell a visitor on a custom customer domain from one on the space's own hostname, and `app.PublicUrl`
is derived from the space rather than the request — it always names the platform subdomain.
`ctx.InitialUrl` is the absolute URL the browser loaded (same `ikon-*` params stripped, so its path
and query match `InitialPath` exactly), which is how a multi-tenant app brands or scopes itself per
domain:

```csharp
app.OnClientJoined(async ctx =>
{
    // Empty for every non-browser client, and client-supplied like InitialPath — treat the host as
    // a hint that selects what to show, and authorize the result server-side as usual.
    if (Uri.TryCreate(ctx.InitialUrl, UriKind.Absolute, out var url))
    {
        _host.SetFor(ctx.ClientSessionId, url.Host);
    }
});
```

### App Data and Public Folders

- **`app/<ProjectName>/Data/` folder**: Files placed here are included in the app bundle. Available at runtime via `app.DataDirectory` for reading from the C# app
- **`frontend-node/public/` folder**: Files placed here are included in the app bundle and served by the frontend. Accessible by the browser (e.g., for images, fonts, static assets)

### Other Host Services

```csharp
app.GlobalState.SpaceId           // Current space ID
app.GlobalState.ServerSessionId   // Id of this Ikon server instance
app.GlobalState.SessionHash       // Hash of session identity params (logical session id)
app.PublicUrl                     // The app's public URL (space access URL)
app.JoinUrl(new { id = gameId })  // PublicUrl + URL-encoded query string from an anonymous object
app.GlobalState.SessionUrl        // Session-specific access URL
app.GlobalState.PrimaryUserId     // Static user ID of session owner
app.GlobalState.FirstUserId       // First human user who joined (dynamically reassigned)
app.GlobalState.GetClientContext(clientSessionId)  // Get client context; null if no such client is connected
app.DataDirectory                 // Path to app's Data directory
app.Databases                     // Database connection info (see Databases section)
app.SessionIdentity               // Current session identity
app.Clients[clientId].Parameters  // Client parameters
app.ReactiveGlobalState.Clients   // Reactive client state
```

### Join URL & QR Code

The session join URL allows other users to connect. Display it as a QR code for easy scanning.

```csharp
// Get the shareable join URL
var joinUrl = app.PublicUrl;
// with query parameters (URL-encoded name=value pairs from an anonymous object):
var inviteUrl = app.JoinUrl(new { id = sessionId });
// or session-specific:
var sessionUrl = app.ReactiveGlobalState.SessionUrl.Value;

// Render as QR code
view.QR(["w-48 h-48"], value: joinUrl);

// Or display as text
view.Text([Text.Body, "text-primary underline"], joinUrl);
```

### BackgroundWork

Prevents the server from idle-shutdown while background processing is active. Returns an `IAsyncDisposable` scope; multiple concurrent scopes are ref-counted.

```csharp
await using var work = await app.BackgroundWork.StartAsync();
await LongRunningTask();
// work.DisposeAsync() signals completion automatically
```

### ClientFunctions

Programmatic client-side actions (no user gesture required):

```csharp
// Every function targets the calling client (resolved via ReactiveScope.ClientId) by default
await ClientFunctions.SetThemeAsync(Theme.Dark);           // persist: true by default; string overload for custom themes
await ClientFunctions.GetMediaDevicesAsync();
await ClientFunctions.StartAudioCaptureAsync(options);     // returns streamId
await ClientFunctions.StartVideoCaptureAsync(source, options); // returns streamId
await ClientFunctions.StopCaptureAsync(streamId);
await ClientFunctions.CaptureImageAsync(options);          // returns ClientImageCapture
await ClientFunctions.KeepScreenAwakeAsync(true);
await ClientFunctions.GetLanguageAsync();
await ClientFunctions.GetTimezoneAsync();
await ClientFunctions.GetUrlAsync();
await ClientFunctions.SetUrlAsync("/path");                // replace: false, preserveQueryParams: false
await ClientFunctions.GetVisibilityAsync();                // ClientVisibility.Visible/Hidden/Unknown
await ClientFunctions.GetBatteryLevelAsync();              // 0-100
await ClientFunctions.GetNetworkTypeAsync();               // connection type
await ClientFunctions.VibrateAsync(200);                   // or a pattern: VibrateAsync(new[] { 100, 50, 100 })
await ClientFunctions.ScrollToAsync(x: 0, y: 0, smooth: true);
await ClientFunctions.PlaySoundAsync(url, volume: 0.8, loop: false);
await ClientFunctions.PlaySoundAsync(data, mimeType, volume: 0.8, loop: false); // from bytes
await ClientFunctions.StopSoundAsync(playbackId);
await ClientFunctions.RequestFullscreenAsync();
await ClientFunctions.ExitFullscreenAsync();
await ClientFunctions.LogoutAsync();

// Pass targetId to address another client session (all functions):
await ClientFunctions.SetThemeAsync(Theme.Dark, targetId: targetId);
```

### Messages

```csharp
app.MessageReceivedAsync += async args => { /* args.Message.Opcode, args.Message.TrackId */ };
await app.SendMessageAsync(ProtocolMessage.Create(app.SessionId, new RequestIdrVideoFrame(),
    trackId: trackId, targetIds: [clientSessionId]));
```

## Common Patterns

### Loading State

```csharp
private readonly Reactive<bool> _isLoading = new(false);

view.Button([Button.PrimaryMd], _isLoading.Value ? "Loading..." : "Submit",
    disabled: _isLoading.Value,
    onClick: async () =>
    {
        _isLoading.Value = true;
        try { await DoWork(); }
        finally { _isLoading.Value = false; }
    });
```

### Conditional Rendering

```csharp
if (_imageData.Value != null)
{
    view.Image(["max-w-full"], data: _imageData.Value, mimeType: _imageMime.Value);
}
```

### Error Handling in Callbacks

```csharp
onClick: async () =>
{
    try { await RiskyOperation(); }
    catch (Exception ex) { Log.Instance.Warning(ex, "Operation failed"); }
}
```
