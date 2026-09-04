# Logging

## Logging

```csharp
Log.Instance.Info("Processing started");
Log.Instance.Debug("Detail info");
```

When logging a caught exception, pass it to `Error`, `Warning`, or `Critical` — the exception's full ToString() (inner-exception chain + stack trace) is appended to the message. Prefer this over interpolating `ex.Message`, which drops those details and can hide the real cause of wrapper exceptions like `DbUpdateException`:

```csharp
Log.Instance.Error(ex, "AI cleanup failed");     // Serilog / Microsoft.Extensions.Logging idiom
Log.Instance.Warning(ex, "Auto-retry failed");
Log.Instance.Critical(ex, "Startup failed");
```

The message-first order (`Log.Instance.Error("AI cleanup failed", ex)`) works too. `Info`/`Debug` take a single string or interpolated string; interpolate the whole exception when needed: `$"... {ex}"`.

### Structured Parameters

An interpolated log message is not just text. Every hole becomes a structured parameter on the
`LogEvent` the handlers receive, named after the expression it came from, and the rendered line is
unchanged. Two wrappers adjust that: `Log.Named` gives a parameter a name of your choosing —
`LogParameter<T>` — and `Log.Sensitive` marks one for redaction, so its value stays out of the
parameter JSON handlers see (`Sensitive<T>`, whose `SensitivityPolicy` currently has the single
`Default` policy). A type that implements `ILogInfo` decides for itself what it contributes as a
parameter value, instead of being serialized whole.

```csharp
public sealed record Invoice(string Id, decimal Total) : ILogInfo
{
    // What a log parameter shows for this type; without it the whole record is serialized.
    public object LogInfo => Id;
}
```

```csharp
public static void LogCharge(Invoice invoice, string cardNumber, double latencyMs)
{
    // An interpolated hole becomes a structured parameter named after the expression.
    Log.Instance.Info($"Charged invoice {invoice}");

    // Named renames the parameter; the rendered text is unchanged, format specifiers included.
    Log.Instance.Debug($"Charge took {Log.Named("LatencyMs", latencyMs):F1} ms");

    // Sensitive keeps the value out of the redacted parameter JSON the log handlers receive.
    Log.Instance.Debug($"Card {Log.Sensitive(cardNumber)} authorized");
}
```

### Scopes

`Log.Instance.UseScope` pushes an `IScopeKey` for the duration of a `using`, and every line logged
inside carries it as a `LogScopeEntry` (a name and an id) on the event. Declare your own key for
whatever the work is about:

```csharp
public readonly struct OrderScope(string orderId) : IScopeKey
{
    public object Id => orderId;
    public string Name => "Order";
}
```

```csharp
public static void LogUnderOrderScope(string orderId, Invoice invoice)
{
    using (Log.Instance.UseScope(new OrderScope(orderId)))
    {
        // Every line inside carries the scope, as a LogScopeEntry on the LogEvent.
        Log.Instance.Info($"Refunded invoice {invoice}");
    }
}
```

Read the ambient ones with `CurrentScopes`, `GetScope<T>()` and `TryGetScope<T>()`. Do NOT push
`UserScope` or `ClientScope` here: those partition reactive state, `UseScope` reaches only the
logging stack, and scoped state would silently resolve to the wrong slot inside the block. Build
error IKON008 catches it; `ReactiveScope.Use(...)` is the one that partitions.

### Events, Usage and Timers

Beside the level-based lines are two other `LogType` kinds. `Log.Instance.Event(name, parameters)`
records a named business event with a structured payload; `Log.Instance.Usage(name, value)` records
a metered quantity, which is how the platform's own model and transfer accounting is reported.
`Log.Instance.BeginTimer(name)` returns a disposable that logs the elapsed time when it falls out of
scope.

```csharp
public static void ReportRun(int itemCount, double megabytesSent)
{
    // A named event with structured parameters, separate from the level-based lines.
    Log.Instance.Event("import_completed", new { ItemCount = itemCount });

    // A metered quantity, accumulated per name rather than printed.
    Log.Instance.Usage("http.sent_megabytes", megabytesSent);
}
```

`Log.Instance.Exception(message)` logs at exception level and returns the message unchanged, so
recording the failure and supplying the throw message is one expression. It creates and rethrows
nothing itself.

```csharp
public static Invoice RequireInvoice(Invoice? invoice, string id)
{
    // Exception logs the message and returns it, so the record and the throw are one expression.
    return invoice ?? throw new UserException(Log.Instance.Exception($"No invoice {id}"));
}
```

### Filters

`LogType` is the kind of a single entry (`Trace`, `Debug`, `Info`, `Warning`, `Error`, `Critical`,
`Event`, `Usage`, `Exception`); `LogFilter` is the cumulative admission set built from them, so
`LogFilter.Info` admits `Info` and everything more severe, plus `Event`, `Usage` and `Exception`.
`Filter` gates what enters the queue at all, while `ConsoleWriterFilter` and `FileWriterFilter` gate
their own writers, so a run can keep a full debug file behind a quiet console.

```csharp
public static void QuietenTheConsole()
{
    // LogFilter is cumulative: Info admits Info and everything more severe, plus Event, Usage
    // and Exception. The writers filter independently of the queue.
    Log.Instance.Filter = LogFilter.Debug;
    Log.Instance.ConsoleWriterFilter = LogFilter.Info;
}
```
