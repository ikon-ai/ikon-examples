namespace Ikon.App.Patterns.Examples;

// The app files guide, as code that compiles.
//
// The holder is the one file the guide reads as — the shared names are its fields, and each
// `#region example:` is one fence that ExamplePinner regenerates into the guide.
// A published guide keeps literal code in its fence, so the marker sits above it rather than
// replacing it.

file sealed class AppFilesExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task RunAsync(string id, byte[] bytes)
    {
        #region example:app-files
        // Read a shipped (or previously written) private file.
        var rules = await app.Files.Data.ReadTextAsync("rules.md");

        // Store a generated image and get the URL to show it.
        await app.Files.Public.WriteBytesAsync($"thumbnails/{id}.png", bytes, "image/png");
        var url = await app.Files.Public.GetUrlAsync($"thumbnails/{id}.png");
        #endregion

        Log.Instance.Debug($"{rules} {url}");
    }
}
