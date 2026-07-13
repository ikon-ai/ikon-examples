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
