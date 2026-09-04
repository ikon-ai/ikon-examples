namespace Ikon.App.Patterns.Patterns;

// Pattern: endpoint-and-mcp-tool — see docs/patterns/endpoint-and-mcp-tool.md.
// The docsnippet region is the three endpoint shapes an app actually needs: a granted one, a public
// webhook that authorizes itself, and an MCP tool. Routes are namespaced under /patterns/ so the
// gallery app cannot collide with a real app's paths.
internal sealed class EndpointAndMcpTool(IAppBase app) : IPatternDemo
{
    public string Slug => "endpoint-and-mcp-tool";
    public string Title => "Endpoint and MCP tool";
    public string Category => "Platform";
    public void RenderDemo(IView view) => RenderShareLink(view);

    private readonly Reactive<string?> _shareUrl = new(null);

    private static bool VerifySignature(string? header, string body) => false;
    private static Task RecordPaymentAsync(string body) => Task.CompletedTask;

    #region docsnippet:pattern-endpoint-and-mcp-tool
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
    #endregion
}
