using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class ProcountorConnectorGuideExamples
{
    public async Task ProcountorAsync(string clientId, string clientSecret, string redirectUri, string apiKey)
    {
        #region example:connectors-procountor
        var procountor = new Procountor(clientId, clientSecret, redirectUri, apiKey);

        var customers = await procountor.ListCustomersAsync();
        var invoices = await procountor.ListSalesInvoicesAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        foreach (var byStatus in invoices.GroupBy(invoice => invoice.Status))
        {
            Log.Instance.Info($"{byStatus.Key}: {byStatus.Count()} invoices, {byStatus.Sum(invoice => invoice.Total ?? 0)} in accounting currency");
        }

        var payments = await procountor.ListPaymentEventsAsync(invoices[0].Id);
        #endregion

        Log.Instance.Debug($"{customers.Count} {payments.Count}");
    }
}
