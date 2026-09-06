namespace Ikon.App.Patterns.Patterns;

// Pattern: mcp-tools-from-server — see docs/patterns/mcp-tools-from-server.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class McpToolsFromServer : IPatternDemo
{
    public string Slug => "mcp-tools-from-server";
    public string Title => "Calling tools on an MCP server";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-mcp-tools-from-server
    private readonly ClientReactiveList<string> _toolNames = new();
    private readonly ClientReactive<string?> _output = new(null);

    /// <summary>
    /// One client per server, held for as long as the app needs it. ConnectAsync is what
    /// populates Tools -- reading them before connecting gives an empty list, not an error.
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
}
