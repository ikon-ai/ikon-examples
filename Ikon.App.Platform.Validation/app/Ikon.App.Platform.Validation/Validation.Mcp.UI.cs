public partial class Validation
{
    private void RenderMcpSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "MCP");

            if (_mcpStartError.Value is { } err)
            {
                view.Box([Alert.Error], content: view => view.Text([Alert.Description], err));
            }

            RenderMcpEndpointCard(view);
            RenderMcpChecksCard(view);
            RenderMcpInvokeCard(view);
        });
    }

    private void RenderMcpEndpointCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Endpoint");

            if (ResolveApiMcpUrl() is not { } url)
            {
                view.Text([Text.Caption], "(starting…)");
                return;
            }

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Text([Text.Body, "select-all break-all flex-1"], url, props: TestId("mcp-url"));
                view.ActionButton([Button.OutlineSm],
                    action: ActionKind.CopyToClipboard,
                    options: new CopyToClipboardActionOptions { Text = url },
                    text: "Copy");
            });

            // A bare URL routes to a cloud instance, so a client pointed at a local run needs the
            // minted one, whose grant carries this run's instance id.
            if (app.GlobalState.ServerRunType == ServerRunType.Local)
            {
                view.Row([Layout.Row.Md, "items-center flex-wrap mt-3"], content: view =>
                {
                    if (_mcpShownGrantUrl.Value is { } minted)
                    {
                        view.Text([Text.Body, "select-all break-all flex-1"], minted, props: TestId("mcp-minted-url"));
                        view.ActionButton([Button.OutlineSm],
                            action: ActionKind.CopyToClipboard,
                            options: new CopyToClipboardActionOptions { Text = minted },
                            text: "Copy");
                    }
                    else
                    {
                        view.Button([Button.OutlineSm], text: "Mint a URL for this local run", props: TestId("mcp-mint-url"),
                            onClick: ShowMcpGrantUrlAsync);
                    }
                });
            }

            view.Box(["grid grid-cols-[auto_1fr] gap-x-6 gap-y-1 text-sm mt-4"], content: view =>
            {
                foreach (var tool in _mcpTools)
                {
                    view.Text([Text.BodyStrong], tool.Name, props: TestId($"mcp-tool-{tool.Name}"));
                    view.Text([], tool.Access);
                }
            });
        });
    }

    private void RenderMcpChecksCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-3"], content: view =>
            {
                view.Text([Text.H3, "flex-1"], "Checks over HTTP");
                view.Button([Button.PrimaryMd],
                    text: _mcpChecking.Value ? "Running…" : "Run all",
                    props: TestId("mcp-checks-run"),
                    disabled: _mcpChecking.Value,
                    onClick: () => RunMcpChecksAsync(McpChecks));
            });

            view.Box(["grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 items-center"], content: view =>
            {
                foreach (var check in McpChecks)
                {
                    view.Button([Button.OutlineSm],
                        text: check.Label,
                        props: TestId($"mcp-check-{check.Key}-run"),
                        disabled: _mcpChecking.Value,
                        onClick: () => RunMcpChecksAsync([check]));
                    view.Text(["text-sm break-all"],
                        _mcpCheckResults.TryGetValue(check.Key, out var result) ? result : "—",
                        props: TestId($"mcp-check-{check.Key}"));
                }
            });
        });
    }

    private void RenderMcpInvokeCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Invoke");

            view.Row([Layout.Row.Md, "items-end flex-wrap"], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Tool");
                    view.Select(
                        value: _mcpToolName.Value,
                        options: _mcpTools.Select(t => new SelectOption(t.Name, t.Name)).ToList(),
                        ariaLabel: "Tool",
                        props: TestId("mcp-tool"),
                        onValueChange: async name =>
                        {
                            _mcpToolName.Value = name;
                            _mcpArgsJson.Value = McpArgsSkeleton(name);
                            _mcpInvokeResult.Value = null;
                        });
                });

                view.Button([Button.PrimaryMd],
                    text: _mcpInvoking.Value ? "Invoking…" : "Invoke",
                    props: TestId("mcp-invoke"),
                    disabled: _mcpInvoking.Value,
                    onClick: InvokeMcpToolAsync);

                view.Text([Text.Caption, "self-center"], $"with {McpCredentialFor(_mcpToolName.Value)}");
            });

            view.Box(["grid gap-3 mt-3 md:grid-cols-2"], content: view =>
            {
                view.TextArea([Textarea.Default, "font-mono text-sm min-h-40"],
                    value: _mcpArgsJson.Value,
                    label: "Arguments",
                    props: TestId("mcp-args"),
                    onValueChange: async v => _mcpArgsJson.Value = v ?? "");

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Result");
                    view.Box(["rounded-md border border-secondary bg-surface p-3 min-h-40 max-h-96 overflow-auto"], content: view =>
                        view.Text(["text-sm whitespace-pre-wrap break-all"], _mcpInvokeResult.Value ?? "—", props: TestId("mcp-invoke-result")));
                });
            });
        });
    }
}
