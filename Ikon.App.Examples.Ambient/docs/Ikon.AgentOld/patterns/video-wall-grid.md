<!-- mined-from: Vorg.Commander -->
# Video Wall Grid — N Live Streams With Auto Layout

Grid of `VideoStreamCanvas` cells that auto-shapes (1×1, 2×2, 3×3) based on how many streams are active. Each cell has a black-bar overlay at the bottom showing the stream's source label. Empty cells render a placeholder. Used for drone fleets, security cameras, multi-cam meetings.

## When to use

Multi-camera dashboards, multi-drone command centers, bandwidth monitors — any time you have a variable count of live video streams and want a clean tile layout without manual breakpoints.

## Snippet

```csharp
private void RenderPrimaryVideoWall(UIView view)
{
    var activeStreams = _videoStreamStates.Keys.Take(9).ToList();

    view.Column(["flex-1 border border-[#333333] rounded bg-[#0d0d0d] p-4"], content: view =>
    {
        view.Row(["justify-between items-center mb-2"], content: view =>
        {
            view.Text(["text-xs font-bold text-[#666666] tracking-wider"], "VIDEO WALL");
            view.Text(["text-xs text-[#00ff00]"], $"{activeStreams.Count} ACTIVE FEEDS");
        });

        if (activeStreams.Count == 0)
        {
            view.Column(["flex-1 items-center justify-center"], content: view =>
            {
                view.Text(["text-[#333333] text-2xl"], "NO ACTIVE VIDEO FEEDS");
                view.Text(["text-[#333333] text-sm mt-2"], "Launch drones to see video streams");
            });
        }
        else
        {
            var cols = activeStreams.Count <= 1 ? 1 : activeStreams.Count <= 4 ? 2 : 3;
            var gridClass = cols switch
            {
                1 => "grid-cols-1",
                2 => "grid-cols-2",
                _ => "grid-cols-3"
            };

            view.Box([$"flex-1 grid {gridClass} gap-2"], content: view =>
            {
                foreach (var streamId in activeStreams)
                {
                    var streamInfo = _videoStreamStates.GetValueOrDefault(streamId);
                    var droneLabel = streamInfo?.DroneId ?? "Unknown";
                    var slotLabel = streamInfo?.SlotNumber > 0 ? $"Slot {streamInfo.SlotNumber}" : "";

                    view.Column(["border border-[#333333] rounded bg-[#050505] relative overflow-hidden min-h-[120px]"], content: view =>
                    {
                        view.VideoStreamCanvas(["w-full h-full object-contain"], streamId: streamId);

                        view.Box(["absolute bottom-0 left-0 right-0 bg-black/70 px-2 py-1"], content: view =>
                        {
                            view.Text(["text-xs text-[#00ff00] font-mono"], $"{droneLabel} {slotLabel}".Trim());
                        });
                    });
                }
            });
        }
    });
}
```

## Notes

- Cap the displayed streams (`Take(9)`) — Tailwind grid only has presets for `grid-cols-{1..6}` so going higher needs custom CSS.
- The label overlay uses `absolute bottom-0 left-0 right-0` over the canvas — simpler than a row beneath.
- `object-contain` keeps the stream's aspect ratio inside variable cell heights; `object-cover` would crop.
- `VideoStreamCanvas` requires the stream to be live; nothing renders before `Video.VideoInputStreamBeginAsync` fires.
- For self-preview (the local user seeing themselves), you must NOT set `TargetIds` on the capture options — it routes the stream past the server.

## See also

- `voice-loop` — same shape but for audio capture
- `multi-user-game` — base reactive shape for tracking which clients are streaming
