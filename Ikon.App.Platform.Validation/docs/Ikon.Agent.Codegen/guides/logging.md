# Logging

## Logging

```csharp
Log.Instance.Info("Processing started");
Log.Instance.Debug("Detail info");
Log.Instance.Warning($"Something unexpected: {ex.Message}");
Log.Instance.Error($"Failed: {ex.Message}");
```

For exceptions, use either of the conventional .NET logger shapes — both work and append the exception's full ToString() (with stack trace) to the log:

```csharp
Log.Instance.Error("AI cleanup failed", ex);   // Serilog / message-first
Log.Instance.Error(ex, "AI cleanup failed");   // Microsoft.Extensions.Logging idiom
```

Or interpolate `ex` into the message string for the single-arg form. The non-exception overloads (`Info`/`Debug`/`Warning`/`Error`/`Critical`) all take a single string or interpolated string.
