namespace Ikon.App.Platform.Validation.Docs;

// The logging guide's structured-parameter, scope, event and filter examples.

#region docsnippet:log-structured-parameters
public sealed record Invoice(string Id, decimal Total) : ILogInfo
{
    // What a log parameter shows for this type; without it the whole record is serialized.
    public object LogInfo => Id;
}
#endregion

#region docsnippet:log-scope-key
public readonly struct OrderScope(string orderId) : IScopeKey
{
    public object Id => orderId;
    public string Name => "Order";
}
#endregion

public static class LoggingDocs
{
    #region docsnippet:log-named-and-sensitive
    public static void LogCharge(Invoice invoice, string cardNumber, double latencyMs)
    {
        // An interpolated hole becomes a structured parameter named after the expression.
        Log.Instance.Info($"Charged invoice {invoice}");

        // Named renames the parameter; the rendered text is unchanged, format specifiers included.
        Log.Instance.Debug($"Charge took {Log.Named("LatencyMs", latencyMs):F1} ms");

        // Sensitive keeps the value out of the redacted parameter JSON the log handlers receive.
        Log.Instance.Debug($"Card {Log.Sensitive(cardNumber)} authorized");
    }
    #endregion

    #region docsnippet:log-scopes
    public static void LogUnderOrderScope(string orderId, Invoice invoice)
    {
        using (Log.Instance.UseScope(new OrderScope(orderId)))
        {
            // Every line inside carries the scope, as a LogScopeEntry on the LogEvent.
            Log.Instance.Info($"Refunded invoice {invoice}");
        }
    }
    #endregion

    #region docsnippet:log-events-and-usage
    public static void ReportRun(int itemCount, double megabytesSent)
    {
        // A named event with structured parameters, separate from the level-based lines.
        Log.Instance.Event("import_completed", new { ItemCount = itemCount });

        // A metered quantity, accumulated per name rather than printed.
        Log.Instance.Usage("http.sent_megabytes", megabytesSent);
    }
    #endregion

    #region docsnippet:log-throw-message
    public static Invoice RequireInvoice(Invoice? invoice, string id)
    {
        // Exception logs the message and returns it, so the record and the throw are one expression.
        return invoice ?? throw new UserException(Log.Instance.Exception($"No invoice {id}"));
    }
    #endregion

    #region docsnippet:log-filters
    public static void QuietenTheConsole()
    {
        // LogFilter is cumulative: Info admits Info and everything more severe, plus Event, Usage
        // and Exception. The writers filter independently of the queue.
        Log.Instance.Filter = LogFilter.Debug;
        Log.Instance.ConsoleWriterFilter = LogFilter.Info;
    }
    #endregion
}

public static class RetrierDocs
{
    #region docsnippet:retrier-basic
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
    #endregion
}

public static class PlatformEventDocs
{
    public static void RecordExportFailure(string invoiceId, Exception ex)
    {
        #region docsnippet:platform-events-own-failure
        Log.Instance.Event("invoice_export_failed", new
        {
            @class = EventFailureClass.Dependency,
            invoiceId,
            errorMessage = ex.Message,
        });
        #endregion
    }
}
