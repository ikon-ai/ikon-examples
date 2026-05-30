<!-- mined-from: Ikon.App.AutoCRM.Agent -->
# Dynamic Text Size By Length — Make Short Replies Feel Big

Pick the message font size from the message length. Short replies render in `text-4xl font-light` like a billboard; medium in `text-2xl`; long messages drop to `text-xl`. Combined with a gradient text fill, it makes any LLM reply feel intentional and crafted.

## When to use

Premium-feel chat surfaces (consumer assistants, brand-forward apps) where the message bubble is the entire visual. Replaces the standard same-size-everything chat row.

## Snippet

```csharp
private static void RenderMessage(UIView view, ChatMessage msg)
{
    var len = msg.Content.Length;

    if (msg.Role == "user")
    {
        var textStyle = len < 20  ? "text-3xl font-medium text-white/50"
                      : len < 60  ? "text-2xl font-medium text-white/50"
                                  : "text-lg text-white/40";

        view.Row(["justify-end w-full"], content: row =>
            row.Text([textStyle, "max-w-[80%] text-right"], msg.Content));
        return;
    }

    var aiStyle = len < 50  ? "text-4xl font-light leading-tight"
                : len < 120 ? "text-3xl font-light leading-snug"
                : len < 300 ? "text-2xl font-light leading-relaxed"
                            : "text-xl font-light leading-relaxed";

    view.Markdown(
        style: [aiStyle, "bg-gradient-to-r from-white via-white/90 to-white/60 bg-clip-text text-transparent"],
        content: msg.Content);
}
```

## Notes

- Three or four breakpoints is enough — more buckets and the size jitters as the LLM emits one extra word.
- Pair with a thin `font-light` weight at the large sizes; bold-at-huge looks like advertising.
- The `bg-clip-text text-transparent` gradient is the secret sauce — gives long-form text an editorial-magazine feel without an image.
- User messages stay smaller and right-aligned; the AI is the protagonist.

## See also

- `cinematic-fullscreen-scene`
- `chat-with-tool-calls`
