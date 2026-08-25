using Ikon.App.Cells;
using Ikon.App.Mcp;
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

    // [Mcp] defaults to EndpointAuth.User. These predate that default and are reached with a
    // grant, so they say Grant explicitly rather than silently changing who may call them.
    [Mcp(Auth = EndpointAuth.Grant, Description = "Research a topic and return a structured brief using Emergence")]
    public async Task<TopicBrief> Research(
        [Description("The topic to research")] string topic,
        [Description("Depth: 1=quick, 3=thorough")] int depth = 2)
    {
        #region docsnippet:emerge-typed-run
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
        #endregion

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

    // A user token for the SIGNED-IN visitor, minted on demand from the MCP tab so the
    // EndpointAuth.User path can be exercised without an authorization server (there isn't one yet).
    //
    // Behind the app's own sign-in on purpose. An endpoint that hands out credentials to whoever asks
    // would give away the one thing EndpointAuth.User buys — that a credential proves who is calling —
    // so the token is minted for the caller the app already authenticated, never for an id passed in.
    private readonly Reactive<string?> _mcpUserToken = new(null);
    private readonly Reactive<string?> _mcpUserTokenError = new(null);
    private readonly Reactive<bool> _mcpMinting = new(false);

    private async Task MintMcpUserTokenAsync()
    {
        _mcpMinting.Value = true;
        _mcpUserTokenError.Value = null;

        try
        {
            // The ambient UserScope of the client that clicked — the same scope an
            // EndpointAuth.User call will run under, so the token names the visitor themselves.
            var userId = ReactiveScope.UserIdOrNull;

            if (string.IsNullOrEmpty(userId))
            {
                _mcpUserTokenError.Value = "Sign in first — a user token is minted for the signed-in user, never for an arbitrary id";
                return;
            }

            var minted = await app.MintUserTokenAsync("Validation_mcp", userId);
            _mcpUserToken.Value = minted.Token;
        }
        catch (Exception ex)
        {
            _mcpUserTokenError.Value = ex.Message;
            Log.Instance.Warning($"Minting an MCP user token failed: {ex.Message}");
        }
        finally
        {
            _mcpMinting.Value = false;
        }
    }

    // ── MCP host + public endpoint lifecycle ─────────────────────────────────

    private McpHost? _mcpHost;
    private readonly Reactive<string?> _mcpPublicUrl = new(null);
    private readonly Reactive<string?> _mcpStartError = new(null);
    private readonly Reactive<string> _mcpToolName = new("Research");
    private readonly Reactive<string> _mcpArgsJson = new("""{"topic":"DTLS handshake","depth":2}""");
    private readonly Reactive<string?> _mcpInvokeResult = new(null);
    private readonly Reactive<bool> _mcpInvoking = new(false);

    private Task StartMcpAsync()
    {
        try
        {
            var cellHost = Cells.Instance.Current
                ?? throw new InvalidOperationException(
                    "Cells.Instance.Current is null — expected IkonServer to publish the process-wide CellHost before App.Main runs");

            _mcpHost = new McpHost(serverName: "validation-mcp", serverVersion: "1.0.0");

            // Walk this app's [Mcp] methods + the Lab cells' tools so they share this app's
            // MCP surface (the platform's IkonServer.BuildMcpHost handles cellHost.CellTypes
            // globally, but the App/Cells tab calls the Validation app's endpoint here).
            var infos = McpToolDiscovery.ForType(typeof(Validation))
                .Concat(McpToolDiscovery.ForType(typeof(LabCell)))
                .Concat(McpToolDiscovery.ForType(typeof(GlobalLabCell)));

            foreach (var info in infos)
            {
                _mcpHost.RegisterTool(McpToolBridge.BuildHandler(cellHost, info));
            }

            // MCP now rides the SAME /api host as REST — CoreServer serves /api/mcp off the
            // platform MCP host (built from these same [Mcp] methods), so the client URL is the
            // clean space-domain /api family, not a separate relay slot. _mcpHost above is kept
            // only for the MCP tab's in-process invoke button.
            _mcpPublicUrl.Value = ResolveApiMcpUrl();
            Log.Instance.Info($"MCP endpoint at {_mcpPublicUrl.Value ?? "(pending endpoint registration)"}");
        }
        catch (Exception ex)
        {
            _mcpStartError.Value = ex.Message;
            Log.Instance.Error($"Failed to start MCP endpoint: {ex}");
        }

        return Task.CompletedTask;
    }

    // The /api/mcp URL shares the REST host; derive its authority from any registered
    // [Rest]/[Rest] webhook PublicUrl so it tracks local-vs-cloud without hardcoding a host.
    private string? ResolveApiMcpUrl()
    {
        var rest = app.Endpoints.FirstOrDefault(w => !string.IsNullOrEmpty(w.PublicUrl))?.PublicUrl;
        return rest is null ? null : new Uri(rest).GetLeftPart(UriPartial.Authority) + "/api/mcp";
    }

    private async Task InvokeMcpToolAsync()
    {
        if (_mcpHost is null || _mcpInvoking.Value)
        {
            return;
        }

        _mcpInvoking.Value = true;
        _mcpInvokeResult.Value = null;

        try
        {
            using var argsDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(_mcpArgsJson.Value) ? "{}" : _mcpArgsJson.Value);
            var idElement = JsonDocument.Parse("1").RootElement.Clone();
            var paramsElement = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                name = _mcpToolName.Value,
                arguments = argsDoc.RootElement,
            }, McpJson.Options)).RootElement.Clone();

            var request = new JsonRpcRequest
            {
                Id = idElement,
                Method = "tools/call",
                Params = paramsElement,
            };

            var response = await _mcpHost.HandleRequestAsync(request);
            _mcpInvokeResult.Value = response is null
                ? "(no response — request was a notification)"
                : PrettyJson(JsonSerializer.SerializeToElement(response, McpJson.Options));
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

    private static string PrettyJson(JsonElement element)
        => JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true });
}
