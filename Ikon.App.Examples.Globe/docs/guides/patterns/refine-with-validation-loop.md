<!-- mined-from: Ikon.App.Examples.DynamicUI -->
# Refine With Validation Loop — LLM Self-Fixes Compile Errors

`Emerge.Refine<T>` runs an initial pass, then re-prompts up to N times — but `opt.ShouldContinue` lets *you* decide whether to stop. By compiling the LLM's code (or running any other validator) inside `ShouldContinue` and feeding the diagnostic back into the next refinement prompt, you get an auto-correcting code generator that only stops when the output passes the check or the iteration cap is hit.

## When to use

Any structured-output use case where there's a real validator: code generation (compile), JSON-against-schema, SQL (parse + EXPLAIN), regex (compile), structured plans against type checkers. The LLM doesn't always need a human in the loop — give it the failure and let it try again.

## Snippet

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

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
    var script = CSharpScript.Create(code, CreateScriptOptions(), typeof(UIScriptGlobals));
    var diagnostics = script.Compile();
    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToList();
    return errors.Count > 0 ? string.Join("\n", errors) : null;
}
```

## Notes

- `ShouldContinue` returns `true` to keep going; capture the validator output in a closed-over local so the next `Refinement` prompt sees the freshest error.
- Surface the *final* error (after all retries exhaust) to the user along with a manual "Retry Fix" button — sometimes a third attempt with extra context helps.
- For non-code domains: validators can be `JsonSchema.Validate`, `Regex.IsMatch`, or even another LLM ("does this answer the question?").
- Keep `MaxRefinements` low (2-3) — the model rarely fixes what it couldn't on attempt 2.

## See also

- `nl-to-csharp-script-execution`
- `plan-then-code-iteration`
- `retry-with-status-text`
