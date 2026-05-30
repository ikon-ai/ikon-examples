using Ikon.App.Cells;

public partial class Validation
{
    // How durable a given external URL is. The inbound-unification work left the platform with two
    // distinct "stable URL" notions plus an ephemeral local form, and conflating them is the usual
    // source of "why did my URL change / why is it still /webhook/" confusion — so the Lab tab names
    // each one explicitly.
    private enum UrlStability
    {
        // Lives under the space domain ({space}.ikonai.app). Survives reprovisioning and relay
        // restarts because it never names a physical location; identity rides in a long-TTL signed
        // token. This is the durable shape to register with third parties.
        SpaceStable,

        // A relay slot URL whose public port was pinned via AppEndpointHost(stablePortName: "..."),
        // so it survives reconnects and process restarts — but it's a relay slot, meant as an
        // internal proxy upstream, not a client-facing address.
        PortStable,

        // A relay slot URL with no pinned port, or a local-dev tunnel URL — the host/port can change
        // on the next start, so it must not be persisted by any external caller.
        Ephemeral,
    }

    private enum UrlShape
    {
        NotRegistered,
        CleanApi,        // {space}.ikonai.app/api/...  — app-declared path (S5)
        LegacyGateway,   // {space}.ikonai.app/ikon/webhook/... — default fallback (no declared Path)
        LocalWebhook,    // {tunnel}/webhook/... — local-dev CoreServer route
        RelaySlot,       // https://{slot}.relay...:{port}/... — relay tunnel (internal upstream)
    }

    private sealed record UrlReadout(UrlStability Stability, UrlShape Shape, bool HasSignedToken);

    private static UrlReadout ClassifyWebhookUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return new UrlReadout(UrlStability.Ephemeral, UrlShape.NotRegistered, false);
        }

        bool hasToken = url.Contains("_st=", StringComparison.Ordinal);

        if (url.Contains("/api/", StringComparison.Ordinal))
        {
            return new UrlReadout(UrlStability.SpaceStable, UrlShape.CleanApi, hasToken);
        }

        if (url.Contains("/ikon/webhook/", StringComparison.Ordinal))
        {
            return new UrlReadout(UrlStability.SpaceStable, UrlShape.LegacyGateway, hasToken);
        }

        // Local dev: CoreServer serves /webhook/{cellType?}/{functionName} on its own tunnel URL,
        // which is regenerated per run — ephemeral.
        return new UrlReadout(UrlStability.Ephemeral, UrlShape.LocalWebhook, hasToken);
    }

    private static UrlReadout ClassifyRelayUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return new UrlReadout(UrlStability.Ephemeral, UrlShape.NotRegistered, false);
        }

        // Relay slot URL — ephemeral. Even with AppEndpointHost(stablePortName: …) the relay was
        // verified to hand out a different port across app restarts (:20008 → :20005), so a relay
        // URL is NOT a durable address. The durable, non-legacy URL is the gateway-fronted
        // {space}.ikonai.app/api/… family; this relay slot is only the internal upstream.
        return new UrlReadout(UrlStability.Ephemeral, UrlShape.RelaySlot, false);
    }

    private static string DescribeShape(UrlShape shape) => shape switch
    {
        UrlShape.CleanApi => "clean /api path (app-declared)",
        UrlShape.LegacyGateway => "legacy /ikon/webhook default",
        UrlShape.LocalWebhook => "local /webhook tunnel route",
        UrlShape.RelaySlot => "relay slot (internal upstream)",
        _ => "not registered",
    };

    private static string DescribeStability(UrlStability s) => s switch
    {
        UrlStability.SpaceStable => "space-stable",
        UrlStability.PortStable => "port-stable",
        _ => "ephemeral",
    };

    private void RenderStabilityPill(UIView view, UrlStability s)
    {
        switch (s)
        {
            case UrlStability.SpaceStable:
                view.Text([Text.Caption, "font-mono text-emerald-400"], "● space-stable");
                break;
            case UrlStability.PortStable:
                view.Text([Text.Caption, "font-mono text-emerald-400"], "● port-stable");
                break;
            default:
                view.Text([Text.Caption, "font-mono text-amber-400"], "● ephemeral");
                break;
        }
    }

    // Prints one external URL split into its parts (base+path, identity query, signed token) with a
    // stability + shape readout above it. This is the heart of the "better URL printing" ask: the
    // raw string alone never told you whether it was durable or which routing shape it took.
    private void RenderUrlDiagnostics(UIView view, string? url, bool isRelay)
    {
        var readout = isRelay ? ClassifyRelayUrl(url) : ClassifyWebhookUrl(url);

        view.Box(["rounded border border-secondary bg-surface p-3 mt-2"], content: v =>
        {
            v.Row(["flex flex-wrap items-center gap-3 mb-2"], content: r =>
            {
                RenderStabilityPill(r, readout.Stability);
                r.Text([Text.Caption, "font-mono text-muted-foreground"], DescribeShape(readout.Shape));

                if (readout.HasSignedToken)
                {
                    r.Text([Text.Caption, "font-mono text-amber-400"], "● _st signed");
                }
            });

            if (string.IsNullOrEmpty(url))
            {
                v.Text([Text.Caption, "italic"], "(not registered yet — deploy to the cloud to mint this URL)");
                return;
            }

            int qIdx = url.IndexOf('?');
            string head = qIdx < 0 ? url : url[..qIdx];
            string query = qIdx < 0 ? string.Empty : url[(qIdx + 1)..];

            v.Text([Text.Caption, "font-mono break-all select-all"], head);

            if (!string.IsNullOrEmpty(query))
            {
                v.Text([Text.Caption, "mt-2 text-muted-foreground"], "query (identity + signed token):");

                foreach (var part in query.Split('&'))
                {
                    int eq = part.IndexOf('=');
                    string key = eq < 0 ? part : part[..eq];
                    string val = eq < 0 ? string.Empty : part[(eq + 1)..];

                    if (key == "_st")
                    {
                        v.Text([Text.Caption, "font-mono break-all text-amber-400"],
                            $"  _st = <signed JWT, {val.Length} chars — offline-verified, identity baked in>");
                    }
                    else
                    {
                        v.Text([Text.Caption, "font-mono break-all"], $"  {key} = {val}");
                    }
                }
            }
        });
    }

    // A banner that makes the run mode explicit, because the URL shape you get is decided by it:
    // local dev always emits /webhook tunnel URLs; only the cloud emits the space-domain family.
    private void RenderRunModeBanner(UIView view)
    {
        var runType = app.GlobalState.ServerRunType;
        bool isLocal = runType == ServerRunType.Local;

        view.Box(["rounded border border-secondary p-4"], content: v =>
        {
            v.Row(["flex flex-wrap items-center gap-3"], content: r =>
            {
                if (isLocal)
                {
                    r.Text([Text.BodyStrong, "text-amber-400"], "Run mode: LOCAL");
                }
                else
                {
                    r.Text([Text.BodyStrong, "text-emerald-400"], $"Run mode: {runType}");
                }
            });

            v.Text([Text.Caption, "mt-2"], isLocal
                ? "Endpoint URLs below resolve to the local tunnel as /webhook/{name} and are " +
                  "ephemeral — the tunnel host changes per run. The clean /api space-domain shape is " +
                  "only emitted in the cloud."
                : "Endpoint URLs below resolve under the space domain. Endpoints with a declared path " +
                  "get the clean /api shape; the rest fall back to the legacy /ikon/webhook default. " +
                  "Both are space-stable and carry a signed _st token.");

            string channelUrl = app.GlobalState.ChannelUrl;

            if (!string.IsNullOrEmpty(channelUrl))
            {
                v.Text([Text.Caption, "mt-2 font-mono text-muted-foreground break-all"],
                    $"ChannelUrl = {channelUrl}");
            }
        });
    }

    // What decides which instance each request lands on — stated concretely for the two cells on
    // this page (with their live values) rather than as an abstract table, so it reads as "here's
    // what's happening" instead of spec.
    private enum IdentitySource
    {
        Verified,      // proven by a logged-in client or a cryptographically signed token
        FromRequest,   // taken from caller-supplied query / path / claims — not independently verified
        Fixed,         // no identity at all → one shared, deterministic instance
    }

    private void RenderIdentityConstruction(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: v =>
        {
            v.Text([Text.H2, "mb-1"], "Which instance a request lands on");
            v.Text([Text.Caption, "mb-3"],
                "Concretely, for the two cells on this page — what picks the instance each request hits:");

            v.Box(["flex flex-col gap-3"], content: rows =>
            {
                RenderIdentityRow(rows, $"This app — UserId {app.SessionIdentity.UserId}",
                    "From your login when you connected; carried on every request and fixed for the session. " +
                    "It selects which app instance serves you — the same one rendering the page above.",
                    IdentitySource.Verified);

                RenderIdentityRow(rows, $"LabCell endpoint — keyed by ?Workspace (now: {_labWorkspace.Value})",
                    $"The Workspace value in the URL picks the instance, so right now the endpoint hits the " +
                    $"\"{_labWorkspace.Value}\" counter. Switch the workspace above and the same URL targets a different one. " +
                    "It runs in this app instance (above), so its counter matches the one shown.",
                    IdentitySource.FromRequest);

                RenderIdentityRow(rows, "GlobalLabCell endpoint — no key",
                    "No identity fields, so every call from anyone lands on the one shared instance, served by its " +
                    "own cell-host (which the SDK subscription and the HTTP endpoints both reach).",
                    IdentitySource.Fixed);
            });
        });
    }

    private void RenderIdentityRow(UIView view, string surface, string how, IdentitySource source)
    {
        view.Box(["rounded border border-secondary p-3"], content: v =>
        {
            v.Row(["flex flex-wrap items-center gap-3 mb-1"], content: r =>
            {
                r.Text([Text.BodyStrong], surface);
                RenderSourcePill(r, source);
            });
            v.Text([Text.Caption], how);
        });
    }

    private void RenderSourcePill(UIView view, IdentitySource source)
    {
        // One literal style array per bucket so Crosswind sees the class tokens at build time
        // (interpolated class strings never emit).
        switch (source)
        {
            case IdentitySource.Verified:
                view.Text([Text.Caption, "font-mono text-emerald-400"], "● verified");
                break;
            case IdentitySource.Fixed:
                view.Text([Text.Caption, "font-mono text-muted-foreground"], "● one shared instance");
                break;
            default:
                view.Text([Text.Caption, "font-mono text-amber-400"], "● from the request");
                break;
        }
    }
}
