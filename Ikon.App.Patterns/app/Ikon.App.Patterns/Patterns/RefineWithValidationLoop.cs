namespace Ikon.App.Patterns.Patterns;

using Microsoft.CodeAnalysis.Scripting;

// Pattern: refine-with-validation-loop — see docs/patterns/refine-with-validation-loop.md.
// The docsnippet region is the auto-correcting generate/compile/re-prompt loop; the stubs outside it
// stand in for the caller's structured-output shape, its prompt corpus and its Roslyn script host.
internal sealed class RefineWithValidationLoop : IPatternDemo
{
    public string Slug => "refine-with-validation-loop";
    public string Title => "Refine with validation loop";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend AI pattern with no standalone UI: Emerge.Refine generates code, compiles it with Roslyn, and re-prompts on errors until it validates. See the source and docs/patterns/refine-with-validation-loop.md.");

    private sealed record UICodeResponse
    {
        public string Code { get; init; } = "";
    }

    private string GetUIDocumentation() => throw new NotImplementedException();

    // App-local: the globals object handed to the script. Roslyn scripting itself (CSharpScript,
    // ScriptOptions, DiagnosticSeverity) is the real Microsoft.CodeAnalysis.CSharp.Scripting API.
    private sealed class UIScriptGlobals
    {
        public UIView View = null!;
    }

    private static ScriptOptions CreateScriptOptions() => throw new NotImplementedException();

    #region docsnippet:pattern-refine-with-validation-loop
    private const int MaxAutoRetries = 2;

    private async Task<(UICodeResponse Response, string? ValidationError)> GenerateUIWithRefinementAsync(
        string description,
        string? previousCode = null,
        string? initialError = null)
    {
        string? validationError = initialError;

        var response = await Emerge.Refine<UICodeResponse>(LLMModel.Claude45Sonnet, new KernelContext(), opt =>
        {
            opt.MaxRefinements = MaxAutoRetries;
            opt.Temperature = 0.3f;
            opt.SystemPrompt = $"""
                You are an expert UI code generator for Ikon Parallax.
                ## UI Framework Documentation
                {GetUIDocumentation()}
                Generate ONLY the C# code body that goes inside a UI building function.
                """;

            opt.Initial(s =>
            {
                s.Command = !string.IsNullOrEmpty(initialError)
                    ? $"## User Request\n{description}\n\nIMPORTANT: previous code had this error:\n{initialError}\n\nPrevious code:\n```csharp\n{previousCode}\n```\nFix the error."
                    : !string.IsNullOrEmpty(previousCode)
                        ? $"## User Request\n{description}\n\nPrevious code (iterate):\n```csharp\n{previousCode}\n```"
                        : $"## User Request\n{description}\n\nGenerate the UI code.";
            });

            opt.Refinement(s =>
            {
                s.Command = $"""
                    ## User Request
                    {description}

                    IMPORTANT: Your previous code had a compilation error:
                    {validationError}

                    Please fix the error and generate corrected code.
                    """;
            });

            opt.ShouldContinue = async (result, _) =>
            {
                validationError = await ValidateSyntaxAsync(result.Code);
                return validationError != null;     // true == do another refinement
            };
        });

        return (response, validationError);
    }

    private async Task<string?> ValidateSyntaxAsync(string code)
    {
        var script = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create(code, CreateScriptOptions(), typeof(UIScriptGlobals));
        var diagnostics = script.Compile();
        var errors = diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.ToString()).ToList();
        return errors.Count > 0 ? string.Join("\n", errors) : null;
    }
    #endregion
}
