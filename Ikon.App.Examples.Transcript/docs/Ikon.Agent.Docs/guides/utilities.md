# Utilities

## Utilities

### Retrying a Transient Failure

`Retrier` wraps a call in bounded retries with exponential backoff. Do not hand-roll a retry loop —
the defaults here are the ones the platform's own network calls use.

```csharp
public static async Task<string> FetchAsync(HttpClient http, string url, CancellationToken ct)
{
    return await Retrier.RunAsync(
        async token => await http.GetStringAsync(url, token),
        ct,
        retries: 3,
        onRetry: async ex =>
        {
            Log.Instance.Warning($"Fetch of {url} failed, retrying", ex);
            await Task.CompletedTask;
        },
        description: $"fetch {url}");
}
```

`retries` counts attempts **beyond** the first, so `retries: 5` allows up to six calls. With no
`retryableExceptions` filter only genuinely transient failures are retried — `IOException`,
`HttpRequestException` and `TimeoutException` — and everything else surfaces immediately rather than
being attempted six times; pass `[typeof(Exception)]` to retry anything. `onRetry` and `onFailure`
are where the log line goes, and `description` names the operation in the built-in ones.

Two per-exception dials tune the ladder without changing the budget. `maxDelay` caps the backoff for
a failure whose next attempt is an independent draw rather than a wait for something to recover;
`maxRetries` lowers the attempt count for a failure where more attempts buy nothing however long you
wait. Neither can raise the budget above `retries`.

`Run`/`RunAsync` come in `Action`, `Func<T>`, `Func<Task>`, `Func<Task<T>>` and
`Func<CancellationToken, Task<T>>` shapes.

### Embedded Resources

`Resources.Instance` reads a file embedded in the app assembly — `ReadAsStringAsync`,
`ReadAsBytesAsync` and `ReadAsStreamAsync`, each taking the resource path.

### Fuzzy Matching

`StringDistance.Levenshtein(a, b)` is the edit distance between two strings: the fewest
single-character insertions, deletions or substitutions that turn one into the other. A null or
empty side returns the other's length. It is O(|a|·|b|) in both time and memory, so it is for "did
you mean" over a short candidate list, not for scanning a corpus.

### Bridging to Microsoft.Extensions.Logging

A library that logs through `ILogger` can be pointed at the Ikon log by registering
`IkonLoggerProvider`, so its output lands under the same filters, redaction and handlers as
everything else instead of a second sink. `AsyncLocalInstances` is the machinery underneath the
`AsyncLocalInstance<T>` singletons (`Log.Instance`, `Resources.Instance`) — it captures and restores
a set of them per owner, which is how several servers run in one process without sharing state.
Hosting code does that; app code reads the instances.
