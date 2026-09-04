namespace Ikon.App.Patterns.Patterns;

// Pattern: public-endpoint-host — see docs/patterns/public-endpoint-host.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class PublicEndpointHost : IPatternDemo
{
    public string Slug => "public-endpoint-host";
    public string Title => "A public HTTP endpoint over the relay";
    public string Category => "Platform mechanics";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Server-side pattern with no UI: an AppEndpointHost serving a webhook over the relay "
        + "tunnel, degrading to the local port when the relay is unreachable. See the source and "
        + "docs/patterns/public-endpoint-host.md.");

    private IAppBase App => throw new NotImplementedException();
    private Task HandleWebhookAsync(string body) => throw new NotImplementedException();

    #region docsnippet:pattern-public-endpoint-host
    private readonly Reactive<string?> _publicUrl = new(null);
    private AppEndpointHost? _host;

    private async Task StartEndpointAsync()
    {
        // stablePortName gives the relay a deterministic port for this name, so PublicUrl survives
        // reconnects and restarts -- which is what a webhook registered with a third party needs.
        _host = new AppEndpointHost(App, secure: true, stablePortName: "payments-webhook");

        _host.MapPost("/hooks/payment", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            await HandleWebhookAsync(await reader.ReadToEndAsync());
            context.Response.StatusCode = 204;
        });

        // Marks external traffic so an endpoint-served instance is not reaped as idle while it is
        // still doing work.
        _host.OnRequest = () => Log.Instance.Debug("endpoint hit");

        // StartAsync returns as soon as the host is SERVING and keeps running in the background;
        // it does not block for the host's lifetime. A failed relay allocation is non-fatal.
        await _host.StartAsync();

        // HasPublicUrl is false when the relay was unreachable -- the host then serves on
        // LocalPort only and retries in the background. Reading PublicUrl in that state throws,
        // so it is guarded rather than assumed.
        if (_host.HasPublicUrl)
        {
            _publicUrl.Value = _host.PublicUrl;
        }
        else
        {
            // The retry event fires only for the background allocation, not when StartAsync
            // already got the tunnel -- so both paths have to set the URL.
            _host.PublicUrlAvailable += url => _publicUrl.Value = url;
        }
    }

    private async Task StopEndpointAsync()
    {
        if (_host is { } host)
        {
            // Waits up to five seconds for pending requests rather than cutting them off.
            await host.StopAsync();
            await host.DisposeAsync();
            _host = null;
        }
    }
    #endregion
}
