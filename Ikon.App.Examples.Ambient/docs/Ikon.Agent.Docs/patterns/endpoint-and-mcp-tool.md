# Endpoint and MCP Tool — Three Shapes, Three Authorizations

An Ikon app exposes HTTP by putting an attribute on a method. The part that goes wrong is not the routing — it is the authorization, because all three shapes look identical in the source and mean completely different things.

| Shape | Attribute | Who may call it |
|---|---|---|
| App API | `[HttpPost("/p")]` | Only a URL carrying a valid signed grant — **the default** |
| Third-party webhook | `[HttpPost("/p", Auth = EndpointAuth.Public)]` | Anyone; the request signature is the authorization |
| Agent tool | `[Mcp(Name = "…")]` | An LLM, through the JSON-RPC multiplexer or its own POST route |

## When to use

Any surface reached from outside the app's own clients: a webhook, an integration, a share link, a tool an agent calls.

## Snippet

```csharp
/// The bound body record must be PUBLIC — a public handler cannot take a less accessible
/// parameter type (CS0051), the same rule that governs SessionIdentity and ClientParameters.
public sealed record SumRequest(int A, int B);

/// The DEFAULT is EndpointAuth.Grant: the bare URL is rejected 401 at the gateway before the
/// handler runs. That is the right default — an app endpoint is not a public API by accident.
[HttpPost("/patterns/sum")]
public HttpResult Sum(SumRequest request) => HttpResult.Ok(new { sum = request.A + request.B });

/// A third-party webhook MUST be Public — a provider calls a fixed URL and cannot carry a grant.
/// Public does not mean unauthorized: the signature IS the authorization, so verify it here.
[HttpPost("/patterns/webhook", Auth = EndpointAuth.Public)]
public async Task<HttpResult> Webhook(Ikon.App.HttpRequest request)
{
    if (!VerifySignature(request.Headers["X-Signature"], request.Body))
    {
        return HttpResult.Unauthorized();
    }

    await RecordPaymentAsync(request.Body);

    // 200 even on a skip. A 4xx or 5xx here buys a provider retry storm for something you
    // already decided to ignore.
    return HttpResult.Ok();
}

/// An MCP tool. The JSON Schema is reflected from the signature, so the parameter names ARE the
/// tool's contract — name them the way you would want an LLM to read them.
[Mcp(Name = "sum_numbers", Description = "Adds two integers and returns the total")]
public int SumNumbers(int a, int b) => a + b;

/// A granted endpoint's PublicUrl is a bare address with no grant, so it is not callable as it
/// stands. Minting is the ONLY way to get a working URL, in the cloud and in local dev alike.
private async Task ShareAsync(string documentId)
{
    MintedUrl minted = await app.MintUrlAsync(nameof(Sum), new { DocumentId = documentId });
    _shareUrl.Value = minted.Url;
}

private void RenderShareLink(IView view)
{
    view.Row([Card.Default, "items-center gap-2 p-3"], content: view =>
    {
        view.Text([Text.Caption, "flex-1 min-w-0 truncate"],
            text: _shareUrl.Value ?? "No link minted yet");

        view.ActionButton([Button.OutlineMd], action: ActionKind.CopyToClipboard,
            options: new CopyToClipboardActionOptions { Text = _shareUrl.Value ?? "" },
            props: new Dictionary<string, object> { ["aria-label"] = "Copy share link" },
            content: v => v.Icon([Icon.Sm], name: "copy"));
    });
}
```

## Notes

- **`EndpointAuth.Grant` is the default and is the right one.** The gateway rejects a bare URL with 401 *before* the handler runs, so an app endpoint is never a public API by accident. `Public` is anonymous; `Deny` always rejects.
- **A webhook has to be `Public`, and that is not the same as unauthorized.** A provider like Stripe calls a fixed URL it cannot attach a grant to, so the signature header over the raw body is the authorization — verify it in the handler and return `HttpResult.Unauthorized()` when it fails.
- **Return 200 even when you skip the work.** A 4xx or 5xx to a webhook provider buys a retry storm for something you already decided to ignore.
- The handler binds **one optional typed body** plus host-injected context (`Ikon.App.HttpRequest`, `HttpCallContext`, `CancellationToken`) in any order. Zero non-injected parameters means no body.
- The bound record must be **public** — a public handler cannot take a less accessible parameter type (CS0051). Same rule that governs `SessionIdentity` and `ClientParameters`.
- Return a value for JSON, a `string` for `text/plain`, or an `HttpResult` when you need the status code.
- **A granted endpoint's `PublicUrl` is not callable.** `app.Endpoints` lists every `EndpointInfo`, but each address is bare. `MintUrlAsync` is the only way to get a working URL — in the cloud *and* in local dev — and it returns a `MintedUrl` whose grant can pin a resource identity (`new { DocumentId = "doc-42" }`) or, with the identity omitted, this instance's own.
- `[Mcp]` reflects its JSON Schema from the C# signature, so **the parameter names are the tool's contract** — name them the way you want an LLM to read them. Each tool is reachable both through the shared JSON-RPC endpoint and at its own POST route. Pair with `[McpResource]` for resources.
- `AuthPolicy = "name"` names a custom edge policy in `router/index.ts` and wins over `Auth` when both are set. On a policy endpoint a grant in the URL is address-only — it picks the instance, the policy authorizes.
- On an `External` function (rather than an HTTP attribute), `[AllowAnonymous]` marks the same intent: a pure marker that documents "authorized by something other than session auth" and silences the startup audit warning for an `External` function with no auth policy. It grants nothing by itself — pair it with an explicit `[RateLimit]` wherever abuse is plausible.

## See also

- `notify-across-channels` — the outbound half, when the app is the one reaching out.
- `copy-and-share-action-row` — putting a minted URL in front of someone.
- `virtual-file-tool-set` — a richer tool surface for an agent than a single `[Mcp]` method.
