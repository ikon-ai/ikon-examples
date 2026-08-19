using Ikon.App.Cells;

public partial class Validation
{
    private void RenderLabSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // 0. Run mode — decides which URL shape every surface below gets.
            RenderRunModeBanner(view);

            // 1. Identity anchor.
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Identity");
                view.Text([Text.Caption, "mb-4"],
                    "Your app session identity is fixed by the platform. The two cells below illustrate " +
                    "the two cell patterns: keyed (LabCell — an AppProcess cell, one instance per " +
                    "Workspace within this process) and substrate (GlobalLabCell — reached via " +
                    "IGlobalLabCell, one instance per (CellType, SessionIdentity) across the deployment, " +
                    "served by a dedicated cell-host the cloud provisions on first webhook hit).");
                view.Text([Text.Caption, "font-mono"],
                    $"app.SessionIdentity = {app.SessionIdentity.UserId} / {app.SessionIdentity.Id}");

                view.Box(["mt-4"], content: view =>
                {
                    view.Text([Text.Caption, "mb-1"], "Cell identity (LabCellIdentity.Workspace) — used by the keyed cell below:");
                    view.Select(
                        value: _labWorkspace.Value,
                        options:
                        [
                            new SelectOption("alpha", "alpha"),
                            new SelectOption("beta", "beta"),
                            new SelectOption("gamma", "gamma"),
                        ],
                        onValueChange: async v => _labWorkspace.Value = v ?? "alpha");
                });
            });

            // 2. Session-identity construction reference.
            RenderIdentityConstruction(view);

            // 3. Keyed cell — one instance per Workspace.
            RenderKeyedCellPane(view);

            // 4. Global cell — single substrate instance, parameterless SessionIdentity.
            RenderGlobalCellPane(view);
        });
    }

    private void RenderKeyedCellPane(UIView view)
    {
        LabCell? cell = null;
        string? connectError = null;

        try
        {
            cell = Cells.Instance.Connect<LabCell>(new LabCellIdentity(_labWorkspace.Value));
        }
        catch (Exception ex)
        {
            connectError = $"{ex.GetType().Name}: {ex.Message}";
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-1"], "Keyed cell: LabCell");
            view.Text([Text.Caption, "mb-4"],
                $"Cells.Connect<LabCell>(new LabCellIdentity(\"{_labWorkspace.Value}\")) — " +
                $"State is workspace-scoped. Switch workspaces above to see isolation.");

            if (cell is null)
            {
                view.Box(["bg-error-solid/10 border border-error rounded p-3"], content: v =>
                    v.Text([Text.Caption, "font-mono"], $"Cells.Connect failed: {connectError ?? "(unknown)"}"));
                return;
            }

            view.Text([Text.Display], cell.Counter.Value.ToString());

            view.Row([Layout.Row.Md, "items-center mt-4 flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "+1", onClick: async () => { cell.Increment(1); });
                view.Button([Button.PrimaryMd], text: "+5", onClick: async () => { cell.Increment(5); });
                view.Button([Button.GhostMd], text: "Reset", onClick: async () =>
                {
                    cell.Counter.Value = 0;
                    cell.History.Clear();
                });
            });

            view.Text([Text.Caption, "mt-4 mb-1"], "History:");
            view.Box(["bg-surface rounded p-2 max-h-48 overflow-auto"], content: v =>
            {
                if (cell.History.Value.Count == 0)
                {
                    v.Text([Text.Caption], "(empty)");
                }
                else
                {
                    foreach (var line in cell.History.Value.AsEnumerable().Reverse())
                    {
                        v.Text([Text.Caption, "font-mono"], line);
                    }
                }
            });

            view.Text([Text.Caption, "mt-4"], "External surfaces (same Increment method, different transports):");
            RenderKeyedRestSurface(view);
            RenderKeyedGrantSurface(view);
            RenderKeyedMcpSurface(view);
        });
    }

    private void RenderGlobalCellPane(UIView view)
    {
        IGlobalLabCell? cell = null;
        string? connectError = null;

        try
        {
            cell = Cells.Instance.Connect<IGlobalLabCell>(new GlobalLabCell.SessionIdentity());
        }
        catch (Exception ex)
        {
            connectError = $"{ex.GetType().Name}: {ex.Message}";
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-1"], "Global cell: GlobalLabCell (substrate)");
            view.Text([Text.Caption, "mb-4"],
                "Cells.Connect<IGlobalLabCell>(new GlobalLabCell.SessionIdentity()) — parameterless " +
                "identity, reached through the cell's interface so the SubstrateCellProxy engages. " +
                "Counter / History are local mirrors fed by an SDK subscription to the cell-host; " +
                "IncrementAsync / ResetAsync dispatch over the SDK connection. The proxy discovers " +
                "the cell-host's AppEndpointHost relay URL on first call and POSTs [Rest] endpoints " +
                "there directly. See docs/private/specs/endpoint-spec.md.");

            if (cell is null)
            {
                view.Box(["bg-error-solid/10 border border-error rounded p-3"], content: v =>
                    v.Text([Text.Caption, "font-mono"], $"Cells.Connect failed: {connectError ?? "(unknown)"}"));
                return;
            }

            view.Text([Text.Display], cell.Counter.Value.ToString());

            // Fire-and-forget the substrate SDK hop: the Counter mirror updates when the cell-host
            // pushes the new value over the subscription, so the button must NOT await the remote
            // round-trip — awaiting it here blocks the handler (and the app's message loop while it
            // waits for the response), which is what made these buttons feel slow/frozen.
            view.Row([Layout.Row.Md, "items-center mt-4 flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "+1",
                    onClick: async () => FireGlobalCommand(() => cell.IncrementAsync(1)));
                view.Button([Button.PrimaryMd], text: "+5",
                    onClick: async () => FireGlobalCommand(() => cell.IncrementAsync(5)));
                view.Button([Button.GhostMd], text: "Reset",
                    onClick: async () => FireGlobalCommand(() => cell.ResetAsync()));
            });

            if (_globalCellLastError.Value is { } lastError)
            {
                view.Box(["bg-error-solid/10 border border-error rounded p-3 mt-3"], content: v =>
                {
                    v.Text([Text.Caption, "font-mono whitespace-pre-wrap"], lastError);
                    v.Text([Text.Caption, "mt-1"],
                        "Substrate cell SDK hop failed. The cell-host may not be provisioned yet, the " +
                        "/init cell-routing may not be wired live, or the relay tunnel may be down. The " +
                        "REST and MCP surfaces below still work through the gateway path.");
                });
            }

            // The History mirror seeds to an empty list (SubstrateCellProxy.EmptyMirrorValue), so the
            // .Value.Count read below is safe even before the first subscription push arrives.
            view.Text([Text.Caption, "mt-4 mb-1"], "History:");
            view.Box(["bg-surface rounded p-2 max-h-48 overflow-auto"], content: v =>
            {
                var history = cell.History.Value;
                if (history is null || history.Count == 0)
                {
                    v.Text([Text.Caption], "(empty)");
                }
                else
                {
                    foreach (var line in history.AsEnumerable().Reverse())
                    {
                        v.Text([Text.Caption, "font-mono"], line);
                    }
                }
            });

            view.Text([Text.Caption, "mt-4"], "External surfaces (same Increment method, no identity routing):");
            RenderGlobalRestSurface(view);
            RenderGlobalMcpSurface(view);
        });
    }

    // ── Keyed cell surface renderers ─────────────────────────────────────────

    private void RenderKeyedRestSurface(UIView view)
    {
        var webhook = app.Endpoints.FirstOrDefault(w => w.FunctionName == "LabCell_IncrementHttp");
        string? resolvedUrl = webhook is null || string.IsNullOrEmpty(webhook.PublicUrl)
            ? null
            : webhook.PublicUrl.Replace("{workspace}", _labWorkspace.Value);

        view.Box(["border border-secondary rounded-lg p-4 mb-3"], content: view =>
        {
            view.Text([Text.BodyStrong], "[HttpPost] LabCell.IncrementHttp");
            view.Text([Text.Caption], """POST { "Delta": <int> } to /api/lab/{workspace}/increment — Workspace identity comes from the URL path — Auth: public — Body: LabSnapshot""");
            RenderUrlDiagnostics(view, resolvedUrl, isRelay: false);

            view.Row([Layout.Row.Md, "items-center mt-3 flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd],
                    text: _labRestInvoking.Value ? "Sending…" : "Send POST { Delta: 1 }",
                    disabled: _labRestInvoking.Value || webhook is null,
                    onClick: InvokeLabRestAsync);

                if (_labRestInvoking.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (_labRestResult.Value is { } restResult)
            {
                view.Text([Text.Caption, "mt-3"], "Response:");
                view.Box(["bg-surface rounded p-3 mt-1 max-h-64 overflow-auto"], content: v =>
                    v.Text([Text.Caption, "font-mono whitespace-pre"], restResult));
            }
        });
    }

    private void RenderKeyedGrantSurface(UIView view)
    {
        view.Box(["border border-secondary rounded-lg p-4 mb-3"], content: view =>
        {
            view.Text([Text.BodyStrong], "[HttpPost · grant] LabCell.IncrementSecureHttp");
            view.Text([Text.Caption], """The same surface behind Auth = Grant: the bare /api/lab/{workspace}/increment-secure 401s. Mint a URL with app.MintUrl — it pins the Workspace and embeds a signed ?ikon-grant= (decoded below).""");

            view.Row([Layout.Row.Md, "items-center mt-3 flex-wrap"], content: view =>
            {
                view.Button([Button.OutlineMd], text: "Mint grant URL", onClick: async () => { await EnsureLabGrantUrlAsync(); });

                view.Button([Button.PrimaryMd],
                    text: _labGrantInvoking.Value ? "Sending…" : "Mint + POST { Delta: 1 }",
                    disabled: _labGrantInvoking.Value,
                    onClick: InvokeLabGrantRestAsync);

                if (_labGrantInvoking.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (_labGrantUrl.Value is { } grantUrl)
            {
                view.Text([Text.Caption, "mt-3"], "Minted URL — its grant decoded:");
                RenderUrlDiagnostics(view, grantUrl, isRelay: false);
            }

            if (_labGrantResult.Value is { } grantResult)
            {
                view.Text([Text.Caption, "mt-3"], "Response:");
                view.Box(["bg-surface rounded p-3 mt-1 max-h-64 overflow-auto"], content: v =>
                    v.Text([Text.Caption, "font-mono whitespace-pre"], grantResult));
            }
        });
    }

    private void RenderKeyedMcpSurface(UIView view)
    {
        var mcpEndpoint = app.Endpoints.FirstOrDefault(w => w.FunctionName == "LabCell_mcp");
        var url = string.IsNullOrEmpty(mcpEndpoint?.PublicUrl) ? null : mcpEndpoint.PublicUrl;

        view.Box(["border border-secondary rounded-lg p-4"], content: view =>
        {
            view.Text([Text.BodyStrong], "[Mcp] LabCell.IncrementMcp");
            view.Text([Text.Caption], "Served through the cell's ONE JSON-RPC multiplexer (/api/lab-cell/mcp) — not a per-tool POST. Invoke with a tools/call request naming the tool.");

            if (url is not null)
            {
                RenderUrlDiagnostics(view, url, isRelay: false);
                view.Text([Text.Caption, "mt-1"],
                    "All of the cell's [Mcp] tools share this one endpoint. Workspace rides the query to " +
                    "target a keyed instance, same as the REST call.");

                view.Row([Layout.Row.Md, "items-center mt-3 flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd, "h-auto whitespace-normal text-left"],
                        text: _labMcpInvoking.Value ? "Sending…" : "Call tools/call IncrementMcp {delta:1}",
                        disabled: _labMcpInvoking.Value,
                        onClick: InvokeLabMcpAsync);

                    if (_labMcpInvoking.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (_labMcpResult.Value is { } mcpResult)
                {
                    view.Text([Text.Caption, "mt-3"], "Response:");
                    view.Box(["bg-surface rounded p-3 mt-1 max-h-64 overflow-auto"], content: v =>
                        v.Text([Text.Caption, "font-mono whitespace-pre"], mcpResult));
                }
            }
            else
            {
                view.Text([Text.Caption, "mt-2"], "(MCP endpoint not started yet — see the MCP tab)");
            }
        });
    }

    // ── Global cell surface renderers ────────────────────────────────────────

    private void RenderGlobalRestSurface(UIView view)
    {
        var webhook = app.Endpoints.FirstOrDefault(w => w.FunctionName == "GlobalLabCell_IncrementHttp");
        string? resolvedUrl = webhook is null || string.IsNullOrEmpty(webhook.PublicUrl)
            ? null
            : webhook.PublicUrl;

        view.Box(["border border-secondary rounded-lg p-4 mb-3"], content: view =>
        {
            view.Text([Text.BodyStrong], "[Rest] GlobalLabCell.IncrementHttp");
            view.Text([Text.Caption], """POST { "Delta": <int> } — no Workspace query needed, identity is empty. The gateway reverse-proxies to the cell-host's relay URL (no client-visible redirect).""");
            RenderUrlDiagnostics(view, resolvedUrl, isRelay: false);

            view.Row([Layout.Row.Md, "items-center mt-3 flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd],
                    text: _globalRestInvoking.Value ? "Sending…" : "Send POST { Delta: 1 }",
                    disabled: _globalRestInvoking.Value || webhook is null,
                    onClick: InvokeGlobalRestAsync);

                if (_globalRestInvoking.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (_globalRestResult.Value is { } restResult)
            {
                view.Text([Text.Caption, "mt-3"], "Response:");
                view.Box(["bg-surface rounded p-3 mt-1 max-h-64 overflow-auto"], content: v =>
                    v.Text([Text.Caption, "font-mono whitespace-pre"], restResult));
            }
        });
    }

    private void RenderGlobalMcpSurface(UIView view)
    {
        var mcpEndpoint = app.Endpoints.FirstOrDefault(w => w.FunctionName == "GlobalLabCell_mcp");
        var url = string.IsNullOrEmpty(mcpEndpoint?.PublicUrl) ? null : mcpEndpoint.PublicUrl;

        view.Box(["border border-secondary rounded-lg p-4"], content: view =>
        {
            view.Text([Text.BodyStrong], "[Mcp] GlobalLabCell.IncrementMcp");
            view.Text([Text.Caption], "The cell's JSON-RPC multiplexer — reverse-proxied to the same cell-host instance as the [Rest] endpoint. Tool name: IncrementGlobalMcp.");

            if (url is not null)
            {
                RenderUrlDiagnostics(view, url, isRelay: false);

                view.Row([Layout.Row.Md, "items-center mt-3 flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd, "h-auto whitespace-normal text-left"],
                        text: _globalMcpInvoking.Value ? "Sending…" : "Call tools/call IncrementGlobalMcp {delta:1}",
                        disabled: _globalMcpInvoking.Value,
                        onClick: InvokeGlobalMcpAsync);

                    if (_globalMcpInvoking.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (_globalMcpResult.Value is { } mcpResult)
                {
                    view.Text([Text.Caption, "mt-3"], "Response:");
                    view.Box(["bg-surface rounded p-3 mt-1 max-h-64 overflow-auto"], content: v =>
                        v.Text([Text.Caption, "font-mono whitespace-pre"], mcpResult));
                }
            }
            else
            {
                view.Text([Text.Caption, "mt-2"], "(MCP endpoint not started yet — see the MCP tab)");
            }
        });
    }
}
