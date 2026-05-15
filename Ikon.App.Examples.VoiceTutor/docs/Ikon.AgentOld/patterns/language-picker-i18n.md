<!-- mined-from: RailGo -->
# Language Picker — Header `Select` + `T()` Helper Through The App

Multilingual apps need (1) a language reactive, (2) a `T(key)` helper that picks the localized string, and (3) a header `Select` that flips the reactive. Because every render reads `_language.Value`, switching language re-streams just the changed strings — no page reload, no route param.

## When to use

Apps with a wide audience where users will pick their preferred language at runtime. Pair with `ClientReactive<AppLanguage>` for per-client (everyone sees their own); use `Reactive` for whole-app language switches.

## Snippet

```csharp
public enum AppLanguage { FI, EN, SV, DE, JA }

private readonly ClientReactive<AppLanguage> _language = new(AppLanguage.EN);

private static readonly Dictionary<AppLanguage, string> LanguageCodes = new()
{
    [AppLanguage.FI] = "fi", [AppLanguage.EN] = "en", [AppLanguage.SV] = "sv",
    [AppLanguage.DE] = "de", [AppLanguage.JA] = "ja",
};

private static readonly Dictionary<AppLanguage, string> LanguageNames = new()
{
    [AppLanguage.FI] = "Suomi", [AppLanguage.EN] = "English", [AppLanguage.SV] = "Svenska",
    [AppLanguage.DE] = "Deutsch", [AppLanguage.JA] = "日本語",
};

// Lookup a string in the active language; English is the source-of-truth key
private string T(string englishKey) =>
    Translations.TryGetValue((_language.Value, englishKey), out var s) ? s : englishKey;

// In the header
actions.Select(
    triggerStyle: [
        $"h-9 px-3 rounded-full border border-[{GoldDeep}]/35 hover:bg-[{Gold}]/10 transition-colors",
        "text-[11px] font-semibold tracking-[0.14em] uppercase text-[#5a5958] flex items-center gap-1.5"
    ],
    contentStyle: ["rounded-lg border border-[#ecc57c]/35 bg-white p-1 min-w-[140px]"],
    itemStyle: ["px-3 py-2 rounded-md text-sm cursor-pointer hover:bg-[#f2ead9]"],
    value: LanguageCodes[_language.Value],
    options: [
        new("fi", LanguageNames[AppLanguage.FI]),
        new("en", LanguageNames[AppLanguage.EN]),
        new("sv", LanguageNames[AppLanguage.SV]),
        new("de", LanguageNames[AppLanguage.DE]),
        new("ja", LanguageNames[AppLanguage.JA]),
    ],
    onValueChange: async v =>
    {
        _language.Value = v switch
        {
            "fi" => AppLanguage.FI, "en" => AppLanguage.EN, "sv" => AppLanguage.SV,
            "de" => AppLanguage.DE, "ja" => AppLanguage.JA, _ => _language.Value,
        };
    });

// At call sites — always wrap user-facing strings
hero.Text([...], T("Travel smarter"));
hero.Text([...], T("What's on sale."));
```

## Notes

- Use the English string itself as the lookup key — keeps the C# readable when scanning. `T("Find trains")` is self-documenting.
- For fallback, return the key when missing — easy to spot untranslated strings in production.
- LLM prompts must respond in `_language.Value`'s language — pass the user's language to the system prompt; don't translate prompts. (See feedback_llm_prompt_language.)
- Keep names native (`Suomi`, not `Finnish`) — better recognition by speakers.
- `ClientReactive` not `Reactive` — different users on the same shared app keep their own language.

## See also

- `persistent-user-preferences` — promote `_language` to `PersistentUserReactive` so it follows the user across sessions
