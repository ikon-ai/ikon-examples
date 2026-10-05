namespace Ikon.App.Patterns.Examples;

public static class PlatformEventExamples
{
    public static void RecordExportFailure(string invoiceId, Exception ex)
    {
        #region example:platform-events-own-failure
        Log.Instance.Event("invoice_export_failed", new
        {
            @class = EventFailureClass.Dependency,
            invoiceId,
            errorMessage = ex.Message,
        });
        #endregion
    }
}
