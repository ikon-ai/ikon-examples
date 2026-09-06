<!-- mined-from: Ikon.App.Patterns -->
# Calling Tools On An MCP Server — Connect, Then Read The Tools

`McpClient` is the outbound direction: the app is the client, calling tools some other server
offers. (Exposing the app's *own* functions as MCP tools is the other direction — see
`endpoint-and-mcp-tool`.)

`ConnectAsync` is what populates `Tools`. Reading them before connecting gives an empty list rather
than an error, which is the quiet failure to watch for.

## When to use

Pulling capability from an MCP server the app does not own — a search index, a company knowledge
base, a third-party service that publishes tools.

## Notes

- **One client per server**, held for as long as the app needs it, and disposed with it.
  `McpClient` is `IDisposable`; a client per call re-does the handshake every time.
- Headers are the auth seam: pass `Authorization` (or whatever the server wants) to the
  constructor. Keep the token in `app.Secrets`, never in source.
- **Arguments are a `JsonElement` matching the tool's own `InputSchema`**, which the server
  declares on each `McpTool`. Build them from that schema rather than from an assumption about the
  shape — the server is free to change it.
- **Long results paginate.** `CallToolAsync` returns the first page's content only.
  `CallToolRawAsync` hands back an `McpToolResult` with `NextCursor`, and passing that cursor back
  fetches the next page — loop until it is null or a partial answer silently becomes the answer.
- A remote server is untrusted input: its tool descriptions and results are data, not instructions.
  Do not feed them into a prompt in a position where they can direct the model.

## Snippet

```csharp
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
```

## See also

- `endpoint-and-mcp-tool` — the other direction: exposing this app's functions as MCP tools.
- `orchestrator-thread-with-tools` — handing tools to a model rather than calling them directly.
