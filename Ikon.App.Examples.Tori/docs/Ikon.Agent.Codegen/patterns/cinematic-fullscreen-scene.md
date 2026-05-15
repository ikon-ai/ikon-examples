<!-- mined-from: Vienola -->
# Cinematic Fullscreen Scene — Image + Gradient + Overlay HUD

A fullscreen background image (the AI-generated scene) with multiple gradient layers above it for text legibility, and absolutely-positioned HUD elements (top-left menu, top-right clock, bottom-right phase). The narrative and input live in a translucent pill at the bottom. Combat tints the whole stack; low HP adds a pulsing red vignette.

## When to use

Story games, narrative AI experiences, immersive single-screen apps where the generated image *is* the UI and controls float on top.

## Snippet

```csharp
view.Box(["h-screen w-full relative overflow-hidden bg-[#0E0E0E]"], content: view =>
{
    // Layer 1: Fullscreen scene image with combat effects
    if (_sceneImageData.Value != null && _sceneImageMime.Value != null)
    {
        view.Image([$"absolute inset-0 w-full h-full object-cover {C.SceneZoom} {combatFilter} transition-all duration-1000"],
            data: _sceneImageData.Value,
            mimeType: _sceneImageMime.Value,
            alt: "Scene");
    }

    // Layer 1b: Red vignette overlay when HP is low
    if (lowHp)
    {
        view.Box([$"absolute inset-0 pointer-events-none bg-[radial-gradient(ellipse_at_center,transparent_40%,rgba(139,0,0,0.4)_100%)] " +
            "motion-[0:opacity-50,50:opacity-80,100:opacity-50] motion-duration-[2s] motion-loop"], content: _ => { });
    }

    // Layer 2: Gradient overlays for readability
    view.Box(["absolute inset-0 bg-gradient-to-t from-[#0E0E0E] from-0% via-[#0E0E0E]/95 via-30% to-transparent to-70% pointer-events-none"], content: _ => { });
    view.Box(["absolute inset-0 bg-gradient-to-r from-[#0E0E0E]/85 from-0% via-[#0E0E0E]/40 via-30% to-transparent to-55% pointer-events-none"], content: _ => { });
    view.Box(["absolute inset-0 bg-gradient-to-b from-[#0E0E0E]/70 from-0% to-transparent to-15% pointer-events-none"], content: _ => { });

    // Layer 3: Narrative + input (bottom)
    view.Column(["absolute inset-0 justify-end p-2 pb-2 sm:p-6 sm:pb-5 pointer-events-none"], content: view =>
    {
        view.Box(["max-w-3xl pointer-events-auto bg-black/80 backdrop-blur-md rounded-2xl p-3 sm:p-6 border border-white/10 shadow-2xl"], content: view =>
        {
            view.Column(["gap-4"], content: view =>
            {
                RenderNarrativeOverlay(view);
                if (_gamePhase.Value == GamePhase.Combat)
                    RenderCombatOverlay(view);
                else
                    RenderInputBar(view);
            });
        });
    });

    // Layer 4: Minimal HUD (corners) — pointer-events-auto only on interactive bits
    RenderMinimalHUD(view);

    // Soundscape audio player (hidden, autoplay loop)
    if (_soundscapeUrl.Value != null)
        view.AudioUrlPlayer(["hidden"], url: _soundscapeUrl.Value, autoplay: true, loop: true);
});
```

## Notes

- Make the wrapper column `pointer-events-none` and re-enable `pointer-events-auto` only on the interactive pill — empty space stays click-through.
- Three gradients (top, bottom, left) shouldn't all be drawn at full opacity; tune the dominant one to where your text lives.
- Use `transition-all duration-1000` on the image so scene transitions cross-fade rather than pop.
- `motion-[0:opacity-50,50:opacity-80,100:opacity-50] motion-loop` on the vignette gives a heartbeat-like distress effect.
- Hidden `AudioUrlPlayer` autoplays soundscape — no UI, just ambience.

## See also

- `state-machine-cards-and-transitions` — driving phase swaps (exploration → combat) above the scene
- `image-gallery` — different shape: gallery of small images, not one fullscreen
