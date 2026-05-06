<!-- mined-from: AB2.BirdCard -->
# Playful Loading Text Rotator — In-Character Status Strings

While an LLM call is in flight, rotate through a themed list of fun status strings every 250ms ("Analyzing feathers...", "Calibrating rage...", "Loading slingshot..."). Lifecycle is a `CancellationTokenSource` stored on the session — start it before the work, cancel it in the `finally`. The text is a `Reactive<string>` the UI just reads.

## When to use

Whenever a long-ish AI call (3-30s) blocks user progress. Generic spinners feel dead; in-character labels make the wait feel intentional and on-brand. Especially for game/character apps but works in any product with a strong tone.

## Snippet

```csharp
private static readonly string[] ProcessingTexts =
[
    "Analyzing feathers...",
    "Slingshotting...",
    "Birdifying...",
    "Coloring plumage...",
    "Hatching ideas...",
    "Ruffling feathers...",
    "Cracking eggs...",
    "Calibrating rage...",
    "Loading slingshot...",
    "Checking nest...",
];

private static void StartProcessingTextCycle(SessionState session)
{
    StopProcessingTextCycle(session);
    var cts = new CancellationTokenSource();
    session.ProcessingTextCts = cts;
    var rng = new Random();

    _ = Task.Run(async () =>
    {
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                session.ProcessingText.Value = ProcessingTexts[rng.Next(ProcessingTexts.Length)];
                await Task.Delay(250, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
    }, cts.Token);
}

private static void StopProcessingTextCycle(SessionState session)
{
    session.ProcessingTextCts?.Cancel();
    session.ProcessingTextCts = null;
}

// Caller — wrap each AI turn:
session.IsProcessing.Value = true;
StartProcessingTextCycle(session);
try { await DoTheWorkAsync(); }
finally { StopProcessingTextCycle(session); session.IsProcessing.Value = false; }

// In the render:
view.Text(["text-amber-400 italic"], session.ProcessingText.Value);
```

## Notes

- 250ms is fast enough to feel alive, slow enough to be readable. Below 150ms reads as flicker.
- Random pick (not round-robin) so two consecutive identical labels can happen — feels more natural than a deterministic rotation.
- Always cancel in a `finally` block. A leaked `CancellationTokenSource` keeps the loop running and burns CPU on a dead session.
- Pair with a separate `IsProcessing` boolean for the spinner/disable state — the rotator is decoration, not the source of truth.

## See also

- `busy-flag-loading`
- `streaming-agent-status`
