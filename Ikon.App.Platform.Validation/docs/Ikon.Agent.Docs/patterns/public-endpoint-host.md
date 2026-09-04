<!-- mined-from: Ikon.App.Patterns -->
# A Public HTTP Endpoint — And What Happens When The Relay Is Down

`AppEndpointHost` serves HTTP from inside the app and publishes it through the relay tunnel. The
part worth writing down is the degraded path: **a failed relay allocation is non-fatal**. The host
serves on `LocalPort` and retries in the background, and reading `PublicUrl` in that state
**throws**.

So `HasPublicUrl` is guarded, not assumed — and because the retry event fires *only* for the
background allocation, both paths have to set the URL or the app that got its tunnel immediately
never records it.

## When to use

A webhook a third party posts to, a REST API for another system, a WebSocket for a non-Ikon client.
For tools an LLM calls, `endpoint-and-mcp-tool` covers the three different authorizations involved.

## Notes

- **`stablePortName` gives the relay a deterministic port for that name**, so `PublicUrl` survives
  reconnects and restarts. A webhook registered with a third party needs this; an ephemeral port
  gets a new URL each boot.
- **`StartAsync` returns as soon as the host is serving** and keeps running in the background — it
  does not block for the host's lifetime, so there is nothing to fire-and-forget around it.
- `LocalPort` and `PublicUrl` both **throw** when read too early: `LocalPort` before `StartAsync`
  completes, `PublicUrl` before the tunnel is allocated.
- **Do NOT await `WaitForPublicUrlAsync` on the initialization path of an app that renders UI.** It
  blocks first paint on something the app does not need in order to draw. It exists for an app
  whose endpoints are useless without their public URL and which would rather start late than
  start wrong.
- Set `OnRequest` so external traffic marks the instance active — otherwise an endpoint-served app
  with no connected clients can be reaped as idle while it is still working.
- `MapWebSocket`'s socket is closed and disposed by the framework once the handler returns. Do not
  dispose it or use it after that.
- `StopAsync` waits up to five seconds for pending requests rather than cutting them off.

## Snippet

```csharp
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
```

## See also

- `endpoint-and-mcp-tool` — HTTP endpoints, webhooks and agent tools, and what separates them.
