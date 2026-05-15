<!-- mined-from: Ikon.App.AutoCRM.Agent -->
# Inline Marker Extraction — `[vehicle:ID]` Tokens In LLM Output

Tell the LLM to embed `[type:id]` markers wherever it references a record (car, product, doc). After streaming completes, regex-extract every marker, fetch the rich payload (images, links, cards), strip the markers from the rendered text, and attach the payload to the message metadata. Markdown stays clean; UI gets structured attachments.

## When to use

Chat agents that mention referenceable records and you want to hang real UI off those mentions (image carousels, citation cards, action buttons) without forcing the model to emit JSON.

## Snippet

```csharp
private async Task<List<VehicleImages>?> ExtractVehicleImagesAsync(string message)
{
    var matches = Regex.Matches(message, @"\[vehicle:([^\]]+)\]");
    if (matches.Count == 0) { return null; }

    var vehicles = new List<VehicleImages>();

    foreach (Match m in matches)
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
                    if (img.TryGetProperty("url", out var url)) urls.Add(url.GetString()!);
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

// In the Completed handler:
var vehicles = await ExtractVehicleImagesAsync(finalMessage);
finalMessage = Regex.Replace(finalMessage, @"\[vehicle:[^\]]+\]", "").Trim();
_messages.Value = [.._messages.Value,
    new ChatMessage("assistant", finalMessage, DateTime.UtcNow, vehicles)];
```

## Notes

- Marker shape: `[type:id]`. Tell the LLM to emit them in the system prompt with explicit examples; tell it WHEN to emit (first introduction, final confirmation) and when not (casual re-mention).
- Track per-conversation `_shownVehicles` so the second mention of the same id doesn't re-render the same image bank — the agent then gets an "already shown" hint in the next system prompt.
- Strip markers before rendering. Markdown should never contain raw `[type:id]` artifacts.
- Attach the structured payload as a separate field on the message record (`Vehicles`, `Citations`, `Cards`) — the renderer iterates it after the markdown.

## See also

- `clickable-reference-card-in-chat`
- `llm-vision-cache`
