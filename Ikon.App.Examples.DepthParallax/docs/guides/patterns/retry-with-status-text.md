<!-- mined-from: NeonArcade -->
# Retry With Status Text — User-Visible AI Backoff

Wraps an LLM call in a retry loop with explicit per-attempt delays (2s, 5s, 10s, 15s, 20s) and updates a `Reactive<string>` status field on each retry so the user sees `CREATING PLAN (RETRY 2/4)...` rather than a hung spinner. Only retries `RetryableAIException`; other exceptions surface immediately. Final failure logs and rethrows.

## When to use

Any user-blocking AI operation that occasionally hits provider 429s/503s. Silent retries hide useful information ("is it stuck or is the provider flaky?"); silent failures lose the user. Surfacing the retry count is the cheapest UX improvement.

## Snippet

```csharp
using Ikon.AI;

private const int MaxRetries = 5;
private static readonly TimeSpan[] RetryDelays =
[
    TimeSpan.FromSeconds(2),
    TimeSpan.FromSeconds(5),
    TimeSpan.FromSeconds(10),
    TimeSpan.FromSeconds(15),
    TimeSpan.FromSeconds(20),
];

private async Task<T?> RunWithRetryAsync<T>(string operationName, Func<Task<T>> operation)
    where T : class
{
    for (int attempt = 0; attempt < MaxRetries; attempt++)
    {
        try
        {
            if (attempt > 0)
            {
                _statusText.Value = $"{operationName} (RETRY {attempt}/{MaxRetries - 1})...";
                await Task.Delay(RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)]);
            }
            return await operation();
        }
        catch (RetryableAIException ex)
        {
            Log.Instance.Warning($"{operationName} attempt {attempt + 1} failed: {ex.Message}");
            if (attempt >= MaxRetries - 1)
            {
                Log.Instance.Error($"{operationName} failed after {MaxRetries} attempts: {ex}");
                throw;
            }
        }
    }
    return null;
}

// Caller:
var result = await RunWithRetryAsync("CREATING PLAN", async () =>
{
    var res = await Emerge.Run<GamePlanResponse>(model, ctx, pass =>
    {
        pass.SystemPrompt = PlanSystemPrompt;
        pass.Command = prompt;
        pass.Timeout = TimeSpan.FromMinutes(3);
    });
    return res;
});
```

## Notes

- Hand-tuned delay array beats exponential backoff for short user-blocking calls — the 2s first retry catches transient 429s without making the user wait.
- Catch the specific `RetryableAIException`, not all exceptions. A schema-mismatch error should fail loudly, not retry into the same wall.
- Always update `_statusText.Value` *before* the delay so the UI reflects "we're waiting" instead of looking frozen.
- Log every attempt at warning level — you'll want this on incidents to tell "model is flaky" from "we're hitting a real bug".

## See also

- `busy-flag-loading`
- `streaming-agent-status`
