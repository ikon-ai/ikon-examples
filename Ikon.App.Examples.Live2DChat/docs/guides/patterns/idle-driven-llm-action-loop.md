<!-- mined-from: Ikon.App.Examples.VRMChat -->
# Idle-Driven LLM Action Loop — Keep The Character Alive

A background `while` loop wakes every few seconds, checks whether the user has been quiet long enough and whether higher-priority states (speaking/listening/thinking) are off, then asks a cheap LLM ("nano" tier) to pick a fresh idle action. The result drives the same reactive motion/expression fields the chat reply uses.

## When to use

Any always-on agent presence — VRM character, tamagotchi-style pet, ambient companion — that should look alive between turns instead of statue-frozen. The cheap nano model keeps cost negligible even at 12-second cadence.

## Snippet

```csharp
private DateTime _lastInteractionTime = DateTime.UtcNow;
private const int IdleCheckIntervalSeconds = 5;
private const int IdleThresholdSeconds = 15;

public class IdleAction
{
    public string Motion { get; set; } = "idle";
    public string Expression { get; set; } = "neutral";
}

private async Task RunIdleLoopAsync()
{
    var ct = _appCts.Token;

    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(IdleCheckIntervalSeconds), ct);

        var secondsIdle = (DateTime.UtcNow - _lastInteractionTime).TotalSeconds;
        if (secondsIdle < IdleThresholdSeconds) continue;
        if (_isProcessingMessage.Value || _ttsSpeaking.Value || _isListening.Value) continue;

        var currentMotion = _currentMotion.Value;
        var idleAction = await Emerge.Run<IdleAction>(
            LLMModel.Gpt5Nano,
            pass =>
            {
                pass.SystemPrompt = "You control a 3D virtual character's idle behavior. "
                    + "Pick a motion and expression for the character to do while idle.\n"
                    + "Available motions: idle, thinking, shy, confident, stretching, looking_around\n"
                    + "Available expressions: happy, relaxed, surprised, neutral\n"
                    + "Current motion: " + currentMotion + "\n"
                    + "Pick something different from the current motion to keep the character lively.";
                pass.Command = "Choose an idle action";
                pass.Temperature = 0.9f;
                pass.MaxOutputTokens = 100;
            },
            ct).ResultAsync(ct);

        if (_isProcessingMessage.Value || _ttsSpeaking.Value || _isListening.Value) continue;

        _currentMotion.Value = idleAction.Motion;
        _currentExpression.Value = idleAction.Expression;

        await Task.Delay(TimeSpan.FromSeconds(12), ct);
    }
}

// kick off in Main():
RunIdleLoopAsync().RunParallel();
```

## Notes

- Re-check `_isProcessingMessage`/`_ttsSpeaking`/`_isListening` *after* the LLM call too — the user may have started talking while the nano model was thinking.
- Pass current motion in the system prompt so the model picks something *different*; high temperature (0.9) gives variety.
- Wire up `app.StoppingAsync` to cancel `_appCts` so the loop dies when the app stops.
- 12-second post-action delay lets each chosen pose actually play before the next one is picked.

## See also

- `vrm-avatar-expression-motion`
- `voice-loop`
- `streaming-agent-status`
