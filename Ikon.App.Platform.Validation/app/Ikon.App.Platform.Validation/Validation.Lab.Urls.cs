using System.Text;
using System.Text.Json;
using Ikon.App.Cells;

public partial class Validation
{
    // How durable a given external URL is. The /api space-domain family never names a physical location
    // (identity rides a signed grant + open path captures), so it survives reprovisioning and relay
    // restarts; a relay-slot or local-dev tunnel URL can change host/port on the next start.
    private enum UrlStability
    {
        SpaceStable,
        Ephemeral,
    }

    private void RenderStabilityPill(UIView view, UrlStability s)
    {
        if (s == UrlStability.SpaceStable)
        {
            view.Text([Text.Caption, "font-mono text-emerald-400"], "● space-stable");
        }
        else
        {
            view.Text([Text.Caption, "font-mono text-amber-400"], "● ephemeral");
        }
    }

    // Prints one external URL split into base path + query, with the signed grant (if present) decoded
    // into its claims. The grant is the single addressing/authorization artifact on the /api surface, so
    // showing its pinned identity + endpoint scope is the heart of "which instance, and may you call it".
    private void RenderUrlDiagnostics(UIView view, string? url, bool isRelay)
    {
        var stability = ClassifyStability(url, isRelay);

        view.Box(["rounded border border-secondary bg-surface p-3 mt-2"], content: v =>
        {
            v.Row(["flex flex-wrap items-center gap-3 mb-2"], content: r =>
            {
                RenderStabilityPill(r, stability);

                if (!string.IsNullOrEmpty(url) && url.Contains("/api/", StringComparison.Ordinal))
                {
                    r.Text([Text.Caption, "font-mono text-muted-foreground"], "clean /api path (space domain)");
                }
            });

            if (string.IsNullOrEmpty(url))
            {
                v.Text([Text.Caption, "italic"], "(no URL yet — deploy to the cloud or run locally, then mint)");
                return;
            }

            int qIdx = url.IndexOf('?');
            string head = qIdx < 0 ? url : url[..qIdx];
            string query = qIdx < 0 ? string.Empty : url[(qIdx + 1)..];

            v.Text([Text.Caption, "font-mono break-all select-all"], head);

            string? grantToken = null;
            var others = new List<(string Key, string Value)>();

            if (!string.IsNullOrEmpty(query))
            {
                foreach (var part in query.Split('&'))
                {
                    int eq = part.IndexOf('=');
                    string key = eq < 0 ? part : part[..eq];
                    string val = eq < 0 ? string.Empty : part[(eq + 1)..];

                    if (key == "ikon-grant")
                    {
                        grantToken = val;
                    }
                    else
                    {
                        others.Add((key, val));
                    }
                }
            }

            if (others.Count > 0)
            {
                v.Text([Text.Caption, "mt-2 text-muted-foreground"], "open query params (caller-supplied, never identity-bearing):");

                foreach (var (key, val) in others)
                {
                    v.Text([Text.Caption, "font-mono break-all"], $"  {key} = {val}");
                }
            }

            if (grantToken is null)
            {
                v.Box(["rounded border border-secondary p-2 mt-2"], content: g =>
                {
                    g.Text([Text.Caption, "font-mono text-muted-foreground"], "● no ikon-grant");
                    g.Text([Text.Caption, "mt-1"],
                        "Bare URL — callable as-is. This is a public endpoint, or one behind a /router/ " +
                        "policy (apiKey/hmac/ipAllow) where the caller sends its own credential. A grant " +
                        "endpoint would 401 here until you mint a working URL with app.MintUrl.");
                });
                return;
            }

            RenderGrantClaims(v, grantToken);
        });
    }

    private static UrlStability ClassifyStability(string? url, bool isRelay)
    {
        if (string.IsNullOrEmpty(url) || isRelay)
        {
            return UrlStability.Ephemeral;
        }

        return url.Contains("/api/", StringComparison.Ordinal) ? UrlStability.SpaceStable : UrlStability.Ephemeral;
    }

    // Decode the base64url(payload).signature grant and show its claims. The payload is signed, NOT
    // encrypted (it's an identity binding the gateway verifies offline) — so decoding it client-side to
    // explain what the URL addresses and authorizes is expected, not a leak.
    private void RenderGrantClaims(UIView view, string token)
    {
        view.Box(["rounded border border-secondary bg-surface p-2 mt-2"], content: g =>
        {
            g.Text([Text.Caption, "font-mono text-emerald-400"], "● signed ikon-grant");

            using var doc = TryParseGrant(token);

            if (doc is null)
            {
                g.Text([Text.Caption, "font-mono break-all text-amber-400 mt-1"],
                    $"  <grant, {token.Length} chars — payload could not be decoded>");
                return;
            }

            var root = doc.RootElement;

            g.Text([Text.Caption, "mt-1 mb-1"], "claims (signed + tamper-evident, not encrypted):");
            RenderClaim(g, "endpoint", GetString(root, "endpointName") ?? "(any)");
            RenderClaim(g, "pinned identity", DescribeIdentity(root));
            RenderClaim(g, "local-instance",
                GetString(root, "localInstanceId") is { } li ? $"{li} — routes to a local-dev run" : "(cloud — no li)");
            RenderClaim(g, "expires", GetString(root, "expiresAt") ?? "never (retire by revocation)");
            RenderClaim(g, "group", GetString(root, "group") ?? "(none)");
            RenderClaim(g, "grant id", Shorten(GetString(root, "id")));
        });
    }

    private void RenderClaim(UIView view, string key, string value)
    {
        view.Row(["flex flex-wrap items-baseline gap-2"], content: r =>
        {
            r.Text([Text.Caption, "font-mono text-muted-foreground"], $"  {key}:");
            r.Text([Text.Caption, "font-mono break-all"], value);
        });
    }

    private static JsonDocument? TryParseGrant(string token)
    {
        try
        {
            int dot = token.LastIndexOf('.');
            string encoded = dot < 0 ? token : token.Substring(0, dot);
            string b64 = encoded.Replace('-', '+').Replace('_', '/');
            b64 += (b64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
            var bytes = Convert.FromBase64String(b64);
            return JsonDocument.Parse(Encoding.UTF8.GetString(bytes));
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static string DescribeIdentity(JsonElement root)
    {
        if (!root.TryGetProperty("sessionIdentity", out var si) || si.ValueKind != JsonValueKind.Object)
        {
            return "(none — fully open, the caller fills every field)";
        }

        var parts = new List<string>();

        foreach (var p in si.EnumerateObject())
        {
            parts.Add($"{p.Name}={p.Value}");
        }

        return parts.Count == 0 ? "(none — fully open, the caller fills every field)" : string.Join(", ", parts);
    }

    private static string Shorten(string? id)
        => string.IsNullOrEmpty(id) ? "(none)" : (id.Length <= 16 ? id : id[..16] + "…");

    // A banner that makes the run mode explicit, because the URL shape you get is decided by it:
    // local dev routes grant URLs to this process via their local-instance id; the cloud cold-starts
    // (or routes to) the instance the grant's pinned identity names.
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
                ? "Endpoint URLs below resolve to your local-dev run. A minted grant carries a " +
                  "local-instance id (li) so it routes to THIS process; a bare public URL reaches it too " +
                  "while this is the only local run."
                : "Endpoint URLs below resolve under the space domain and are space-stable. Each is a bare " +
                  "address: a grant endpoint needs a minted ?ikon-grant=; a public endpoint is callable as-is.");

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
        Verified,      // proven by a logged-in client or pinned into a signed grant
        FromRequest,   // taken from caller-supplied query / path captures — not independently verified
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

                RenderIdentityRow(rows, $"LabCell endpoint — keyed by the {{workspace}} path (now: {_labWorkspace.Value})",
                    $"The Workspace value in the URL path picks the instance, so right now the endpoint hits the " +
                    $"\"{_labWorkspace.Value}\" counter. On the public endpoint it's an OPEN capture the caller fills; " +
                    "on the grant endpoint it's PINNED into the grant at mint time (unforgeable). Switch the workspace " +
                    "above and the same surface targets a different one.",
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
