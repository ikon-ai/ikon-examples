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

    [Mcp(Description = "Research a topic and return a structured brief using Emergence")]
    public async Task<TopicBrief> Research(
        [Description("The topic to research")] string topic,
        [Description("Depth: 1=quick, 3=thorough")] int depth = 2)
    {
        var (brief, _) = await Emerge.Run<TopicBrief>(
            LLMModel.Claude45Sonnet, new KernelContext(),
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
            }).FinalAsync();

        return brief;
    }

    [Mcp(Description = "Echo back a string (sanity-check tool)")]
    public string McpEcho([Description("Text to echo")] string text) => $"echo: {text}";

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
            var cellHost = Cells.Current
                ?? throw new InvalidOperationException(
                    "Cells.Current is null — expected IkonServer to publish the process-wide CellHost before App.Main runs");

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
