using Ikon.App.Cells;
using Ikon.App.Mcp;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
// Bare [Description] resolves to Ikon.Common.DescriptionAttribute (Property|Field only),
// which the compiler rejects on positional record parameters and method parameters.
// The BCL one allows AttributeTargets.All and is what JsonSchemaBuilder + JsonSchemaGenerator
// pick up across both surfaces. Alias it so bare [Description] just works here.
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

public partial class Validation
{
    // ── Tool surface ─────────────────────────────────────────────────────────
    // [Mcp] methods on the App class are discovered automatically — the
    // running Validation instance is registered as a singleton in CellHost
    // by IkonServer's plugin bootstrap, so McpToolDiscovery walks it like
    // any other cell type.

    public sealed record TopicBrief(
        [Description("One-sentence summary of the topic")] string Summary,
        [Description("Key facts a reader should know")] string[] KeyFacts,
        [Description("Follow-up questions worth pursuing")] string[] OpenQuestions,
        [Description("0.0–1.0 confidence in the brief's accuracy")] double Confidence);

    // Reached with a grant, so Auth says Grant rather than the User default.
    [Mcp(Auth = EndpointAuth.Grant, Description = "Research a topic and return a structured brief using Emergence")]
    public async Task<TopicBrief> Research(
        [Description("The topic to research")] string topic,
        [Description("Depth: 1=quick, 3=thorough")] int depth = 2)
    {
        var brief = await Emerge.Run<TopicBrief>(
            LLMModel.Claude45Sonnet,
            pass =>
            {
                pass.SystemPrompt = """
                    Research the given topic. Return JSON matching the output schema.
                    Be concrete — named entities, dates, numbers. Confidence reflects
                    how grounded your facts are; lower it if you're guessing.
                    """;
                pass.Command = $"Topic: {topic}\nDepth: {depth}";
                pass.Temperature = 0.2;
                pass.MaxIterations = depth;
            }).ResultAsync();
        
        return brief;
    }

    [Mcp(Auth = EndpointAuth.Grant, Description = "Echo back a string (sanity-check tool)")]
    public string McpEcho([Description("Text to echo")] string text) => $"echo: {text}";

    // ── Mixed auth across one multiplexer ────────────────────────────────────
    // Three credentials behind the single /api/mcp URL, which is the whole point of the per-tool
    // gate: McpPing is Public, McpEcho/Research take a grant, McpWhoAmI takes a user token. One
    // Public tool opens the gate so any client can handshake and read tools/list; each tools/call
    // is then authorized against the tool it names.

    [Mcp(Auth = EndpointAuth.Public, Description = "Anonymous liveness check — no credential")]
    public string McpPing() => "pong";

    /// <summary>
    /// The scoped tool: reachable by the same user token as <see cref="McpWhoAmI"/>, but only when
    /// that token also carries <c>validation:write</c>.
    /// </summary>
    /// <remarks>
    /// Exists so per-tool scope is exercised against a real deployment rather than unit tests alone.
    /// It is what makes three things observable end to end: the declared scope reaching the resource
    /// metadata through the manifest, the 403 <c>insufficient_scope</c> a caller without it gets, and
    /// the challenge naming the scopes already held alongside the missing one.
    /// </remarks>
    [Mcp(Auth = EndpointAuth.User, Scope = "validation:write", Description = "Requires the validation:write scope on top of a user token")]
    public string McpScopedWrite([Description("Text to record")] string text)
    {
        return $"scoped write ok: {text}";
    }

    /// <summary>
    /// The user-authorized tool. Returns who the caller proved to be AND a value read out of that
    /// user's own <see cref="UserReactive{T}"/> partition, so a passing call demonstrates both halves:
    /// the token authorized, and the handler ran inside that user's scope rather than merely knowing
    /// their id. A scopeless read would throw instead of quietly answering with someone else's data.
    /// </summary>
    [Mcp(Auth = EndpointAuth.User, Description = "Report the signed-in space user this call runs as")]
    public string McpWhoAmI()
    {
        var userId = McpCallContext.Current?.UserId ?? "(none)";
        var visits = _mcpUserVisits.Value + 1;
        _mcpUserVisits.Value = visits;

        return $"userId={userId} scopedVisits={visits}";
    }

    // Partitioned per user by the ambient UserScope the endpoint dispatch pushes. Two different users
    // calling McpWhoAmI must see their own counts; a call with no proven user cannot reach it at all.
    private readonly UserReactive<int> _mcpUserVisits = new(0);

    // Counts out loud: reports each step as MCP progress, so a client that asked for an event stream
    // sees notifications/progress arrive before the result.
    [Mcp(Auth = EndpointAuth.Public, Description = "Count to steps, reporting each step as progress")]
    public async Task<string> McpCountdown([Description("How many steps to report")] int steps, IProgress<ProgressUpdate> progress, CancellationToken ct)
    {
        steps = Math.Clamp(steps, 1, 10);

        for (var step = 1; step <= steps; step++)
        {
            await Task.Delay(200, ct);
            progress.Report(new ProgressUpdate(step, steps, $"step {step} of {steps}"));
        }

        return $"counted {steps}";
    }

    // Waits long enough that a notifications/cancelled sent a moment later is what ends it.
    [Mcp(Auth = EndpointAuth.Public, Description = "Wait the given number of seconds unless cancelled")]
    public async Task<string> McpWait([Description("Seconds to wait, at most 25")] int seconds, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 25)), ct);
        return $"waited {seconds}s";
    }

    [McpResource("validation://status", Description = "The app's current server time and connected client count", MimeType = "text/plain")]
    public string McpStatusResource() => $"server time {DateTime.UtcNow:HH:mm:ss}Z, {app.Clients.Ids.Count()} client(s) connected";

    // ── Calling /api/mcp over HTTP ───────────────────────────────────────────
    // Every call on the MCP tab goes through the public endpoint, as an MCP client's would: the edge
    // checks the credential each tool asks for, then reverse-proxies to this app — through the relay
    // on a local run.
    //
    // Calls go to a URL minted for Validation_mcp. With no identity the grant pins this instance's own
    // session (and a local run's instance id), so the call lands here rather than on whichever
    // instance a bare URL would find or cold-start. Refusal checks use the bare URL: the edge refuses
    // them before routing anywhere.
    //
    // User tokens are minted for the SIGNED-IN visitor, never for an id passed in: an endpoint that
    // handed out credentials to whoever asked would give away the one thing EndpointAuth.User buys.

    private const string McpEndpointName = "Validation_mcp";
    private const string McpScope = "validation:write";

    private static readonly HttpClient _mcpHttp = new() { Timeout = TimeSpan.FromSeconds(40) };

    private sealed record McpToolRow(string Name, EndpointAuth Auth, string Scope, JsonElement InputSchema)
    {
        public string Access => string.IsNullOrEmpty(Scope) ? Auth.ToString().ToLowerInvariant() : $"{Auth.ToString().ToLowerInvariant()} + {Scope}";
    }

    private sealed record McpHttpResponse(int Status, string Challenge, IReadOnlyList<string> Messages)
    {
        // The JSON-RPC answer is the last message; an event stream puts notifications before it.
        public string Body => Messages.Count == 0 ? "" : Messages[^1];
    }

    private sealed record McpCheck(string Key, string Label, Func<Validation, string?, Task<string>> Run);

    // Only the checks that mint a user token need someone signed in; the rest say so only when they run.
    private static string RequireUser(string? userId)
        => string.IsNullOrEmpty(userId) ? throw new InvalidOperationException("no signed-in user to mint a token for") : userId;

    private static readonly McpCheck[] McpChecks =
    [
        new("list", "tools/list", (v, _) => v.CheckMcpToolListAsync()),
        new("ping", "McpPing (public)", (v, _) => v.CheckMcpToolAsync(nameof(McpPing), new { }, null, "pong")),
        new("echo-refused", "McpEcho without a grant", (v, _) => v.CheckMcpRefusedAsync(nameof(McpEcho), new { text = "x" }, 401, null)),
        new("echo", "McpEcho with the grant", (v, _) => v.CheckMcpToolAsync(nameof(McpEcho), new { text = "hello" }, null, "echo: hello")),
        new("whoami-refused", "McpWhoAmI without a token", (v, _) => v.CheckMcpRefusedAsync(nameof(McpWhoAmI), new { }, 401, "Bearer")),
        new("whoami", "McpWhoAmI with my token", (v, userId) => v.CheckMcpWhoAmIAsync(RequireUser(userId))),
        new("scope-refused", "McpScopedWrite without its scope", (v, userId) => v.CheckMcpScopeRefusedAsync(RequireUser(userId))),
        new("scope", "McpScopedWrite with its scope", (v, userId) => v.CheckMcpToolAsync(nameof(McpScopedWrite), new { text = "hello" }, (RequireUser(userId), true), "scoped write ok: hello")),
        new("resource", "Read validation://status", (v, userId) => v.CheckMcpResourceAsync(RequireUser(userId))),
        new("progress", "Progress from McpCountdown", (v, userId) => v.CheckMcpProgressAsync(RequireUser(userId))),
        new("cancel", "Cancel McpWait", (v, userId) => v.CheckMcpCancelAsync(RequireUser(userId))),
    ];

    private IReadOnlyList<McpToolRow> _mcpTools = [];
    private (string? BaseUrl, Task<string> Mint)? _mcpGrantUrl;

    private readonly Reactive<string?> _mcpStartError = new(null);
    private readonly ClientReactiveDictionary<string, string> _mcpCheckResults = new();
    private readonly ClientReactive<bool> _mcpChecking = new(false);
    private readonly ClientReactive<string> _mcpToolName = new(nameof(McpEcho));
    private readonly ClientReactive<string> _mcpArgsJson = new("""
        {
          "text": "hello"
        }
        """);
    private readonly ClientReactive<string?> _mcpInvokeResult = new(null);
    private readonly ClientReactive<bool> _mcpInvoking = new(false);
    private readonly ClientReactive<string?> _mcpShownGrantUrl = new(null);

    private Task StartMcpAsync()
    {
        try
        {
            var cellHost = Cells.Instance.Current
                ?? throw new InvalidOperationException(
                    "Cells.Instance.Current is null — expected IkonServer to publish the process-wide CellHost before App.Main runs");

            // The tools /api/mcp serves for this app, with the access each one declares and its input
            // schema for pre-filling arguments. The Lab cells' tools live on their own /api/lab-cell/mcp.
            _mcpTools = McpToolDiscovery.ForType(typeof(Validation))
                .Select(info =>
                {
                    var attribute = info.Handler.GetCustomAttribute<McpAttribute>();
                    var schema = McpToolBridge.BuildHandler(cellHost, info).InputSchema;
                    return new McpToolRow(info.Name, attribute?.Auth ?? EndpointAuth.User, attribute?.Scope ?? "", schema);
                })
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception ex)
        {
            _mcpStartError.Value = ex.Message;
            Log.Instance.Error($"Failed to start MCP endpoint: {ex}");
        }

        return Task.CompletedTask;
    }

    // The /api/mcp URL shares the REST host; derive its authority from any registered endpoint's
    // PublicUrl. Read on every use: a local run's endpoints switch from the local address to the
    // space domain (reverse-proxied through the relay) only once the run registers with the backend.
    private string? ResolveApiMcpUrl()
    {
        var rest = app.Endpoints.FirstOrDefault(w => !string.IsNullOrEmpty(w.PublicUrl))?.PublicUrl;
        return rest is null ? null : new Uri(rest).GetLeftPart(UriPartial.Authority) + "/api/mcp";
    }

    // Minted once per endpoint address: a local run's address changes when it registers with the
    // backend, and a grant minted before that carries no instance id to route back here. Re-minting the
    // same non-expiring grant otherwise returns the same URL. A failed mint is not kept.
    private async Task<string> GetMcpGrantUrlAsync()
    {
        var baseUrl = ResolveApiMcpUrl();

        if (_mcpGrantUrl is not { } cached || cached.BaseUrl != baseUrl)
        {
            cached = (baseUrl, MintMcpGrantUrlAsync());
            _mcpGrantUrl = cached;
        }

        try
        {
            return await cached.Mint;
        }
        catch
        {
            _mcpGrantUrl = null;
            throw;
        }
    }

    private async Task<string> MintMcpGrantUrlAsync() => (await app.MintUrlAsync(McpEndpointName)).Url;

    private async Task ShowMcpGrantUrlAsync()
    {
        try
        {
            _mcpShownGrantUrl.Value = await GetMcpGrantUrlAsync();
        }
        catch (Exception ex)
        {
            _mcpShownGrantUrl.Value = $"Error: {ex.Message}";
        }
    }

    private async Task<McpHttpResponse> SendMcpAsync(object payload, string? bearerToken, bool addressed = true)
    {
        var url = addressed
            ? await GetMcpGrantUrlAsync()
            : ResolveApiMcpUrl() ?? throw new InvalidOperationException("the MCP endpoint URL is not known yet");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");

        if (bearerToken != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await _mcpHttp.SendAsync(request);
        var challenge = string.Join(" ", response.Headers.WwwAuthenticate.Select(h => h.ToString()));
        var text = await response.Content.ReadAsStringAsync();

        // Streamable HTTP answers as an event stream when the client accepts one: each data line is one
        // JSON-RPC message, notifications first and the response last.
        IReadOnlyList<string> messages = response.Content.Headers.ContentType?.MediaType == "text/event-stream"
            ? text.Split('\n')
                .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
                .Select(line => line["data:".Length..].Trim())
                .ToList()
            : [text];

        return new McpHttpResponse((int)response.StatusCode, challenge, messages);
    }

    private static object ToolCall(string tool, object arguments, object? id = null)
        => new { jsonrpc = "2.0", id = id ?? 1, method = "tools/call", @params = new { name = tool, arguments } };

    private async Task<string> MintMcpUserTokenAsync(string userId, bool withScope)
        => (await app.MintUserTokenAsync(McpEndpointName, userId, scopes: withScope ? ["mcp:tools", McpScope] : null)).Token;

    private async Task RunMcpChecksAsync(IEnumerable<McpCheck> checks)
    {
        _mcpChecking.Value = true;

        try
        {
            var userId = ReactiveScope.UserIdOrNull;

            foreach (var check in checks)
            {
                _mcpCheckResults[check.Key] = "Running…";

                try
                {
                    _mcpCheckResults[check.Key] = await check.Run(this, userId);
                }
                catch (Exception ex)
                {
                    _mcpCheckResults[check.Key] = $"FAIL {ex.Message}";
                }
            }
        }
        finally
        {
            _mcpChecking.Value = false;
        }
    }

    private async Task<string> CheckMcpToolListAsync()
    {
        var response = await SendMcpAsync(new { jsonrpc = "2.0", id = 1, method = "tools/list" }, null);

        if (response.Status != 200)
        {
            return $"FAIL HTTP {response.Status}: {Truncate(response.Body, 200)}";
        }

        using var listDocument = JsonDocument.Parse(response.Body);
        var tools = listDocument.RootElement.GetProperty("result").GetProperty("tools");
        var listed = tools.EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToHashSet();
        var missing = _mcpTools.Select(t => t.Name).Where(name => !listed.Contains(name)).ToList();

        if (missing.Count > 0)
        {
            return $"FAIL not listed: {string.Join(", ", missing)}";
        }

        bool researchHasOutputSchema = tools.EnumerateArray()
            .Any(t => t.GetProperty("name").GetString() == nameof(Research) && t.TryGetProperty("outputSchema", out _));

        return researchHasOutputSchema
            ? $"PASS {listed.Count} tools, Research declares an output schema"
            : "FAIL Research has no outputSchema";
    }

    private async Task<string> CheckMcpToolAsync(string tool, object arguments, (string UserId, bool WithScope)? user, string expected)
    {
        var token = user is { } u ? await MintMcpUserTokenAsync(u.UserId, u.WithScope) : null;
        var response = await SendMcpAsync(ToolCall(tool, arguments), token);

        if (response.Status != 200)
        {
            return $"FAIL HTTP {response.Status}: {Truncate(response.Body, 200)}";
        }

        var answer = DescribeMcpBody(response.Body);
        return answer == $"OK: {expected}" ? $"PASS {expected}" : $"FAIL expected \"{expected}\", got {answer}";
    }

    private async Task<string> CheckMcpRefusedAsync(string tool, object arguments, int expectedStatus, string? expectedChallenge)
    {
        var response = await SendMcpAsync(ToolCall(tool, arguments), null, addressed: false);

        if (response.Status != expectedStatus)
        {
            return $"FAIL expected HTTP {expectedStatus}, got {response.Status}: {Truncate(response.Body, 200)}";
        }

        if (expectedChallenge != null && !response.Challenge.StartsWith(expectedChallenge, StringComparison.Ordinal))
        {
            return $"FAIL HTTP {expectedStatus} without a {expectedChallenge} challenge";
        }

        return expectedChallenge == null
            ? $"PASS refused: {expectedStatus}"
            : $"PASS refused: {expectedStatus} with a {expectedChallenge} challenge";
    }

    private async Task<string> CheckMcpWhoAmIAsync(string userId)
    {
        var response = await SendMcpAsync(ToolCall(nameof(McpWhoAmI), new { }), await MintMcpUserTokenAsync(userId, withScope: false));

        if (response.Status != 200)
        {
            return $"FAIL HTTP {response.Status}: {Truncate(response.Body, 200)}";
        }

        var answer = DescribeMcpBody(response.Body);
        return answer.StartsWith($"OK: userId={userId} ", StringComparison.Ordinal)
            ? $"PASS {answer["OK: ".Length..]}"
            : $"FAIL expected userId={userId}, got {answer}";
    }

    private async Task<string> CheckMcpScopeRefusedAsync(string userId)
    {
        var response = await SendMcpAsync(ToolCall(nameof(McpScopedWrite), new { text = "refused" }), await MintMcpUserTokenAsync(userId, withScope: false));
        bool insufficientScope = response.Challenge.Contains("insufficient_scope", StringComparison.Ordinal)
            || response.Body.Contains("insufficient_scope", StringComparison.Ordinal);

        return response.Status == 403 && insufficientScope
            ? "PASS refused: 403 insufficient_scope"
            : $"FAIL expected 403 insufficient_scope, got HTTP {response.Status}: {Truncate(response.Body, 200)}";
    }

    private async Task<string> CheckMcpResourceAsync(string userId)
    {
        const string uri = "validation://status";
        var list = await SendMcpAsync(new { jsonrpc = "2.0", id = 1, method = "resources/list" }, null);

        if (list.Status != 200 || !list.Body.Contains(uri, StringComparison.Ordinal))
        {
            return $"FAIL resources/list does not name {uri}: HTTP {list.Status} {Truncate(list.Body, 200)}";
        }

        // resources/read takes the strictest policy among the tools, a user token here.
        var read = await SendMcpAsync(new { jsonrpc = "2.0", id = 2, method = "resources/read", @params = new { uri } },
            await MintMcpUserTokenAsync(userId, withScope: false));

        if (read.Status != 200)
        {
            return $"FAIL resources/read HTTP {read.Status}: {Truncate(read.Body, 200)}";
        }

        using var readDocument = JsonDocument.Parse(read.Body);
        var root = readDocument.RootElement;

        if (!root.TryGetProperty("result", out var result) || result.GetProperty("contents").GetArrayLength() == 0)
        {
            return $"FAIL no contents: {Truncate(read.Body, 200)}";
        }

        return $"PASS {result.GetProperty("contents")[0].GetProperty("text").GetString()}";
    }

    // The progress and cancel checks send a user token although both tools are public: the edge
    // resolves a tool's access from the deployed bundle, so a local run's tool the deployment does
    // not have yet takes the strictest rule. The token satisfies either.
    private async Task<string> CheckMcpProgressAsync(string userId)
    {
        const int steps = 3;
        var response = await SendMcpAsync(ToolCall(nameof(McpCountdown), new { steps }), await MintMcpUserTokenAsync(userId, withScope: false));

        if (response.Status != 200)
        {
            return $"FAIL HTTP {response.Status}: {Truncate(response.Body, 200)}";
        }

        var notifications = response.Messages.Count(m => m.Contains("\"notifications/progress\"", StringComparison.Ordinal));
        var answer = DescribeMcpBody(response.Body);

        return notifications == steps && answer == $"OK: counted {steps}"
            ? $"PASS {notifications} progress notifications, then \"counted {steps}\""
            : $"FAIL {notifications} progress notifications of {steps}, answer {answer}";
    }

    private async Task<string> CheckMcpCancelAsync(string userId)
    {
        // The host keys in-flight calls by request id alone, so the id must be unique across callers.
        var requestId = $"validation-cancel-{Guid.NewGuid():N}";
        var token = await MintMcpUserTokenAsync(userId, withScope: false);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var call = SendMcpAsync(ToolCall(nameof(McpWait), new { seconds = 20 }, requestId), token);

        await Task.Delay(TimeSpan.FromSeconds(2));
        var cancel = await SendMcpAsync(new
        {
            jsonrpc = "2.0",
            method = "notifications/cancelled",
            @params = new { requestId, reason = "validation check" },
        }, null);

        if (cancel.Status is not (200 or 202))
        {
            // Let the uncancelled call finish rather than leave it running unobserved.
            await call;
            return $"FAIL notifications/cancelled answered HTTP {cancel.Status}";
        }

        var response = await call;
        var elapsed = stopwatch.Elapsed.TotalSeconds;

        // Anything but a 200 means the call never reached the tool, so there was nothing to cancel.
        if (response.Status != 200)
        {
            return $"FAIL the call answered HTTP {response.Status}: {Truncate(response.Body, 200)}";
        }

        return elapsed < 10 && !response.Body.Contains("waited", StringComparison.Ordinal)
            ? $"PASS stopped after {elapsed:F1}s of 20s"
            : $"FAIL ran {elapsed:F1}s: {Truncate(response.Body, 200)}";
    }

    private async Task InvokeMcpToolAsync()
    {
        if (_mcpInvoking.Value)
        {
            return;
        }

        _mcpInvoking.Value = true;
        _mcpInvokeResult.Value = null;

        try
        {
            var tool = _mcpTools.FirstOrDefault(t => t.Name == _mcpToolName.Value);
            using var argsDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(_mcpArgsJson.Value) ? "{}" : _mcpArgsJson.Value);
            string? token = null;

            if (tool?.Auth == EndpointAuth.User)
            {
                var userId = ReactiveScope.UserIdOrNull;

                if (string.IsNullOrEmpty(userId))
                {
                    _mcpInvokeResult.Value = "Error: no signed-in user to mint a token for";
                    return;
                }

                token = await MintMcpUserTokenAsync(userId, withScope: !string.IsNullOrEmpty(tool.Scope));
            }

            var response = await SendMcpAsync(ToolCall(_mcpToolName.Value, argsDoc.RootElement.Clone()), token);

            _mcpInvokeResult.Value = response.Status == 200
                ? DescribeMcpBody(response.Body)
                : $"Error: HTTP {response.Status} {response.Body}";
        }
        catch (Exception ex)
        {
            _mcpInvokeResult.Value = $"Error: {ex.Message}";
        }
        finally
        {
            _mcpInvoking.Value = false;
        }
    }

    private string McpCredentialFor(string toolName)
        => _mcpTools.FirstOrDefault(t => t.Name == toolName) switch
        {
            { Auth: EndpointAuth.Public } => "no credential",
            { Auth: EndpointAuth.Grant } => "the minted grant",
            { Auth: EndpointAuth.User, Scope: { Length: > 0 } scope } => $"my user token + {scope}",
            { Auth: EndpointAuth.User } => "my user token",
            _ => "?",
        };

    private static string DescribeMcpBody(string json)
    {
        using var document = JsonDocument.Parse(json);
        return DescribeMcpResponse(document.RootElement);
    }

    // The tool's own answer, not the JSON-RPC envelope around it: a result's text content, or the
    // error the host or the tool reported.
    private static string DescribeMcpResponse(JsonElement response)
    {
        if (response.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
        {
            return $"Error: {(error.TryGetProperty("message", out var message) ? message.GetString() : error.ToString())}";
        }

        if (!response.TryGetProperty("result", out var result))
        {
            return $"Error: unexpected response {response}";
        }

        var texts = result.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array
            ? content.EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .ToList()
            : [];
        string body = texts.Count > 0 ? string.Join("\n", texts.Select(IndentIfJson)) : IndentIfJson(result.ToString());
        bool isError = result.TryGetProperty("isError", out var flag) && flag.ValueKind == JsonValueKind.True;

        return isError ? $"Error: {body}" : $"OK: {body}";
    }

    // A tool that returns a record answers with its JSON as text; indent it so it reads like the input.
    private static string IndentIfJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.TrimStart()[0] is not ('{' or '['))
        {
            return text ?? "";
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            return "\n" + JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
        }
        catch (JsonException)
        {
            // Text that only looks like JSON is shown as the tool sent it.
            return text;
        }
    }

    // An argument object with every input property present, so choosing a tool shows what it takes.
    private string McpArgsSkeleton(string toolName)
    {
        var tool = _mcpTools.FirstOrDefault(t => t.Name == toolName);

        if (tool is null || !tool.InputSchema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return "{}";
        }

        var skeleton = new Dictionary<string, object?>();

        foreach (var property in properties.EnumerateObject())
        {
            string type = property.Value.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                ? typeElement.GetString() ?? ""
                : "";
            // A declared default first; otherwise a value that makes the call do something visible,
            // since a zero delta or an empty topic returns nothing worth looking at.
            skeleton[property.Name] = property.Value.TryGetProperty("default", out var defaultValue)
                ? (object?)defaultValue.Clone()
                : type switch
                {
                    "integer" or "number" => 1,
                    "boolean" => true,
                    "array" => Array.Empty<object>(),
                    "object" => new Dictionary<string, object>(),
                    _ => "test",
                };
        }

        return JsonSerializer.Serialize(skeleton, new JsonSerializerOptions { WriteIndented = true });
    }
}
