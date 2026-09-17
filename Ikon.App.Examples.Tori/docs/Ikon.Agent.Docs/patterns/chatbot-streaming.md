# Chatbot — Streaming AI Reply with Busy Flag
<!-- checked-against: d1acb674d2610937 -->
Shared transcript with an AI assistant. The transcript is shared reactive state; Send is gated by a reactive busy flag while the AI is thinking; the input clears after Send.

## When to use

Any single-LLM-conversation app — assistant, tutor, support bot, advisor, journaling buddy. If the brief asks for "talk to AI", reach for this.

## Snippet

```csharp
public sealed record ChatMessage(string Role, string Text);

private readonly ReactiveList<ChatMessage> _transcript = new();
private readonly ClientReactive<string> _draft = new("");
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string> _streaming = new("");
private KernelContext _ctx = new();

private async Task SendAsync()
{
    var text = _draft.Value.Trim();

    if (string.IsNullOrEmpty(text) || _busy.Value)
    {
        return;
    }

    _transcript.Add(new ChatMessage("You", text));
    _draft.Value = "";
    _streaming.Value = "";
    using var _ = _busy.AsToken();

    try
    {
        // Streaming: iterate Emerge.Run's event stream. Each ModelText<T>
        // event is the next chunk of the model's own output — append to
        // _streaming so the UI re-renders token-by-token. Completed<T>
        // fires once with the final result + the new KernelContext.
        //
        // Run<string> is what makes those chunks readable. Every other T
        // turns JSON mode on, and then the chunks ARE the JSON: the bubble
        // fills up with {"Reply":"He… instead of the reply.
        var sb = new System.Text.StringBuilder();
        await foreach (var ev in Emerge.Run<string>(LLMModel.Claude46Sonnet, _ctx, pass =>
        {
            pass.SystemPrompt = "You are a helpful assistant. Reply concisely.";
            pass.Command = text;
        }))
        {
            if (ev is ModelText<string> token)
            {
                sb.Append(token.Text);
                _streaming.Value = sb.ToString();
            }
            else if (ev is Completed<string> done)
            {
                _ctx = done.Context;

                if (!string.IsNullOrWhiteSpace(done.Result))
                {
                    _transcript.Add(new ChatMessage("Assistant", done.Result));
                }

                _streaming.Value = "";
            }
        }
    }
    catch (EmergenceStoppedException)
    {
        // Visible, and in the reader's language — never the exception text, which names a
        // provider and a socket to someone who wanted an answer.
        _transcript.Add(new ChatMessage("System", "Couldn't reach the assistant — send it again."));
        _streaming.Value = "";
    }
}

private void Render(IView view)
{
    view.ScrollArea(rootStyle: ["flex-1 min-h-0"], viewportStyle: ["p-4"], content: view =>
    {
        view.Column(["gap-3"], content: view =>
        {
            foreach (var msg in _transcript)
            {
                var isUser = msg.Role == "You";
                // bg-brand-solid, not bg-primary: in the Ikon semantic set bg-primary is the
                // PAGE surface, so a bubble painted with it disappears into the page.
                view.Box([isUser ? "self-end bg-brand-solid text-primary-on-brand" : "self-start bg-surface", "rounded-lg p-3 max-w-[80%]"], content: v =>
                    v.Text(text: msg.Text));
            }

            // In-flight streaming bubble: reading _streaming.Value here
            // registers a dependency, so this re-renders on every ModelText
            // event. Empties when Completed fires above.
            if (!string.IsNullOrEmpty(_streaming.Value))
            {
                view.Box(["self-start bg-surface rounded-lg p-3 max-w-[80%] opacity-80"], content: v =>
                    v.Text(text: _streaming.Value));
            }
        });
    });
    view.Row(["p-4 gap-2 border-t"], content: view =>
    {
        view.TextField([Input.Default, "flex-1"], value: _draft.Value, placeholder: "Type a message…",
            onValueChange: async v => _draft.Value = v,
            onSubmit: async _ => await SendAsync());
        view.Button(style: [Button.Default, _busy.Value ? "opacity-50" : ""],
            disabled: _busy.Value, onClick: SendAsync,
            content: v => v.Text(text: _busy.Value ? "Thinking…" : "Send"));
    });
}
```

## Notes

- `_transcript` is a `ReactiveList<ChatMessage>` — `_transcript.Add(msg)` mutates and notifies in one call; `.Value.Add` does not compile.
- `_busy` gates Send (button disabled, label changes). The label-change is the loading state — no spinner needed for sub-2s replies; for longer ones add a Skeleton row.
- **Do NOT bind `_busy` to TextField's `disabled` prop** — the framework re-mounts the input on disabled flips and drops keyboard focus mid-typing. Gate the action via the Button + the early-return in `SendAsync`; let the user keep typing the next message while the AI is replying.
- Pass the **full transcript** to the LLM, not just the last user message.
- **Streaming uses `Emerge.Run<T>(model, ctx, pass => …)` with `await foreach`** — observe `ModelText<T>` for live token chunks and `Completed<T>` for the final result + next-turn `KernelContext`. `Emerge.AskAsync(command)` is a one-shot shortcut that does NOT stream — only reach for it when the brief explicitly does not need streaming.
- **A streamed chat is `Run<string>`, not `Run<SomeRecord>`.** `EmergeScope<T>.UseJson` defaults to true for every `T` except `string`, so a typed `T` puts the model in JSON mode and `ModelText<T>.Text` then carries the raw JSON deltas — a bubble that fills with `{"Reply":"He…` and only becomes a sentence at `Completed`. `Run<string>` sends no schema, so the chunks are the prose. When a turn genuinely needs typed fields, run the typed pass separately and stream a `string` pass for what the user reads.
- **Do not put the schema in `pass.Command` yourself.** The executor already appends the schema (and an example) as its own instruction whenever `UseJson` is on; a hand-written `Return JSON:\n{pass.JsonSchema}` only sends it twice.
- Empty / whitespace input is a no-op.
- Wrap the LLM call in try/catch for `EmergenceStoppedException`; surface the failure as a System message (visible) instead of swallowing — **as a human sentence with a way to try again, never `ex.Message`**, which shows the reader a provider id and a socket error.
- **The user's bubble is `bg-brand-solid`, not `bg-primary`.** In the Ikon semantic set `bg-primary` is the page surface (white in light, near-black in dark), so a bubble painted with it vanishes into the page; Crosswind logs a warning when it sees it.
- ScrollArea with `flex-1 min-h-0` keeps the input pinned to the bottom and scrolls only the message list.

## See also

- `busy-flag-loading` — generalised reactive busy pattern.
- `shared-list-ai-cleanup` — list mutation + AI transformation, similar reactive shape.
- `emergence` (top-level guide) — full Emerge.Run signatures including streaming.
