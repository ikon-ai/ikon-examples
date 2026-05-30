<!-- mined-from: Veldra.OS -->
# Map Layer Toggle Panel — Collapsible Layers With Counts + Presets

A floating top-corner panel over a tactical map that collapses to a small "LAYERS (n on)" pill and expands to a grouped list of toggles. Each toggle shows its live count so the operator knows whether toggling does anything. A row of preset chips ("Default", "Threats", "Infra") snaps the layer set to common configurations in one click. Layer state is a single immutable record updated with `with`.

## When to use

Any map / dashboard with more than ~5 toggleable overlays. Ad-hoc checkbox lists drown in 18 toggles; this panel groups them, hides them by default, shows counts so empty layers don't look broken, and lets the user jump between named views.

## Snippet

```csharp
private readonly Reactive<MapLayers> _mapLayers = new(MapLayers.Default);
private readonly Reactive<bool> _layerPanelOpen = new(false);

private void RenderLayerTogglePanel(UIView view)
{
    if (!_layerPanelOpen.Value)
    {
        view.Box([
            "absolute top-3 left-3 z-[1000] px-3 py-2 cursor-pointer rounded-sm",
            "bg-[#0A1018]/95 border-2", Theme1.BorderBrand,
        ], onClick: async () => { _layerPanelOpen.Value = true; await Task.CompletedTask; },
        content: view => view.Row(["items-center gap-2"], content: view =>
        {
            view.Text([Theme1.Text1, "text-[11px] tracking-[0.25em] font-bold"], "LAYERS");
            view.Text([Theme1.Text2, "text-[10px] font-mono"], $"{CountActiveLayers(_mapLayers.Value)} on");
        }));
        return;
    }
    view.Column(["absolute top-3 left-3 z-[1000] p-3 gap-1 w-[240px]"], content: view =>
    {
        view.Row(["gap-1 mb-1"], content: view =>
        {
            LayerPresetChip(view, "Default", MapLayers.Default);
            LayerPresetChip(view, "Threats", MapLayers.ThreatsOnly);
        });
        var c = ComputeLayerCounts();
        RenderLayerRow(view, "Tracked threats", c.AliveTracks,
            l => l.TrackedTargets, (l, v) => l with { TrackedTargets = v });
        RenderLayerRow(view, "Predictions", c.Predictions,
            l => l.Predictions, (l, v) => l with { Predictions = v });
        RenderLayerRow(view, "No-fly zones", c.NoFlyZones,
            l => l.NoFlyZones, (l, v) => l with { NoFlyZones = v });
    });
}

private void RenderLayerRow(UIView view, string label, int count,
    Func<MapLayers, bool> read, Func<MapLayers, bool, MapLayers> write)
{
    var on = read(_mapLayers.Value);
    view.Box(["px-2 py-0.5 cursor-pointer hover:bg-[#17202B]"],
        onClick: async () => { _mapLayers.Value = write(_mapLayers.Value, !on); await Task.CompletedTask; },
        content: view => view.Row(["items-center gap-2"], content: view =>
        {
            view.Box([on ? "w-2 h-2 rounded-full bg-[#F5A623]" : "w-2 h-2 rounded-full border"]);
            view.Text(["flex-1 text-[10px]"], label);
            view.Text(["text-[9px] font-mono"], count.ToString());
        }));
}
```

## Notes

- Use a single `record MapLayers(bool A, bool B, ...)` with `with`-update so the whole layer state diffs as one value.
- Live counts next to each label distinguish "no data" from "broken layer".
- Presets are equality-checked against the current record to highlight the active preset chip.
- `z-[1000]` is needed so the panel beats Leaflet's own controls.

## See also

- `tactical-map-markers`
- `filter-button-group`
