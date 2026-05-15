<!-- mined-from: Vorg.Commander -->
# Tactical Map + Markers — Lat/Lon-Driven Map With Clickable Pins

`view.TacticalMap(...)` takes a list of `MapMarker` (lat/lon + color + type), optional `FlightPath` polylines, area overlays, and waypoints. You build the marker list from your domain state each render — no imperative map handles. Click a marker, get a callback with the marker data.

> **Requires app-local code**: `view.TacticalMap(...)` is a custom React component registered via `view.AddNode("custom.tactical-map", ...)`. To use this pattern verbatim, copy `TacticalMapExtensions.cs` and the matching `frontend-node/src/customNodes/TacticalMap.tsx` from `Ikon.App.Vorg.Commander` or `Ikon.App.Veldra.OS`. Without those, the build fails with CS1061 on `view.TacticalMap`. If you don't have the custom node yet, the platform-level fallback is a Leaflet/Mapbox embed via `view.AddNode("custom.maplibre", ...)` — see `custom-react-node-embed`.

## When to use

Drone fleets, delivery dashboards, vehicle tracking, geo-events. Anywhere the data is already lat/lon and you'd otherwise reach for Leaflet/Mapbox JS.

## Snippet

```csharp
private void RenderOverviewMapView(UIView view)
{
    view.Column(["flex-1 border border-[#1E2E22] rounded bg-[#0B0F0C] overflow-hidden"], content: view =>
    {
        view.Row(["justify-between items-center px-4 py-2 border-b border-[#1E2E22]"], content: view =>
        {
            view.Text(["text-xs font-bold text-[#9FB5A3] tracking-wider"], "TACTICAL MAP");
            view.Text(["text-xs text-[#9FB5A3]"], $"{_fleet.Value.Count} SILOS  {_activeDrones.Value.Count} ACTIVE  {_targets.Value.Count} TARGETS");
        });

        var markers = BuildMapMarkers();
        var paths = BuildFlightPaths();
        var (centerLat, centerLon) = GetMapCenter();

        view.Row(["flex-1 overflow-hidden"], content: view =>
        {
            view.TacticalMap(
                markers: markers,
                paths: paths,
                centerLat: centerLat,
                centerLon: centerLon,
                zoom: 11,
                onMarkerClick: async data =>
                {
                    AddEventLog($"Selected: {data.Type} {data.Label ?? data.Id}", "Map");
                },
                style: ["flex-1"]);
        });
    });
}

private List<MapMarker> BuildMapMarkers()
{
    var markers = new List<MapMarker>();

    foreach (var silo in _fleet.Value)
    {
        var ready = silo.Slots.Count(s => s.Status == SlotStatus.Ready);
        var total = silo.Slots.Count;
        var color = ready == total ? "#00ff00" : ready > 0 ? "#ffff00" : "#ff0000";
        markers.Add(new MapMarker
        {
            Id = silo.SiloId, Lat = silo.Latitude, Lon = silo.Longitude,
            Type = "silo", Label = $"{silo.Name} ({ready}/{total})",
            Color = color, Status = $"{ready} ready",
        });
    }

    foreach (var target in _targets.Value)
    {
        markers.Add(new MapMarker
        {
            Id = target.TargetId, Lat = target.Latitude, Lon = target.Longitude,
            Type = "target", Label = $"{target.Type}: {target.SourceLabel}",
            Color = "#ff6600",
            Status = target.IsAssigned ? $"Assigned to {target.AssignedSiloId}" : "Unassigned",
        });
    }
    return markers;
}
```

## Notes

- The map is a leaf component — you don't draw inside it; you hand it data and a click handler.
- Re-derive the marker list every render from `Reactive<List<...>>` state. The diff is what gets streamed.
- Marker `Type` is a free string; the React side picks an icon based on it. Conventional values: `silo`, `target`, `drone`, `track`.
- Center the map on the most relevant entity (active drone → silo → fallback hardcoded coord) — see `GetMapCenter`.
- `paths` (polylines) and `areaOverlays` are optional; pass `null` to skip rather than empty lists for clarity.

## See also

- `kpi-card-grid` — companion summary panels next to the map
- `video-wall-grid` — pair the map with live drone feeds in a split view
