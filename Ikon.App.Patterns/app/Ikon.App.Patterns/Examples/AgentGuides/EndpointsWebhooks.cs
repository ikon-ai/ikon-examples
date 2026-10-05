using System.Net.Sockets;
using System.Net.WebSockets;

namespace Ikon.App.Patterns.Examples;

file sealed class EndpointsExamples
{
    private static bool VerifyStripe(string? signature, string body) => signature != null && body.Length > 0;

    #region example:http-endpoints
    // The JSON body binds to your typed parameter. Missing fields default and unknown fields are
    // ignored, but a body naming no member of the type ({} included), or an empty/null body for a
    // required parameter, returns 400. Bad input returns a 4xx (it never throws a 500).
    [HttpPost("/sum")]
    public HttpResult Sum(SumRequest req) => HttpResult.Ok(new { sum = req.A + req.B });

    // Explicit verb, no body. Return a value (→ JSON), a string (→ text/plain), or an HttpResult.
    [HttpGet("/health")]
    public string Health() => "ok";

    // A third-party webhook is a normal [HttpPost]. It must be Auth = Public: the default (Grant)
    // makes the gateway reject the bare URL with 401 before the handler runs, and a provider like
    // Stripe calls a fixed URL it cannot carry a grant on. Read the signature header + raw body from
    // the injected Ikon.App.HttpRequest and verify it yourself — the signature IS the authorization.
    [HttpPost("/stripe", Auth = EndpointAuth.Public)]
    public async Task<HttpResult> Stripe(Ikon.App.HttpRequest req)
    {
        // TryGetValue, not the indexer: a missing header would throw and turn the 401 into a 500
        if (!req.Headers.TryGetValue("Stripe-Signature", out var signature) || !VerifyStripe(signature, req.Body)) return HttpResult.Unauthorized();
        // ... process req.Body ...
        return HttpResult.Ok();   // return 200 even on a skip to avoid the provider's retry storm
    }

    // An MCP tool, callable by an LLM / agent. Its JSON Schema is reflected from the signature.
    [Mcp(Name = "add_numbers", Description = "Adds two integers")]
    public int AddNumbers(int a, int b) => a + b;
    #endregion
}

#region example:http-request-record
public record SumRequest(int A, int B);
#endregion

file sealed class MintingExamples(IApp<SessionIdentity, ClientParameters> app)
{
    [HttpGet("/doc")]
    public string GetDocument() => "doc";

    [HttpGet("/doc-alias")]
    public string GetDoc() => "doc";

    [HttpPost("/doc")]
    public string UpdateDoc() => "ok";

    [HttpPost("/sum-alias")]
    public string Sum() => "ok";

    public async Task MintAsync()
    {
        #region example:url-minting
        // Pin a resource identity into a signed grant in the URL:
        MintedUrl minted = await app.MintUrlAsync(nameof(GetDocument), new { DocumentId = "doc-42" });
        string url = minted.Url;   // https://{space}.ikonai.app/api/...?ikon-grant=...

        // On an app-class endpoint, omit the identity to pin THIS instance's own identity (the URL
        // routes back here); on a [Cell] endpoint omitting it pins nothing:
        MintedUrl self = await app.MintUrlAsync(nameof(Sum));

        // Batch several endpoints under one identity in a single backend round-trip:
        IReadOnlyDictionary<string, MintedUrl> urls = await app.MintUrlsAsync(
            new[] { nameof(GetDoc), nameof(UpdateDoc) }, new { DocumentId = "doc-42" });
        #endregion

        Log.Instance.Debug($"{url} {self} {urls.Count}");
    }
}

internal sealed partial class AgentGuideExamples
{

    private async Task DocEndpointWebSocketAsync()
    {
        #region example:endpoint-websocket
        var endpoint = new AppEndpointHost(app);

        endpoint.MapWebSocket("/ws", async (ctx, webSocket) =>
        {
            var buffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) { break; }
                await webSocket.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, true, CancellationToken.None);
            }
        });

        await endpoint.StartAsync();
        #endregion
    }

    private static void DocEndpointMapGet(AppEndpointHost endpoint)
    {
        #region example:endpoint-mapget
        // Write the response via ctx.Response.Body (a Stream). NOT ctx.Response.WriteAsync(string)
        // — that ASP.NET Core extension (Microsoft.AspNetCore.Http) is not in scope in a
        // generated app and produces CS1061. Write UTF-8 bytes to the body stream.
        endpoint.MapGet("/stream/{**path}", async ctx =>
        {
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("OK"));
        });
        #endregion
    }

    private async Task DocRawEndpointAsync()
    {
        #region example:raw-endpoint
        await using var endpoint = await app.RequestEndpointAsync(EndpointProtocol.Udp);
        var udp = new UdpClient(endpoint.LocalPort);
        Log.Instance.Info($"Game server listening at udp://{endpoint.PublicHost}:{endpoint.PublicPort}");
        // `await using` above releases the endpoint when it goes out of scope.
        #endregion

        udp.Dispose();
    }

    private void DocEndpointCleanup(IAsyncDisposable endpoint)
    {
        #region example:endpoint-cleanup
        app.OnStopping(async () =>
        {
            await endpoint.DisposeAsync();
        });
        #endregion
    }
}
