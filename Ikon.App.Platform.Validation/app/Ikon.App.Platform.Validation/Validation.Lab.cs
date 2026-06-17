using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Text.Json;
using Ikon.App.Auth;
using Ikon.App.Cells;
// Same reason as Validation.Mcp.cs — Ikon.Common.DescriptionAttribute is Property|Field only,
// the BCL one allows AttributeTargets.All. JsonSchemaGenerator now reads both transparently.
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

public partial class Validation
{
    // The Lab tab self-tests the app's OWN endpoints. In local dev those are served on a
    // self-signed localhost cert that the default validator rejects (the in-app client can't pass
    // `curl -k`). Accept the dev cert for localhost only; every other host validates normally.
    private static readonly HttpClient s_labHttp = new(new HttpClientHandler
    {
        // Dev/self-test client: the app's own endpoints run on a self-signed localhost cert in
        // local dev. Bypass validation for loopback — including when RequestUri is unavailable
        // (pooled HTTP/2 connection reuse passes a null req, which made the old `Host is "localhost"`
        // check fall through and reject the dev cert → "SSL connection could not be established").
        // Every non-loopback host still validates normally.
        ServerCertificateCustomValidationCallback = (req, _, _, errors) =>
            errors == SslPolicyErrors.None
            || req.RequestUri is null or { IsLoopback: true }
            || req.RequestUri.Host is "localhost" or "127.0.0.1",
    })
    {
        // Force HTTP/1.1 so an h2/ALPN negotiation against the local dev listener can't abort the
        // handshake before the cert callback runs.
        DefaultRequestVersion = System.Net.HttpVersion.Version11,
        DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionOrLower,
    };

    private readonly Reactive<string?> _labRestResult = new(null);
    private readonly Reactive<bool> _labRestInvoking = new(false);
    private readonly Reactive<string?> _labMcpResult = new(null);
    private readonly Reactive<bool> _labMcpInvoking = new(false);
    private readonly Reactive<string?> _globalRestResult = new(null);
    private readonly Reactive<bool> _globalRestInvoking = new(false);
    private readonly Reactive<string?> _globalMcpResult = new(null);
    private readonly Reactive<bool> _globalMcpInvoking = new(false);

    // The grant-protected REST surface: the minted URL (with its decoded grant shown in the diagnostics)
    // and the last invocation result.
    private readonly Reactive<string?> _labGrantUrl = new(null);
    private readonly Reactive<string?> _labGrantResult = new(null);
    private readonly Reactive<bool> _labGrantInvoking = new(false);

    // Last error from a substrate-cell SDK call (the IncrementAsync / ResetAsync buttons on the
    // global cell pane). Rendered as a status pill so a failed substrate call surfaces cleanly
    // instead of bubbling up and breaking the UI.
    private readonly Reactive<string?> _globalCellLastError = new(null);

    private async Task InvokeLabRestAsync()
    {
        if (_labRestInvoking.Value)
        {
            return;
        }

        _labRestInvoking.Value = true;
        _labRestResult.Value = null;

        try
        {
            var webhook = app.Endpoints.FirstOrDefault(w => w.FunctionName == "LabCell_IncrementHttp");

            if (webhook is null || string.IsNullOrEmpty(webhook.PublicUrl))
            {
                _labRestResult.Value = "(webhook LabCell_IncrementHttp not registered yet — cloud deployment required)";
                return;
            }

            // The endpoint path carries the Workspace identity as a {workspace} segment, so the emitted
            // PublicUrl is a template — substitute the selected workspace into it. The gateway captures
            // that segment as the cell's identity and reverse-proxies to the cell-host's relay URL.
            var url = webhook.PublicUrl.Replace("{workspace}", Uri.EscapeDataString(_labWorkspace.Value));

            using var response = await s_labHttp.PostAsJsonAsync(url, new LabIncrementRequest(1));
            var body = await response.Content.ReadAsStringAsync();
            _labRestResult.Value = $"{(int)response.StatusCode} {response.StatusCode}\n\n{PrettyOrRaw(body)}";
        }
        catch (Exception ex)
        {
            _labRestResult.Value = $"Error: {ex.Message}";
        }
        finally
        {
            _labRestInvoking.Value = false;
        }
    }

    // Mint a working, grant-bearing URL for the grant-protected endpoint, pinning the selected Workspace
    // (substituted into the {workspace} path segment). Minting round-trips to the backend (which holds the
    // per-space grant secret) and is idempotent for these stable grants, so re-minting returns the same URL.
    private async Task<string?> EnsureLabGrantUrlAsync()
    {
        try
        {
            var minted = await app.MintUrlAsync("LabCell_IncrementSecureHttp", new { Workspace = _labWorkspace.Value });
            _labGrantUrl.Value = minted.Url;
            return minted.Url;
        }
        catch (Exception ex)
        {
            _labGrantResult.Value = $"Mint failed: {ex.Message}";
            return null;
        }
    }

    private async Task InvokeLabGrantRestAsync()
    {
        if (_labGrantInvoking.Value)
        {
            return;
        }

        _labGrantInvoking.Value = true;
        _labGrantResult.Value = null;

        try
        {
            // Mint (or re-mint for the current workspace) the grant URL, then call it. The bare PublicUrl
            // would 401 — possession of the minted ?ikon-grant= is what authorizes this endpoint.
            var url = await EnsureLabGrantUrlAsync();

            if (url is null)
            {
                return;
            }

            using var response = await s_labHttp.PostAsJsonAsync(url, new LabIncrementRequest(1));
            var body = await response.Content.ReadAsStringAsync();
            _labGrantResult.Value = $"{(int)response.StatusCode} {response.StatusCode}\n\n{PrettyOrRaw(body)}";
        }
        catch (Exception ex)
        {
            _labGrantResult.Value = $"Error: {ex.Message}";
        }
        finally
        {
            _labGrantInvoking.Value = false;
        }
    }

    private Task InvokeLabMcpAsync()
        // Workspace identity rides the query (this AppProcess cell resolves it in-process); the tool
        // name is the C# method name, IncrementMcp.
        => InvokeMcpToolHttpAsync("LabCell_mcp", "IncrementMcp", _labWorkspace.Value, _labMcpResult, _labMcpInvoking);

    /// <summary>
    /// POST a JSON-RPC <c>tools/call</c> to a cell's MCP multiplexer endpoint (<c>{Cell}_mcp</c>, served
    /// at the cell's <c>/mcp</c> path). MCP tools are reachable ONLY through this multiplexer — never as
    /// per-tool HTTP POSTs — so the body is a single JSON-RPC request naming the tool. When
    /// <paramref name="workspace"/> is non-null it's appended as the cell's identity query so the call
    /// targets the same keyed instance the REST card uses.
    /// </summary>
    private async Task InvokeMcpToolHttpAsync(string mcpEndpointName, string toolName, string? workspace, Reactive<string?> result, Reactive<bool> invoking)
    {
        if (invoking.Value)
        {
            return;
        }

        invoking.Value = true;
        result.Value = null;

        try
        {
            var endpoint = app.Endpoints.FirstOrDefault(w => w.FunctionName == mcpEndpointName);

            if (endpoint is null || string.IsNullOrEmpty(endpoint.PublicUrl))
            {
                result.Value = $"({mcpEndpointName} not registered yet — cloud deployment required)";
                return;
            }

            var url = endpoint.PublicUrl;

            if (workspace is not null)
            {
                url += url.Contains('?') ? "&" : "?";
                url += $"Workspace={Uri.EscapeDataString(workspace)}";
            }

            var rpc = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new { name = toolName, arguments = new { delta = 1 } },
            };

            using var response = await s_labHttp.PostAsJsonAsync(url, rpc);
            var body = await response.Content.ReadAsStringAsync();
            result.Value = $"{(int)response.StatusCode} {response.StatusCode}\n\n{PrettyOrRaw(body)}";
        }
        catch (Exception ex)
        {
            result.Value = $"Error: {ex.Message}";
        }
        finally
        {
            invoking.Value = false;
        }
    }

    private async Task InvokeGlobalRestAsync()
    {
        if (_globalRestInvoking.Value)
        {
            return;
        }

        _globalRestInvoking.Value = true;
        _globalRestResult.Value = null;

        try
        {
            var webhook = app.Endpoints.FirstOrDefault(w => w.FunctionName == "GlobalLabCell_IncrementHttp");

            if (webhook is null || string.IsNullOrEmpty(webhook.PublicUrl))
            {
                _globalRestResult.Value = "(webhook GlobalLabCell_IncrementHttp not registered yet — cloud deployment required)";
                return;
            }

            using var response = await s_labHttp.PostAsJsonAsync(webhook.PublicUrl, new LabIncrementRequest(1));
            var body = await response.Content.ReadAsStringAsync();
            _globalRestResult.Value = $"{(int)response.StatusCode} {response.StatusCode}\n\n{PrettyOrRaw(body)}";
        }
        catch (Exception ex)
        {
            _globalRestResult.Value = $"Error: {ex.Message}";
        }
        finally
        {
            _globalRestInvoking.Value = false;
        }
    }

    private Task InvokeGlobalMcpAsync()
        // The global cell has no per-call identity, so no Workspace query — the tool name is the [Mcp]
        // Name override, IncrementGlobalMcp.
        => InvokeMcpToolHttpAsync("GlobalLabCell_mcp", "IncrementGlobalMcp", workspace: null, _globalMcpResult, _globalMcpInvoking);

    // Fire-and-forget a substrate-cell SDK command (Increment / Reset on the global cell). The
    // result lands on the Counter mirror via the cell-host subscription, so the UI handler must NOT
    // await the remote hop — awaiting it blocks the handler (and the app message loop while it waits
    // for the response), freezing the UI. We observe the discarded task so a failed hop still
    // surfaces in _globalCellLastError instead of being lost.
    private void FireGlobalCommand(Func<Task> work)
    {
        _globalCellLastError.Value = null;
        _ = ObserveGlobalCommandAsync(work);
    }

    private async Task ObserveGlobalCommandAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            _globalCellLastError.Value = $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string PrettyOrRaw(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return body;
        }
    }

    // ── Cell ─────────────────────────────────────────────────────────────────
    // The whole point of this tab: one [Cell] class, identity-scoped state,
    // exposed through three surfaces that all mutate the same Reactive<T> fields.

    public sealed record LabCellIdentity(string Workspace);
    public sealed record LabIncrementRequest(int Delta);
    public sealed record LabSnapshot(int Counter, string[] History, string Workspace);
    public sealed record LabSignatureEcho(string Signature, string Workspace);
    public sealed record GlobalLabSnapshot(int Counter, string[] History);

    [Cell(IdleTtlSeconds = 600)]
    public sealed class LabCell(ICell<LabCellIdentity> ctx)
    {
        public LabCellIdentity Identity { get; } = ctx.Identity;
        public Reactive<int> Counter { get; } = new(0);
        public Reactive<List<string>> History { get; } = new([]);

        // Internal mutation path — every surface ends up here, so the demo's
        // "same state, different transport" message holds by construction.
        public void Increment(int delta)
        {
            Counter.Value += delta;
            History.Add($"[{DateTime.UtcNow:HH:mm:ss}] +{delta} → {Counter.Value} ({Identity.Workspace})");
        }

        // Surface 1: REST — the Workspace identity comes from the URL PATH
        // (/lab/{workspace}/increment). For this AppProcess cell the gateway forwards the captured
        // segment and the in-process CellHost keys the instance on it. Authorization is the "public"
        // /router/ edge policy (anonymous, gated by anti-abuse).
        [HttpPost("/lab/{workspace}/increment", Auth = EndpointAuth.Public)]
        public HttpResult IncrementHttp(LabIncrementRequest req)
        {
            Increment(req.Delta);
            return HttpResult.Ok(Snapshot());
        }

        // Surface 1b: REST behind a GRANT. Same path-captured Workspace identity, but Auth = Grant means
        // the URL must carry a signed ?ikon-grant= minted by app.MintUrl — possession authorizes the call
        // (and the cold-start). The bare PublicUrl alone 401s; mint a working URL first. Minting pins the
        // Workspace into the grant and substitutes it into the {workspace} segment.
        [HttpPost("/lab/{workspace}/increment-secure", Auth = EndpointAuth.Grant)]
        public HttpResult IncrementSecureHttp(LabIncrementRequest req)
        {
            Increment(req.Delta);
            return HttpResult.Ok(Snapshot());
        }

        // Surface 2: MCP — auto-derived input + output schema from the C# signature. Served through the
        // cell's one JSON-RPC multiplexer (/lab-cell/mcp), not a per-tool POST; the Workspace identity
        // rides the request query just like REST.
        [Mcp(Description = "Increment the Lab counter for the supplied workspace identity")]
        public LabSnapshot IncrementMcp([Description("How much to add")] int delta = 1)
        {
            Increment(delta);
            return Snapshot();
        }

        // The Stripe pattern as a plain REST endpoint: read an untrusted request header inline (here
        // a stand-in signature) — no separate auth cell — while the instance stays keyed by the
        // upstream-resolved Workspace identity. Reading the header can't retarget the call.
        [HttpPost("echo-signature", Auth = EndpointAuth.Public)]
        public HttpResult EchoSignature()
            => HttpResult.Ok(new LabSignatureEcho(HttpCallContext.Current?.Header("X-Demo-Signature") ?? "(none)", Identity.Workspace));

        private LabSnapshot Snapshot() => new(Counter.Value, [.. History.Value], Identity.Workspace);
    }

    /// <summary>
    /// The proxy surface of <see cref="GlobalLabCell"/> — what an app process sees through
    /// <c>Cells.Connect&lt;IGlobalLabCell&gt;()</c>. Substrate routing only engages when a cell is
    /// reached via an interface (DispatchProxy can't proxy a concrete class), so this interface is
    /// the seam: <see cref="Counter"/> / <see cref="History"/> become local mirrors fed by an SDK
    /// subscription, and the methods dispatch over the SDK connection (they're <c>[Function]</c>-
    /// marked on the cell, the SDK opt-in).
    /// </summary>
    public interface IGlobalLabCell
    {
        Reactive<int> Counter { get; }
        Reactive<List<string>> History { get; }
        Task IncrementAsync(int delta);
        Task ResetAsync();
    }

    /// <summary>
    /// Sibling cell with a parameterless SessionIdentity, hosted on the cell-substrate
    /// (<see cref="CellProcessScope.Substrate"/>) — one instance per <c>(CellType, SessionIdentity)</c>
    /// across the whole deployment. Reached through <see cref="IGlobalLabCell"/>, the
    /// <c>SubstrateCellProxy</c> mirrors its <c>Reactive&lt;T&gt;</c> state and dispatches its
    /// <c>[Function]</c> methods over a standard SDK connection to the cell-host.
    /// </summary>
    [Cell(IdleTtlSeconds = 600, ProcessScope = CellProcessScope.Substrate)]
    public sealed class GlobalLabCell(ICell<GlobalLabCell.SessionIdentity> ctx) : IGlobalLabCell
    {
        public record SessionIdentity();  // empty → global, eager-spawned at host init

        private readonly ICell<SessionIdentity> _ctx = ctx;

        public Reactive<int> Counter { get; } = new(0);
        public Reactive<List<string>> History { get; } = new([]);

        // Internal mutation core — every surface (SDK [Function], REST [Rest], MCP [Mcp]) routes
        // through here so they all mutate the same Reactive<T> fields.
        private void IncrementCore(int delta)
        {
            Counter.Value += delta;
            History.Add($"[{DateTime.UtcNow:HH:mm:ss}] +{delta} → {Counter.Value} (global)");
        }

        // SDK surface — [Function] puts these on the function-call wire so the proxy can
        // dispatch them over its SDK connection to the cell-host.
        [Function]
        public Task IncrementAsync(int delta)
        {
            IncrementCore(delta);
            return Task.CompletedTask;
        }

        [Function]
        public Task ResetAsync()
        {
            Counter.Value = 0;
            History.Clear();
            return Task.CompletedTask;
        }

        [HttpPost("increment", Auth = EndpointAuth.Public)]
        public HttpResult IncrementHttp(LabIncrementRequest req)
        {
            IncrementCore(req.Delta);
            return HttpResult.Ok(Snapshot());
        }

        [Mcp(Name = "IncrementGlobalMcp", Description = "Increment the shared global Lab counter (no per-call identity)")]
        public GlobalLabSnapshot IncrementMcp([Description("How much to add")] int delta = 1)
        {
            IncrementCore(delta);
            return Snapshot();
        }

        private GlobalLabSnapshot Snapshot() => new(Counter.Value, [.. History.Value]);
    }

    // ── UI-side selected identity for the Lab tab ────────────────────────────
    private readonly ClientReactive<string> _labWorkspace = new("alpha");
}
