namespace Ikon.App.Patterns.Examples;

// The logging guide's structured-parameter, scope, event and filter examples.

#region example:log-structured-parameters
public sealed record Invoice(string Id, decimal Total) : ILogInfo
{
    // What a log parameter shows for this type; without it the whole record is serialized.
    public object LogInfo => Id;
}
#endregion

#region example:log-scope-key
public readonly struct OrderScope(string orderId) : IScopeKey
{
    public object Id => orderId;
    public string Name => "Order";
}
#endregion

public static class LoggingExamples
{
    #region example:log-named-and-sensitive
    public static void LogCharge(Invoice invoice, string cardNumber, double latencyMs)
    {
        // An interpolated hole becomes a structured parameter named after the expression.
        Log.Instance.Info($"Charged invoice {invoice}");

        // Named renames the parameter; the rendered text is unchanged, format specifiers included.
        Log.Instance.Debug($"Charge took {Log.Named("LatencyMs", latencyMs):F1} ms");

        // Sensitive flags the parameter ("sensitive": true) and keeps the value out of the rendered text.
        Log.Instance.Debug($"Card {Log.Sensitive(cardNumber)} authorized");
    }
    #endregion

    #region example:log-scopes
    public static void LogUnderOrderScope(string orderId, Invoice invoice)
    {
        using (Log.Instance.UseScope(new OrderScope(orderId)))
        {
            // Every line inside carries the scope, as a LogScopeEntry on the LogEvent.
            Log.Instance.Info($"Refunded invoice {invoice}");
        }
    }
    #endregion

    #region example:log-events-and-usage
    public static void ReportRun(int itemCount, double megabytesSent)
    {
        // A named event with structured parameters, separate from the level-based lines.
        Log.Instance.Event("import_completed", new { ItemCount = itemCount });

        // A metered quantity: each call is its own usage record, billed by its name, and kept off the console only by the default ConsoleWriterFilter.
        Log.Instance.Usage("http.sent_megabytes", megabytesSent);
    }
    #endregion

    #region example:log-throw-message
    public static Invoice RequireInvoice(Invoice? invoice, string id)
    {
        // Exception logs the message and returns it, so the record and the throw are one expression.
        return invoice ?? throw new UserException(Log.Instance.Exception($"No invoice {id}"));
    }
    #endregion

    #region example:log-filters
    public static void QuietenTheConsole()
    {
        // LogFilter is cumulative: Info admits Info and everything more severe, plus Event, Usage
        // and Exception. The writers filter independently of the queue.
        Log.Instance.Filter = LogFilter.Debug;
        Log.Instance.ConsoleWriterFilter = LogFilter.Info;
    }
    #endregion
}

internal sealed partial class AgentGuideExamples
{

    private static void DocLogLevels()
    {
        #region example:log-levels
        Log.Instance.Info("Processing started");
        Log.Instance.Debug("Detail info");
        #endregion
    }

    private static void DocLogExceptions(Exception ex)
    {
        #region example:log-exceptions
        Log.Instance.Error(ex, "AI cleanup failed");     // Serilog / Microsoft.Extensions.Logging idiom
        Log.Instance.Warning(ex, "Auto-retry failed");
        Log.Instance.Critical(ex, "Startup failed");
        #endregion
    }
}
