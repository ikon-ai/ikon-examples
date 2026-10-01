using System.Net.Http.Json;
using System.Text.Json;
using Ikon.App.Cells;
// Ikon.Common.DescriptionAttribute is Property|Field only; the BCL one allows AttributeTargets.All,
// which parameters need.
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

public partial class Validation
{
    // ── Reaching the cells over HTTP ─────────────────────────────────────────
    // Each check calls a cell endpoint the way an outside client would. Calls go to a URL minted for
    // that endpoint: the grant pins the cell identity (the workspace) and, on a local run, the run's
    // instance id, so a public endpoint is addressed as surely as a grant one. The refusal check uses
    // the bare URL, which the edge refuses before routing anywhere.
    //
    // Answers are compared with each other, never with this process's own cell: in the cloud a cell
    // endpoint is served by its own cell-host, not by the app instance rendering this tab.

    private sealed record CellCheck(string Key, string Label, Func<Validation, Task<string>> Run);

    private static readonly CellCheck[] CellChecks =
    [
        new("rest", "REST (public)", v => v.CheckLabRestAsync()),
        new("grant-refused", "REST (grant) without the grant", v => v.CheckLabGrantRefusedAsync()),
        new("grant", "REST (grant) with the grant", v => v.CheckLabGrantAsync()),
        new("mcp", "MCP IncrementMcp", v => v.CheckLabMcpAsync()),
        new("pinned", "?workspace= cannot retarget the grant", v => v.CheckLabPinnedAsync()),
        new("header", "Request header reaches the cell", v => v.CheckLabHeaderAsync()),
        new("global-rest", "Global cell REST", v => v.CheckGlobalRestAsync()),
        new("global-mcp", "Global cell MCP", v => v.CheckGlobalMcpAsync()),
        new("global-proxy", "Global cell through its interface", v => v.CheckGlobalProxyAsync()),
    ];

    private readonly ClientReactiveDictionary<string, string> _cellCheckResults = new();
    private readonly ClientReactive<bool> _cellChecking = new(false);
    private readonly ClientReactive<string> _labWorkspace = new("alpha");

    // Last error from each card's cell command (its +1 / Reset buttons), kept apart so a failing cell
    // is not blamed on the other one.
    private readonly ClientReactive<string?> _labCellError = new(null);
    private readonly ClientReactive<string?> _globalCellError = new(null);

    private async Task RunCellChecksAsync(IEnumerable<CellCheck> checks)
    {
        _cellChecking.Value = true;

        try
        {
            foreach (var check in checks)
            {
                _cellCheckResults[check.Key] = "Running…";

                try
                {
                    _cellCheckResults[check.Key] = await check.Run(this);
                }
                catch (Exception ex)
                {
                    _cellCheckResults[check.Key] = $"FAIL {ex.Message}";
                }
            }
        }
        finally
        {
            _cellChecking.Value = false;
        }
    }

    // Through the interface, as for any cell: in the cloud that reaches the cell-host serving the
    // workspace, the same instance the HTTP checks move; a local run hosts it in-process.
    private static ILabCell ConnectLabCell(string workspace) => Cells.Instance.Connect<ILabCell>(new LabCellIdentity(workspace));

    // Which process this code runs in, so each view and each answer can say where a cell lives.
    private static readonly string ProcessLabel = $"{Environment.MachineName}:{Environment.ProcessId}";

    private async Task<string> MintLabUrlAsync(string endpoint, string? workspace)
        => (await app.MintUrlAsync(endpoint, workspace is null ? null : new { Workspace = workspace })).Url;

    private static async Task<(int Status, string Body)> PostCellAsync(string url, object payload, (string Name, string Value)? header = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");

        if (header is { } h)
        {
            request.Headers.Add(h.Name, h.Value);
        }

        using var response = await _mcpHttp.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // An MCP multiplexer may answer as an event stream; the response is its last data line.
        if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
        {
            body = body.Split('\n').Where(line => line.StartsWith("data:", StringComparison.Ordinal)).Select(line => line["data:".Length..].Trim()).LastOrDefault() ?? "";
        }

        return ((int)response.StatusCode, body);
    }

    private static (int Counter, string Workspace, string Process) LabSnapshotIn(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return (Property(root, "Counter").GetInt32(), Property(root, "Workspace").GetString() ?? "", Property(root, "Process").GetString() ?? "");
    }

    private static (int Counter, string Process) GlobalSnapshotIn(string json)
    {
        using var document = JsonDocument.Parse(json);
        return (Property(document.RootElement, "Counter").GetInt32(), Property(document.RootElement, "Process").GetString() ?? "");
    }

    // Endpoint answers and tool answers serialize the same record with different casing.
    private static JsonElement Property(JsonElement element, string name)
        => element.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    // An MCP tools/call answers with the tool's record as JSON text inside the result.
    private static string McpResultText(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    private static (int Counter, string Process) GlobalSnapshotInMcpResult(string json) => GlobalSnapshotIn(McpResultText(json));

    private static object IncrementCall(string tool) => new { jsonrpc = "2.0", id = 1, method = "tools/call", @params = new { name = tool, arguments = new { delta = 1 } } };

    // Two calls, each answered by the pinned workspace, the second one higher. Anyone else's increment
    // in between only makes it higher.
    private static async Task<string> ExpectLabIncrementAsync(string workspace, Func<Task<(int Status, string Body)>> call, Func<string, (int Counter, string Workspace, string Process)> snapshotOf)
    {
        var answers = new List<(int Counter, string Workspace, string Process)>();

        for (var i = 0; i < 2; i++)
        {
            var (status, body) = await call();

            if (status != 200)
            {
                return $"FAIL HTTP {status}: {Truncate(body, 200)}";
            }

            answers.Add(snapshotOf(body));
        }

        if (answers.Any(a => a.Workspace != workspace))
        {
            return $"FAIL answered by workspace \"{answers.First(a => a.Workspace != workspace).Workspace}\", expected {workspace}";
        }

        return answers[1].Counter > answers[0].Counter
            ? $"PASS {workspace}: {answers[0].Counter} → {answers[1].Counter} @ {answers[1].Process}"
            : $"FAIL {workspace}: {answers[0].Counter} → {answers[1].Counter} @ {answers[1].Process}";
    }

    private Task<string> CheckLabRestAsync()
    {
        var workspace = _labWorkspace.Value;
        return ExpectLabIncrementAsync(workspace,
            async () => await PostCellAsync(await MintLabUrlAsync("LabCell_IncrementHttp", workspace), new LabIncrementRequest(1)),
            LabSnapshotIn);
    }

    private async Task<string> CheckLabGrantRefusedAsync()
    {
        var endpoint = app.Endpoints.FirstOrDefault(e => e.FunctionName == "LabCell_IncrementSecureHttp")
            ?? throw new InvalidOperationException("LabCell_IncrementSecureHttp is not registered");
        var url = endpoint.PublicUrl.Replace("{workspace}", Uri.EscapeDataString(_labWorkspace.Value));
        var (status, body) = await PostCellAsync(url, new LabIncrementRequest(1));

        return status == 401 ? "PASS refused: 401" : $"FAIL expected HTTP 401, got {status}: {Truncate(body, 200)}";
    }

    private Task<string> CheckLabGrantAsync()
    {
        var workspace = _labWorkspace.Value;
        return ExpectLabIncrementAsync(workspace,
            async () => await PostCellAsync(await MintLabUrlAsync("LabCell_IncrementSecureHttp", workspace), new LabIncrementRequest(1)),
            LabSnapshotIn);
    }

    private Task<string> CheckLabMcpAsync()
    {
        var workspace = _labWorkspace.Value;
        return ExpectLabIncrementAsync(workspace,
            async () => await PostCellAsync(await MintLabUrlAsync("LabCell_mcp", workspace), IncrementCall("IncrementMcp")),
            body => LabSnapshotIn(McpResultText(body)));
    }

    // The grant pins the workspace; an open query value naming another one must not move the call.
    private async Task<string> CheckLabPinnedAsync()
    {
        var workspace = _labWorkspace.Value;
        var other = workspace == "beta" ? "gamma" : "beta";
        var url = await MintLabUrlAsync("LabCell_EchoSignature", workspace) + $"&workspace={other}";
        var (status, body) = await PostCellAsync(url, new { });

        if (status != 200)
        {
            return $"FAIL HTTP {status}: {Truncate(body, 200)}";
        }

        using var document = JsonDocument.Parse(body);
        var answered = Property(document.RootElement, "Workspace").GetString();

        return answered == workspace
            ? $"PASS answered by {workspace} despite ?workspace={other}"
            : $"FAIL answered by {answered}, expected {workspace}";
    }

    private async Task<string> CheckLabHeaderAsync()
    {
        var workspace = _labWorkspace.Value;
        var signature = $"sig-{Guid.NewGuid():N}"[..12];
        var (status, body) = await PostCellAsync(await MintLabUrlAsync("LabCell_EchoSignature", workspace), new { }, ("X-Demo-Signature", signature));

        if (status != 200)
        {
            return $"FAIL HTTP {status}: {Truncate(body, 200)}";
        }

        using var document = JsonDocument.Parse(body);
        var echoed = Property(document.RootElement, "Signature").GetString();
        var echoedWorkspace = Property(document.RootElement, "Workspace").GetString();

        return echoed == signature && echoedWorkspace == workspace
            ? $"PASS {signature} in {workspace}"
            : $"FAIL got signature {echoed} in {echoedWorkspace}, expected {signature} in {workspace}";
    }

    // The global cell answers every caller from one instance, so each check calls twice and expects
    // the second answer one higher: someone else's increment in between would only make it higher.
    private async Task<string> ExpectGlobalIncrementAsync(Func<Task<(int Status, string Body)>> call, Func<string, (int Counter, string Process)> snapshotOf)
    {
        var first = await call();

        if (first.Status != 200)
        {
            return $"FAIL HTTP {first.Status}: {Truncate(first.Body, 200)}";
        }

        var second = await call();

        if (second.Status != 200)
        {
            return $"FAIL HTTP {second.Status}: {Truncate(second.Body, 200)}";
        }

        var (a, b) = (snapshotOf(first.Body), snapshotOf(second.Body));
        return b.Counter > a.Counter ? $"PASS {a.Counter} → {b.Counter} @ {b.Process}" : $"FAIL {a.Counter} → {b.Counter} @ {b.Process}";
    }

    private async Task<string> CheckGlobalRestAsync()
    {
        var url = await MintLabUrlAsync("GlobalLabCell_IncrementHttp", null);
        return await ExpectGlobalIncrementAsync(() => PostCellAsync(url, new LabIncrementRequest(1)), GlobalSnapshotIn);
    }

    private async Task<string> CheckGlobalMcpAsync()
    {
        var url = await MintLabUrlAsync("GlobalLabCell_mcp", null);
        return await ExpectGlobalIncrementAsync(() => PostCellAsync(url, IncrementCall("IncrementGlobalMcp")), GlobalSnapshotInMcpResult);
    }

    // Through the interface the app holds: the call goes over the SDK connection to the cell-host,
    // and the Counter mirror moves only when the cell-host pushes the new value back.
    private async Task<string> CheckGlobalProxyAsync()
    {
        var cell = Cells.Instance.Connect<IGlobalLabCell>(new GlobalLabCell.SessionIdentity());
        var before = cell.Counter.Value;

        await cell.IncrementAsync(1);

        for (var waited = 0; waited < 50 && cell.Counter.Value == before; waited++)
        {
            await Task.Delay(100);
        }

        return cell.Counter.Value > before
            ? $"PASS mirror {before} → {cell.Counter.Value} @ {cell.Process.Value}"
            : $"FAIL the mirror stayed at {before} for 5s";
    }

    // Fire-and-forget a cell command from a button: the result lands on the Counter mirror via
    // the cell-host subscription, so the handler must not await the remote hop — awaiting it blocks the
    // handler (and the app message loop while it waits), freezing the UI. The discarded task is
    // observed so a failed hop still surfaces in the card's error line.
    private void FireCellCommand(ClientReactive<string?> error, Func<Task> work)
    {
        error.Value = null;
        _ = ObserveCellCommandAsync(error, ReactiveScope.ClientId, work);
    }

    private static async Task ObserveCellCommandAsync(ClientReactive<string?> error, int clientSessionId, Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            error.SetFor(clientSessionId, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void RenderCellsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Cells");
            view.Text([Text.Body],
                $"This app: {ProcessLabel} · {(app.GlobalState.ServerRunType == ServerRunType.Local ? "local run" : "cloud")}",
                props: TestId("cell-app-process"));

            view.Box(["grid gap-6 md:grid-cols-2"], content: view =>
            {
                RenderKeyedCellCard(view);
                RenderGlobalCellCard(view);
            });

            RenderCellChecksCard(view);
        });
    }

    private void RenderKeyedCellCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-3"], content: view =>
            {
                view.Text([Text.H3, "flex-1"], "LabCell");
                view.Select(
                    value: _labWorkspace.Value,
                    options: [new SelectOption("alpha", "alpha"), new SelectOption("beta", "beta"), new SelectOption("gamma", "gamma")],
                    ariaLabel: "Workspace",
                    props: TestId("cell-workspace"),
                    onValueChange: async v =>
                    {
                        _labWorkspace.Value = v ?? "alpha";
                        _labCellError.Value = null;
                    });
            });

            ILabCell cell;

            try
            {
                cell = ConnectLabCell(_labWorkspace.Value);
            }
            catch (Exception ex)
            {
                view.Text([Text.Body], $"Error: {ex.Message}", props: TestId("cell-keyed-error"));
                return;
            }

            RenderCellCounter(view, cell.Counter.Value, cell.History.Value ?? [], cell.Process.Value, _labCellError.Value, "cell-keyed",
                onIncrement: async () => FireCellCommand(_labCellError, () => cell.IncrementAsync(1)),
                onReset: async () => FireCellCommand(_labCellError, () => cell.ResetAsync()));
        });
    }

    private void RenderGlobalCellCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "GlobalLabCell");

            IGlobalLabCell cell;

            try
            {
                cell = Cells.Instance.Connect<IGlobalLabCell>(new GlobalLabCell.SessionIdentity());
            }
            catch (Exception ex)
            {
                view.Text([Text.Body], $"Error: {ex.Message}", props: TestId("cell-global-error"));
                return;
            }

            RenderCellCounter(view, cell.Counter.Value, cell.History.Value ?? [], cell.Process.Value, _globalCellError.Value, "cell-global",
                onIncrement: async () => FireCellCommand(_globalCellError, () => cell.IncrementAsync(1)),
                onReset: async () => FireCellCommand(_globalCellError, () => cell.ResetAsync()));
        });
    }

    // Mirrors seed empty, so a cell reached across processes reads blank until the cell-host's first push.
    private void RenderCellCounter(UIView view, int counter, IReadOnlyList<string> history, string? process, string? error, string testId, Func<Task> onIncrement, Func<Task> onReset)
    {
        view.Row([Layout.Row.Md, "items-center"], content: view =>
        {
            view.Text([Text.DisplaySm, "tabular-nums flex-1"], counter.ToString(), props: TestId($"{testId}-counter"));
            view.Button([Button.PrimaryMd], text: "+1", onClick: onIncrement);
            view.Button([Button.OutlineMd], text: "Reset", onClick: onReset);
        });

        view.Text([Text.Body, "mt-2"],
            string.IsNullOrEmpty(process) ? "Served by …" : $"Served by {process} · {(process == ProcessLabel ? "this process" : "own cell-host")}",
            props: TestId($"{testId}-process"));

        if (error != null)
        {
            view.Text([Text.Body, "mt-2"], $"Error: {error}");
        }

        view.Box(["text-sm mt-3 min-h-20"], content: view =>
        {
            foreach (var line in history.Reverse().Take(4))
            {
                view.Text([Text.Caption], line);
            }
        });
    }

    private void RenderCellChecksCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-3"], content: view =>
            {
                view.Text([Text.H3, "flex-1"], "Checks over HTTP");
                view.Button([Button.PrimaryMd],
                    text: _cellChecking.Value ? "Running…" : "Run all",
                    props: TestId("cell-checks-run"),
                    disabled: _cellChecking.Value,
                    onClick: () => RunCellChecksAsync(CellChecks));
            });

            view.Box(["grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 items-center"], content: view =>
            {
                foreach (var check in CellChecks)
                {
                    view.Button([Button.OutlineSm],
                        text: check.Label,
                        props: TestId($"cell-check-{check.Key}-run"),
                        disabled: _cellChecking.Value,
                        onClick: () => RunCellChecksAsync([check]));
                    view.Text(["text-sm break-all"],
                        _cellCheckResults.TryGetValue(check.Key, out var result) ? result : "—",
                        props: TestId($"cell-check-{check.Key}"));
                }
            });
        });
    }

    // ── Cell ─────────────────────────────────────────────────────────────────
    // The whole point of this tab: one [Cell] class, identity-scoped state,
    // exposed through three surfaces that all mutate the same Reactive<T> fields.

    public sealed record LabCellIdentity(string Workspace);
    public sealed record LabIncrementRequest(int Delta);
    public sealed record LabSnapshot(int Counter, string[] History, string Workspace, string Process);
    public sealed record LabSignatureEcho(string Signature, string Workspace);
    public sealed record GlobalLabSnapshot(int Counter, string[] History, string Process);

    /// <summary>
    /// What an app process holds through <c>Cells.Connect&lt;ILabCell&gt;(identity)</c>. Reached through
    /// an interface, a cell served by another process becomes a proxy: its <c>Reactive&lt;T&gt;</c>
    /// properties are mirrors fed by an SDK subscription and its <c>[Function]</c> methods are sent over
    /// the SDK connection.
    /// </summary>
    public interface ILabCell
    {
        Reactive<int> Counter { get; }

        // A plain Reactive<T>, not ReactiveList<T>: the proxy mirrors only exact Reactive<T> getters.
#pragma warning disable IKON002
        Reactive<List<string>> History { get; }
#pragma warning restore IKON002
        Reactive<string> Process { get; }
        Task IncrementAsync(int delta);
        Task ResetAsync();
    }

    [Cell(IdleTtlSeconds = 600)]
    public sealed class LabCell(ICell<LabCellIdentity> ctx) : ILabCell
    {
        public LabCellIdentity Identity { get; } = ctx.Identity;
        public Reactive<int> Counter { get; } = new(0);
#pragma warning disable IKON002
        public Reactive<List<string>> History { get; } = new([]);
#pragma warning restore IKON002
        public Reactive<string> Process { get; } = new(ProcessLabel);

        // Every surface ends up here, so they all move the same state. History is replaced rather than
        // changed in place: a snapshot or a render reads the list it got without the reactive's lock.
        private void Increment(int delta)
        {
            Counter.Value += delta;
            History.Value = [.. History.Value, $"[{DateTime.UtcNow:HH:mm:ss}] +{delta} → {Counter.Value} ({Identity.Workspace})"];
        }

        [Function]
        public Task IncrementAsync(int delta)
        {
            Increment(delta);
            return Task.CompletedTask;
        }

        [Function]
        public Task ResetAsync()
        {
            Counter.Value = 0;
            History.Value = [];
            return Task.CompletedTask;
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
        // comes from the grant it is called with.
        [Mcp(Auth = EndpointAuth.Grant, Description = "Increment the Lab counter for the supplied workspace identity")]
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

        private LabSnapshot Snapshot() => new(Counter.Value, [.. History.Value], Identity.Workspace, Process.Value);
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

        // Stays a plain Reactive<T> (not ReactiveList<T>): the substrate proxy only mirrors
        // exact Reactive<T> getters — a ReactiveList<T> here would be dispatched as a wire
        // method and break Cells.Connect<IGlobalLabCell>() at runtime.
#pragma warning disable IKON002
        Reactive<List<string>> History { get; }
#pragma warning restore IKON002
        Reactive<string> Process { get; }
        Task IncrementAsync(int delta);
        Task ResetAsync();
    }

    /// <summary>
    /// Sibling cell with a parameterless SessionIdentity — one shared instance across the whole
    /// deployment. Reached through <see cref="IGlobalLabCell"/>; in the cloud the cell-host proxy mirrors
    /// its <c>Reactive&lt;T&gt;</c> state and dispatches its <c>[Function]</c> methods over a standard SDK
    /// connection to the cell-host (a local run hosts it in-process).
    /// </summary>
    [Cell(IdleTtlSeconds = 600)]
    public sealed class GlobalLabCell(ICell<GlobalLabCell.SessionIdentity> ctx) : IGlobalLabCell
    {
        public record SessionIdentity();  // empty → global, eager-spawned at host init

        private readonly ICell<SessionIdentity> _ctx = ctx;

        public Reactive<int> Counter { get; } = new(0);

        // Matches IGlobalLabCell.History exactly (interface implementation is invariant) and stays
        // a plain Reactive<T> so the substrate proxy can mirror it.
#pragma warning disable IKON002
        public Reactive<List<string>> History { get; } = new([]);
#pragma warning restore IKON002

        public Reactive<string> Process { get; } = new(ProcessLabel);

        // Internal mutation core — every surface (SDK [Function], REST [Rest], MCP [Mcp]) routes
        // through here so they all mutate the same Reactive<T> fields.
        private void IncrementCore(int delta)
        {
            Counter.Value += delta;
            History.Value = [.. History.Value, $"[{DateTime.UtcNow:HH:mm:ss}] +{delta} → {Counter.Value} (global)"];
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
            History.Value = [];
            return Task.CompletedTask;
        }

        [HttpPost("increment", Auth = EndpointAuth.Public)]
        public HttpResult IncrementHttp(LabIncrementRequest req)
        {
            IncrementCore(req.Delta);
            return HttpResult.Ok(Snapshot());
        }

        [Mcp(Auth = EndpointAuth.Grant, Name = "IncrementGlobalMcp", Description = "Increment the shared global Lab counter (no per-call identity)")]
        public GlobalLabSnapshot IncrementMcp([Description("How much to add")] int delta = 1)
        {
            IncrementCore(delta);
            return Snapshot();
        }

        private GlobalLabSnapshot Snapshot() => new(Counter.Value, [.. History.Value], Process.Value);
    }
}
