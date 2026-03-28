# Reactive State

## Reactive State Management

### Basic Reactive Types

```csharp
// Shared across all clients (global state)
private readonly Reactive<int> _count = new(0);

// Per-client state (each connected client sees their own value)
private readonly ClientReactive<string> _theme = new("light");

// Per-user state (shared across a user's multiple client sessions)
// If a user connects from phone and desktop, both clients share the same UserReactive values
private readonly UserReactive<string> _userPref = new("");

// Persisted across app restarts (saved to cloud storage automatically)
private readonly PersistentReactive<int> _totalVisits = new(0);
```

`PersistentReactive<T>` values survive app restarts by persisting to cloud storage. Use for counters, settings, or any state that should not reset on redeployment.

### Scope Requirements

**Never access `ClientReactive` or `UserReactive` values outside `UI.Root()`.** `Main()` runs before any client/user scope exists. All reads and writes of scoped reactive values must happen inside `UI.Root()` or inside event callbacks (onClick, onSubmit, etc.) which run within a scope. For background tasks, use `ReactiveScope.Use()` to enter a scope explicitly.

```csharp
// WRONG — crashes at startup, no user scope active
public async Task Main()
{
    if (_hasJoined.Value) { ... }  // UserReactive — throws InvalidOperationException
    RenderTavern();
}

// CORRECT — branch inside UI.Root() where scopes are active
public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        if (_hasJoined.Value) { RenderTavern(view); }  // OK — inside UI.Root()
        else { RenderEntry(view); }
    });
}
```

### Value Mutation

```csharp
// Simple assignment
_count.Value = 42;

// List mutation (mutate in place and notify)
_items.Value.Add(newItem);
_items.NotifyUpdate();

// Record mutation
_config.Value = _config.Value with { Theme = "dark" };
```

### Complete Example: Shared Messages + Per-Client Input

```csharp
// Shared state — all clients see the same messages
private readonly Reactive<List<string>> _messages = new([]);

// Per-client state — each client has their own input
private readonly ClientReactive<string> _input = new("");

public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        view.Column(["h-screen"], content: view =>
        {
            // All clients see the same messages
            view.ScrollArea(autoScroll: true, autoScrollKey: _messages.Value.Count.ToString(),
                rootStyle: ["flex-1 min-h-0 px-4"], content: view =>
            {
                foreach (var msg in _messages.Value)
                {
                    view.Text([Text.Body, "py-1"], msg);
                }
            });

            // Each client has their own input
            view.Row(["p-4 gap-2 flex-shrink-0"], content: view =>
            {
                view.TextField([Input.Default, "flex-1"],
                    value: _input.Value,
                    onValueChange: async v => _input.Value = v,
                    onSubmit: async _ =>
                    {
                        _messages.Value.Add(_input.Value);
                        _messages.NotifyUpdate(); // Required for in-place list mutation
                    },
                    clearOnSubmit: true);
            });
        });
    });
}
```

### ReactiveScope Context

Inside UI callbacks, access the current client/user context. `host` does not have a `ClientId` property. Always use `ReactiveScope.ClientId` inside UI callbacks.

```csharp
var clientId = ReactiveScope.ClientId;

// Manually set scope (e.g., in background tasks)
using var _ = ReactiveScope.Use(new ClientScope(clientSessionId));
_clientTheme.Value = "dark"; // Now targets the specified client
```
