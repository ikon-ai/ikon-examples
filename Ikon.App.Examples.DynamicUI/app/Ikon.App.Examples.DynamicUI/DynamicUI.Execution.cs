using System.Reflection;
using Ikon.Common.Core.Reactive;
using Ikon.Parallax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class UIScriptGlobals
{
    public UIView view { get; }
    public SharedState state { get; }

    public UIScriptGlobals(UIView view, SharedState sharedState)
    {
        this.view = view;
        state = sharedState;
    }
}

public class SharedState
{
    private readonly Dictionary<string, object> _state = new();

    public T GetOrCreate<T>(string key, Func<T> factory) where T : class
    {
        if (_state.TryGetValue(key, out var existing) && existing is T typed)
        {
            return typed;
        }

        var newValue = factory();
        _state[key] = newValue!;
        return newValue;
    }

    public void Set<T>(string key, T value) where T : class
    {
        _state[key] = value!;
    }

    public T? Get<T>(string key) where T : class
    {
        if (_state.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }
        return null;
    }

    public void Clear()
    {
        _state.Clear();
    }
}

public partial class DynamicUI
{
    private static ScriptOptions? _cachedScriptOptions;

    private static ScriptOptions CreateScriptOptions()
    {
        if (_cachedScriptOptions != null)
        {
            return _cachedScriptOptions;
        }

        // Get all assemblies we need
        var assemblies = new[]
        {
            typeof(DynamicUI).Assembly,
            typeof(Reactive<>).Assembly,
            typeof(UIView).Assembly,
            typeof(List<>).Assembly,
            typeof(Enumerable).Assembly,
            typeof(object).Assembly
        };

        // Create metadata references only from assemblies with valid locations
        var references = new List<MetadataReference>();

        foreach (var assembly in assemblies)
        {
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        // Also add referenced assemblies that have locations
        foreach (var assembly in assemblies)
        {
            foreach (var referencedName in assembly.GetReferencedAssemblies())
            {
                try
                {
                    var referencedAssembly = Assembly.Load(referencedName);
                    if (!string.IsNullOrEmpty(referencedAssembly.Location))
                    {
                        var reference = MetadataReference.CreateFromFile(referencedAssembly.Location);
                        if (!references.Any(r => r.Display == reference.Display))
                        {
                            references.Add(reference);
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                }
            }
        }

        _cachedScriptOptions = ScriptOptions.Default
            .AddReferences(references)
            .AddImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Threading.Tasks",
                "Ikon.Common.Core.Reactive",
                "Ikon.Parallax",
                "Ikon.Parallax.Components.Standard",
                "Ikon.Parallax.Components.Charts",
                "Ikon.Parallax.Themes.Default");

        return _cachedScriptOptions;
    }

    private (bool Success, string? Error) ExecuteCodeSync(string code, UIView uiView)
    {
        try
        {
            var options = CreateScriptOptions();
            var globals = new UIScriptGlobals(uiView, _sharedState);

            // Execute synchronously - required for UI action callbacks to work during render
            CSharpScript.RunAsync(code, options, globals).GetAwaiter().GetResult();
            return (true, null);
        }
        catch (CompilationErrorException ex)
        {
            var errors = string.Join("\n", ex.Diagnostics.Select(d => d.ToString()));
            return (false, $"Compilation error:\n{errors}");
        }
        catch (Exception ex)
        {
            return (false, $"Runtime error: {ex.Message}");
        }
    }

    private async Task<string?> ValidateSyntaxAsync(string code)
    {
        try
        {
            var options = CreateScriptOptions();
            var script = CSharpScript.Create(code, options, typeof(UIScriptGlobals));
            var diagnostics = script.Compile();

            var errors = diagnostics
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();

            if (errors.Count > 0)
            {
                return string.Join("\n", errors);
            }

            return null;
        }
        catch (Exception ex)
        {
            return $"Validation error: {ex.Message}";
        }
    }
}
