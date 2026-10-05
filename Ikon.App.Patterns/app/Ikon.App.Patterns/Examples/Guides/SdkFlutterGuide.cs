namespace Ikon.App.Patterns.Examples;

file static class FlutterTargetsExamples
{
    public static void Render(UIView view)
    {
        #region example:flutter-target-variants
        view.Box(
            style: [
                "px-3 py-2 rounded-md",                                  // shared
                "web:(bg-background text-secondary border border-input)",// web only
                "flutter:(bg-slate-900 text-slate-100 border border-slate-700)" // Flutter only
            ],
            content: view => view.Text(text: "Adapts per target"));
        #endregion
    }
}
