namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: private-generated-media — see docs/patterns/private-generated-media.md.
// The doc quotes the example region below. The demo starts with no entries. The button generates
// the media with the gallery's mocks and writes it to the app's private files.
internal sealed class PrivateGeneratedMedia(IAppBase app) : IPatternDemo
{
    public string Slug => "private-generated-media";
    public string Title => "Private generated media";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-private-generated-media
    // The paths are what the app keeps (in a database row in a real app), never a URL: a private
    // file's URL is signed and stops working after an hour.
    private sealed record Dream(string Id, string Title, string ImagePath, string AudioPath);

    private readonly ReactiveList<Dream> _dreams = new();
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _error = new(null);
    private readonly ClientReactive<string?> _openId = new(null);

    private async Task CreateAsync(string title)
    {
        if (_busy.Value)
        {
            return;
        }

        _error.Value = null;
        using var busy = _busy.AsToken();

        try
        {
            var image = await ImageGenerator.GenerateAsync($"A soft watercolour of a dream: {title}");
            var music = await MusicGenerator.GenerateAsync($"A calm ambient soundscape for a dream about {title}");

            // app.Files.Data is the private tree: nothing in it has a public URL. One folder per
            // user keeps the owner's files together for an account deletion.
            var id = Guid.NewGuid().ToString("N");
            var folder = $"users/{ReactiveScope.UserId}/dreams/{id}";
            var dream = new Dream(id, title, $"{folder}/image", $"{folder}/audio");

            await app.Files.Data.WriteBytesAsync(dream.ImagePath, await image.GetDataAsync(), image.MimeType);
            await app.Files.Data.WriteBytesAsync(dream.AudioPath, await music.GetDataAsync(), music.MimeType);
            _dreams.Add(dream);
        }
        catch (AIException ex)
        {
            Log.Instance.Warning($"Dream media for '{title}' failed, the user can try again: {ex.Message}");
            _error.Value = "Couldn't make the picture and sound. Try again.";
        }
    }

    private void Render(UIView view)
    {
        view.Column(["gap-3 max-w-md"], content: view =>
        {
            view.Button([Button.PrimaryMd, "self-start"], text: _busy.Value ? "Making it…" : "Make a dream card", disabled: _busy.Value,
                onClick: async () => await CreateAsync("flying over a lighthouse"));

            if (_error.Value is { } error)
            {
                view.Text(["text-destructive text-sm"], error);
            }

            foreach (var dream in _dreams)
            {
                view.Button([Button.OutlineMd, "self-start"], text: dream.Title, onClick: () => _openId.Value = dream.Id);
            }

            if (_dreams.FirstOrDefault(dream => dream.Id == _openId.Value) is { } open)
            {
                // Asset URIs, not URLs: the image and the player each turn one into a signed URL
                // and fetch a new one before that expires.
                view.Image(["w-full rounded-lg"], assetUri: app.Files.Data.GetAssetUri(open.ImagePath), alt: open.Title);
                // Keyed by the dream, so opening another stops this one's audio at once rather than once its URL arrives
                view.AudioUrlPlayer(assetUri: app.Files.Data.GetAssetUri(open.AudioPath), controls: true, key: open.Id);
            }
        });
    }
    #endregion
}
