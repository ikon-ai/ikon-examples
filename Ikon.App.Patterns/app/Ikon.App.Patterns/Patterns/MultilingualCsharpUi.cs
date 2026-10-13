using System.Globalization;
using Ikon.Common.Core.Protocol;

namespace Ikon.App.Patterns.Patterns;

// Pattern: multilingual-csharp-ui — see docs/patterns/multilingual-csharp-ui.md.
// `app` is the App's primary-constructor handle. The demo draws the page with its language picker, so
// switching languages in the gallery re-renders every string.
internal sealed class MultilingualCsharpUi : IPatternDemo
{
    public string Slug => "multilingual-csharp-ui";
    public string Title => "Multilingual C# UI";
    public string Category => "Platform";
    public void RenderDemo(IView view) => RenderEventPage((UIView)view);

    #region example:pattern-multilingual-csharp-ui
    private static readonly SelectOption[] Languages =
    [
        new("en", "English"),
        new("sv", "Svenska"),
        new("fi", "Suomi"),
    ];

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["en"] = new() { ["title"] = "Your events", ["reply-by"] = "Reply by {0:D}", ["language"] = "Language" },
        ["sv"] = new() { ["title"] = "Dina evenemang", ["reply-by"] = "Svara senast {0:D}", ["language"] = "Språk" },
        ["fi"] = new() { ["title"] = "Tapahtumasi", ["reply-by"] = "Vastaa viimeistään {0:D}", ["language"] = "Kieli" },
    };

    // Per client, so two people in one shared instance each read their own language.
    private readonly ClientReactive<string> _language = new("en");

    // Call from app.OnClientJoined(async (ctx, parameters) => ChooseLanguage(ctx, parameters.Lang)):
    // ?lang= first, then the browser's language, then English.
    private void ChooseLanguage(Context ctx, string? requested) =>
        _language.SetFor(ctx.ClientSessionId, Supported(requested) ?? Supported(ctx.Locale) ?? "en");

    private static string? Supported(string? tag) =>
        string.IsNullOrEmpty(tag) ? null : Languages.Select(l => l.Value).FirstOrDefault(l => tag.StartsWith(l, StringComparison.OrdinalIgnoreCase));

    // A missing translation falls back to English, and a missing key shows the key, so a render
    // never throws. Dates and numbers format in the reader's language too.
    private string T(string key, params object[] args)
    {
        var language = _language.Value;
        var text = Strings[language].GetValueOrDefault(key) ?? Strings["en"].GetValueOrDefault(key) ?? key;
        return args.Length == 0 ? text : string.Format(CultureInfo.GetCultureInfo(language), text, args);
    }

    private void RenderEventPage(UIView view)
    {
        view.Column([Card.Default, "p-6 gap-3 max-w-md"], content: view =>
        {
            view.Row(["items-center justify-between gap-3"], content: row =>
            {
                row.Text([Text.H3], T("title"));
                row.Select(options: Languages, value: _language.Value, ariaLabel: T("language"),
                    onValueChange: async language => _language.Value = language);
            });

            view.Text([Text.Body], T("reply-by", new DateTime(2026, 11, 20)));
        });
    }
    #endregion
}

internal static class MultilingualCsharpUiParameters
{
    #region example:pattern-multilingual-csharp-ui-parameters
    // ?lang=sv fills Lang: a query parameter binds to the ClientParameters property of the same name.
    public record ClientParameters(string Name = "", string? Lang = null);
    #endregion
}
