public partial class Validation
{
    private void RenderMcpSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Public URL banner — copy this into any MCP client.
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "MCP Public Endpoint");
                view.Text([Text.Caption, "mb-4"],
                    "Any MCP client (Claude Desktop, custom HTTP, etc.) can POST JSON-RPC to this URL. " +
                    "Auto-derived from this app's [Mcp]-decorated methods.");

                if (_mcpStartError.Value is { } err)
                {
                    view.Box([Alert.Error], content: view => view.Text([Alert.Description], err));
                }
                else if (_mcpPublicUrl.Value is { } url)
                {
                    view.Text([Text.Caption, "font-mono select-all break-all"], url);
                }
                else
                {
                    view.Text([Text.Caption], "(starting…)");
                }
            });

            // Tool list — schemas come from the C# signatures + record return types.
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Tools");

                if (_mcpHost is null)
                {
                    view.Text([Text.Caption], "(MCP host not ready)");
                    return;
                }

                foreach (var tool in _mcpHost.Tools)
                {
                    view.Box(["border border-secondary rounded-lg p-4 mb-3"], content: view =>
                    {
                        view.Text([Text.BodyStrong], tool.Name);
                        view.Text([Text.Caption, "mb-2"], tool.Description);

                        view.Text([Text.Caption, "mt-2"], "inputSchema:");
                        view.Box(["bg-surface rounded p-2 mt-1"], content: v =>
                            v.Text([Text.Caption, "font-mono whitespace-pre"], PrettyJson(tool.InputSchema)));

                        if (tool.OutputSchema is { } outSchema)
                        {
                            view.Text([Text.Caption, "mt-2"], "outputSchema:");
                            view.Box(["bg-surface rounded p-2 mt-1"], content: v =>
                                v.Text([Text.Caption, "font-mono whitespace-pre"], PrettyJson(outSchema)));
                        }
                    });
                }
            });

            // Invocation form — round-trips through the same McpHost instance the public
            // URL serves, so what you see here is exactly what an external client gets.
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Invoke");

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Tool name");
                    view.TextField(
                        [Input.Default],
                        value: _mcpToolName.Value,
                        onValueChange: async v => _mcpToolName.Value = v ?? "");
                });

                view.Box([FormField.Root, "mt-3"], content: view =>
                {
                    view.Text([FormField.Label], "Arguments (JSON)");
                    view.TextArea(
                        [Textarea.Default],
                        value: _mcpArgsJson.Value,
                        onValueChange: async v => _mcpArgsJson.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "items-center mt-3"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        label: _mcpInvoking.Value ? "Invoking…" : "Invoke",
                        disabled: _mcpInvoking.Value || _mcpHost is null,
                        onClick: InvokeMcpToolAsync);

                    if (_mcpInvoking.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (_mcpInvokeResult.Value is { } result)
                {
                    view.Text([Text.Caption, "mt-4"], "Response:");
                    view.Box(["bg-surface rounded p-3 mt-1 max-h-96 overflow-auto"], content: v =>
                        v.Text([Text.Caption, "font-mono whitespace-pre"], result));
                }
            });
        });
    }
}
