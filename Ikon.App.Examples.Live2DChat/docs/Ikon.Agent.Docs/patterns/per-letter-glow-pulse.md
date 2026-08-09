<!-- mined-from: Ikon.App.Examples.Kahoot -->
# Per-Letter Glow Pulse — Animated Wordmark Without JS

A logo or hero-text effect built entirely in Crosswind motion: each letter loops a brightness keyframe (`0:brightness-100, 40:brightness-[1.8], 100:brightness-100`) staggered across letters. Combined with a `drop-shadow-[0_0_20px_...]` and an aurora gradient backdrop, the wordmark feels alive without a single JS animation. Same effect appears in Kahoot's player intro and on every fresh blank-state hero.

## When to use

Lobby screens, splash hero text, "Get ready!" countdowns — places where the user is waiting and the screen is mostly empty. Kahoot uses it both at lobby (lights up the brand) and at the per-letter "Game Over!" reveal.

## Snippet

```csharp
private const string AuroraGradient =
    "bg-[radial-gradient(ellipse_at_top_left,rgba(59,130,246,0.25)_0%,transparent_50%),"
  + "radial-gradient(ellipse_at_top_right,rgba(139,92,246,0.2)_0%,transparent_45%),"
  + "radial-gradient(ellipse_at_bottom,rgba(6,182,212,0.2)_0%,transparent_50%)]";

view.Column(style: ["w-full h-screen bg-black text-white relative overflow-hidden", AuroraGradient], content: view =>
{
    view.Text(
        style: [
            "text-7xl font-bold text-purple-400 drop-shadow-[0_0_20px_rgba(168,85,247,0.5)]",
            "motion-[0:brightness-100,40:brightness-[1.8],100:brightness-100]",
            "motion-duration-1500ms motion-stagger-200ms motion-per-letter-loop"
        ],
        text: "Ikon Kahoot");

    view.Text(style: ["text-xl text-gray-400 mt-2"], text: "Test your Ikon platform knowledge!");
});

// VRMChat uses a softer per-letter blur-in for chat messages:
view.Text(
    style: [
        "text-white text-lg font-medium",
        "letter:motion-[0:opacity-0 blur-[3px],100:opacity-100 blur-0]",
        "letter:motion-duration-200ms letter:motion-stagger-50ms letter:motion-per-letter letter:motion-fill-both"
    ],
    text: messageText);
```

## Notes

- `motion-per-letter-loop` keeps the brightness pulsing forever; `motion-per-letter` (no `-loop`) plays once on mount — use the second for chat-message reveals.
- `motion-stagger-200ms` shifts each letter's start so the pulse looks like a wave traveling across the word.
- The aurora gradient is just three nested `radial-gradient(ellipse_at_*)` calls — compose them in one Tailwind arbitrary-value class.
- Pair with a `drop-shadow-[0_0_20px_...]` matching the text color for the "neon" look without bloom shaders.

## See also

- `cinematic-fullscreen-scene`
- `playful-loading-text-rotator`
