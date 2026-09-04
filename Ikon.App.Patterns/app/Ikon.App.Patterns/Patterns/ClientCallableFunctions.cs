using Ikon.Common.Core.Functions;

namespace Ikon.App.Patterns.Patterns;

// Pattern: client-callable-functions — see docs/patterns/client-callable-functions.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ClientCallableFunctions : IPatternDemo
{
    public string Slug => "client-callable-functions";
    public string Title => "Functions a client can call";
    public string Category => "App structure";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-client-callable-functions
    // Every method here is advertised over the protocol, because [RegisterAll] sets the default and
    // each [Function] inherits it. Without the class attribute a bare [Function] is Local: callable
    // in-process and invisible to a client, which is the quiet version of "my endpoint 404s".
    [RegisterAll(Visibility = FunctionVisibility.External)]
    [RequireLogin]
    private sealed class CatalogFunctions(Reactive<int> viewCount)
    {
        [Function("Look a product up by its SKU")]
        public string Describe(string sku) => $"Product {sku}";

        // The name a client calls is the full type name plus the method name unless Name is set.
        // Set it: the default embeds the namespace, so moving the class renames the endpoint.
        [Function(Name = "catalog.views", Description = "How many times the catalog was opened")]
        public int Views() => viewCount.Value;
    }

    private readonly Reactive<int> _viewCount = new(0);
    private readonly Reactive<string?> _registered = new(null);

    private void Register()
    {
        // RegisterFromInstance for instance methods — the closure over _viewCount is the point;
        // RegisterFromType(typeof(X)) is the static-only equivalent and needs no instance.
        FunctionRegistry.Instance.RegisterFromInstance(new CatalogFunctions(_viewCount));

        // The registry is readable, which is how an app shows a client what it may call rather than
        // maintaining a second hand-written list that drifts.
        var external = FunctionRegistry.Instance.Functions
            // Keyed by NAME, and each entry holds every registered version of it.
            .SelectMany(entry => entry.Value)
            .Where(f => f.Visibility == FunctionVisibility.External)
            .Select(f => $"{f.Name}({string.Join(", ", f.Parameters.Select(p => p.Name))})")
            .Order(StringComparer.Ordinal)
            .ToList();

        _registered.Value = string.Join("  ·  ", external);
    }
    #endregion

    private void Render(IView view)
    {
        view.Column([Layout.Column.Md], column =>
        {
            column.Text([Text.H3], text: "Client-callable functions");
            column.Button([Button.PrimarySm, "self-start"], text: "Register the catalog",
                onClick: () => Register());

            if (_registered.Value is { } names)
            {
                column.Text([Text.Muted, "text-xs"], text: names);
            }
        });
    }
}
