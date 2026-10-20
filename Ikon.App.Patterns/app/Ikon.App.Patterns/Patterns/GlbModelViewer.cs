using System.Runtime.CompilerServices;

namespace Ikon.App.Patterns.Patterns;

#region example:pattern-glb-model-viewer-extension
public static class ModelViewerExtensions
{
    public static void ModelViewer(this UIView view, string src, string? alt = null, bool autoRotate = true, string[]? style = null, string? key = null,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "", [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        view.AddNode("model-viewer", new Dictionary<string, object?>
        {
            ["src"] = src,
            ["alt"] = alt,
            ["autoRotate"] = autoRotate,
        }, style: style, key: key, file: file, line: line);
    }
}
#endregion

// Pattern: glb-model-viewer — see docs/patterns/glb-model-viewer.md.
// The demo shows its note: the model-viewer node needs a React component this gallery's frontend
// does not register, and a generated model needs a running app's file storage.
internal sealed class GlbModelViewer(IAppBase app) : IPatternDemo
{
    public string Slug => "glb-model-viewer";
    public string Title => "GLB model viewer";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Generates a 3D model with MeshGenerator, keeps its GLB in the app's public files and shows it in a <model-viewer> custom node the user can rotate and zoom. See docs/patterns/glb-model-viewer.md.");

    #region example:pattern-glb-model-viewer
    private readonly Reactive<string?> _modelUrl = new(null);
    private readonly Reactive<bool> _generating = new(false);
    private readonly Reactive<string?> _error = new(null);

    private async Task GenerateModelAsync(string prompt)
    {
        _generating.Value = true;
        _error.Value = null;

        try
        {
            var mesh = await MeshGenerator.GenerateAsync(prompt);

            if (mesh.GlbUrl is null)
            {
                _error.Value = "The model came back without a GLB file.";
                return;
            }

            // The signed URL expires after about three days, so the app keeps its own copy and
            // shows that: a public file's URL is a path on the app's own origin.
            var path = $"models/{Guid.NewGuid():N}.glb";
            await app.Files.Public.WriteFromUrlAsync(path, mesh.GlbUrl, "model/gltf-binary");
            _modelUrl.Value = await app.Files.Public.GetUrlAsync(path);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Model generation for '{prompt}' failed, the user can try again: {ex.Message}");
            _error.Value = "Couldn't make that model. Try again.";
        }
        finally
        {
            _generating.Value = false;
        }
    }

    private void RenderModel(UIView view)
    {
        view.Column([Card.Default, "p-4 gap-3"], content: view =>
        {
            if (_modelUrl.Value is { } url)
            {
                view.ModelViewer(src: url, alt: "The generated model", style: ["w-full h-96 rounded-lg bg-card"]);
            }
            else
            {
                view.EmptyState([EmptyState.RootSm], title: _generating.Value ? "Making the model" : "No model yet", description: _generating.Value ? "This takes a few minutes." : "Generate one to see it here.");
            }

            if (_error.Value is { } error)
            {
                view.Text([Text.Caption, "text-error-primary"], error);
            }

            view.Button([Button.PrimaryMd, "self-start"], text: "Make a treasure chest", disabled: _generating.Value,
                onClick: async () => await GenerateModelAsync("A small wooden treasure chest with brass fittings"));
        });
    }
    #endregion
}
