namespace Ikon.App.Patterns.Patterns;

// Pattern: inline-image-marker-extraction — see docs/patterns/inline-image-marker-extraction.md.
// The docsnippet region extracts markers from finished LLM output; the stubs outside it stand in for
// the showroom API, the visual cache and the per-conversation state the extraction reads.
internal sealed class InlineImageMarkerExtraction : IPatternDemo
{
    public string Slug => "inline-image-marker-extraction";
    public string Title => "Inline image marker extraction";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: extracts [vehicle:id] markers from finished LLM output, resolves each to images once, and strips the markers from the shown text. See the source and docs/patterns/inline-image-marker-extraction.md.");

    private sealed record VehicleImages(string Label, List<string> Urls);
    private sealed record ShownVehicle(string Label, string VisualKey);
    private sealed record ChatMessage(string Role, string Text, DateTime At, List<VehicleImages>? Vehicles);

    private sealed class ShowroomApiClient
    {
        public string? GetVehicleLabel(string showroom, string id) => throw new NotImplementedException();
        public Task<string> GetVehicleImagesAsync(string showroom, string id) => throw new NotImplementedException();
    }

    private sealed class VisualCache
    {
        public Task<string> GetOrCreateAsync(string id, string label, List<string> urls) => throw new NotImplementedException();
    }

    private readonly ShowroomApiClient _apiClient = new();
    private readonly VisualCache _visualCache = new();
    private readonly Reactive<string> _showroom = new("");
    private readonly Dictionary<string, ShownVehicle> _shownVehicles = new();
    private readonly ReactiveList<ChatMessage> _messages = new();
    private string finalMessage = "";

    #region docsnippet:pattern-inline-image-marker-extraction
    private async Task<List<VehicleImages>?> ExtractVehicleImagesAsync(string message)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(message, @"\[vehicle:([^\]]+)\]");
        if (matches.Count == 0) { return null; }

        var vehicles = new List<VehicleImages>();

        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            var id = m.Groups[1].Value;
            var alreadyShown = _shownVehicles.ContainsKey(id);
            if (alreadyShown) { continue; }

            try
            {
                var label = _apiClient.GetVehicleLabel(_showroom.Value, id) ?? "Vehicle";
                var imagesJson = await _apiClient.GetVehicleImagesAsync(_showroom.Value, id);
                using var doc = JsonDocument.Parse(imagesJson);

                var urls = new List<string>();
                if (doc.RootElement.TryGetProperty("images", out var images))
                {
                    foreach (var img in images.EnumerateArray())
                    {
                        if (img.TryGetProperty("url", out var url)) { urls.Add(url.GetString()!); }
                    }
                }

                if (urls.Count > 0)
                {
                    _shownVehicles[id] = new ShownVehicle(label, await _visualCache.GetOrCreateAsync(id, label, urls));
                    vehicles.Add(new VehicleImages(label, urls));
                }
            }
            catch (Exception ex) { Log.Instance.Debug($"marker {id}: {ex.Message}"); }
        }

        return vehicles.Count > 0 ? vehicles : null;
    }

    private async Task Demo()
    {
        // In the Completed handler:
        var vehicles = await ExtractVehicleImagesAsync(finalMessage);
        finalMessage = System.Text.RegularExpressions.Regex.Replace(finalMessage, @"\[vehicle:[^\]]+\]", "").Trim();
        _messages.Add(new ChatMessage("assistant", finalMessage, DateTime.UtcNow, vehicles));
    }
    #endregion
}
