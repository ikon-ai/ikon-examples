namespace Ikon.App.Patterns.Patterns;

// Pattern: mcp-tools-from-server — see docs/patterns/mcp-tools-from-server.md.
// The example region below is the canonical body the doc extracts.
internal sealed class McpToolsFromServer(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "mcp-tools-from-server";
    public string Title => "Calling tools on an MCP server";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-mcp-tools-from-server
    private readonly ClientReactiveList<string> _toolNames = new();
    private readonly ClientReactive<string?> _output = new(null);
    private McpClient? _client;

    /// <summary>
    /// One client per server, held for as long as the app needs it. ConnectAsync is what
    /// populates Tools -- reading them before connecting gives an empty list, not an error.
    /// The tool names and the output are per-client state, so this and CallAsync run from a
    /// control's callback (the button below), where a ClientScope is active -- from Main() or
    /// a background task the writes throw.
    /// </summary>
    private async Task<McpClient> ConnectAsync(string endpoint, string token)
    {
        var client = new McpClient(endpoint, new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {token}",
        });

        await client.ConnectAsync();
        _toolNames.ReplaceAll(client.Tools.Select(t => t.Name));
        return client;
    }

    /// <summary>
    /// Arguments are a JsonElement matching the tool's own InputSchema, which the server
    /// declares -- so build them from that rather than from an assumption about the shape.
    /// </summary>
    private async Task CallAsync(McpClient client, string toolName, string query)
    {
        var arguments = JsonSerializer.SerializeToElement(new { query });

        // Long results paginate. McpClient.CallToolAsync returns the first page's content only; the raw
        // form hands back NextCursor so a caller can drain the rest.
        var page = await client.CallToolRawAsync(toolName, arguments);
        var text = new StringBuilder(page.Content);

        while (page.NextCursor is { } cursor)
        {
            page = await client.CallToolRawAsync(toolName, arguments, cursor);
            text.Append(page.Content);
        }

        _output.Value = text.ToString();
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            // The server's address and token are app secrets, set once with
            // `ikon secret set KB_MCP_URL` and `ikon secret set KB_MCP_TOKEN`, never in source.
            col.Button(text: "Connect", onClick: async () =>
            {
                if (!app.Secrets.TryGet("KB_MCP_URL", out var endpoint) || !app.Secrets.TryGet("KB_MCP_TOKEN", out var token))
                {
                    _output.Value = "Set the KB_MCP_URL and KB_MCP_TOKEN secrets, then restart the app.";
                    return;
                }

                var client = await ConnectAsync(endpoint, token);
                _client?.Dispose();
                _client = client;
            });
            col.Button(text: "Search", disabled: _client is null, onClick: async () => await CallAsync(_client!, "search", "ikon"));

            foreach (var name in _toolNames)
            {
                col.Text(["text-muted-foreground text-sm"], key: name, text: name);
            }

            if (_output.Value is { } output)
            {
                col.Markdown(output);
            }
        });
    }
    #endregion

    #region example:pattern-mcp-tools-in-emerge-run
    /// <summary>
    /// The model calls the server's tools itself. Only the tools named here are offered, because a server
    /// can add tools at any time. A tool that writes or deletes reaches the model only if the app names it.
    /// </summary>
    private static async Task<string> AnswerWithServerToolsAsync(McpClient client, string question, CancellationToken ct)
    {
        var serverTools = client.ToFunctions(["search", "get_document"], ct);

        return await Emerge.Run<string>(LLMModel.Claude55Sonnet, pass =>
        {
            pass.Command = question;
            // Every tool call of the run counts: enough for a few lookups, one answer. A list that
            // needs a lookup per item needs at least one call per item
            pass.MaxToolCalls = 6;

            foreach (var tool in serverTools)
            {
                pass.Tools.Add(tool);
            }
        }, ct);
    }
    #endregion
}
