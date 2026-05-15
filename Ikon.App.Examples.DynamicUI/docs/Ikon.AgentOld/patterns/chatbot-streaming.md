# Chatbot — Streaming AI Reply with Busy Flag

Shared transcript with an AI assistant. The transcript is shared reactive state; Send is gated by a reactive busy flag while the AI is thinking; the input clears after Send.

## When to use

Any single-LLM-conversation app — assistant, tutor, support bot, advisor, journaling buddy. If the brief asks for "talk to AI", reach for this.

## Snippet

```csharp
public sealed record ChatMessage(string Role, string Text);

private readonly Reactive<List<ChatMessage>> _transcript = new([]);
private readonly Reactive<string> _draft = new("");
private readonly Reactive<bool> _busy = new(false);

private async Task SendAsync()
{
    var text = _draft.Value.Trim();
    if (string.IsNullOrEmpty(text) || _busy.Value) return;

    _transcript.Value = [.. _transcript.Value, new ChatMessage("You", text)];
    _draft.Value = "";
    using var _ = _busy.AsToken();

    try
    {
        var transcript = string.Join("\n", _transcript.Value.Select(m => $"{m.Role}: {m.Text}"));
        var reply = await Emerge.AskAsync(
            $"Conversation so far:\n{transcript}\n\nReply concisely as Assistant.");
        _transcript.Value = [.. _transcript.Value, new ChatMessage("Assistant", reply)];
    }
    catch (Exception ex)
    {
        _transcript.Value = [.. _transcript.Value, new ChatMessage("System", $"Error: {ex.Message}")];
    }
}

// Inside UI.Root:
view.ScrollArea(rootStyle: ["flex-1 min-h-0"], viewportStyle: ["p-4"], content: view =>
{
    view.Column([..], content: view =>
    {
        foreach (var msg in _transcript.Value)
        {
            var isUser = msg.Role == "You";
            view.Box([isUser ? "self-end bg-primary" : "self-start bg-surface", "rounded-lg p-3 max-w-[80%]"], content: v =>
                v.Text(text: msg.Text));
        }
    });
});
view.Row(["p-4 gap-2 border-t"], content: view =>
{
    view.TextField(["flex-1"], value: _draft.Value, placeholder: "Type a message…",
        onValueChange: async v => _draft.Value = v,
        onSubmit: async _ => await SendAsync());
    view.Button(style: [Button.Default, _busy.Value ? "opacity-50" : ""],
        disabled: _busy.Value, onClick: SendAsync,
        content: v => v.Text(text: _busy.Value ? "Thinking…" : "Send"));
});
```

## Notes

- `_transcript` is a `Reactive<List<ChatMessage>>`. **Reassign** (`_transcript.Value = [.. _transcript.Value, x]`) — don't mutate the list in-place without `Notify()`.
- `_busy` gates Send (button disabled, label changes). The label-change is the loading state — no spinner needed for sub-2s replies; for longer ones add a Skeleton row.
- **Do NOT bind `_busy` to TextField's `disabled` prop** — the framework re-mounts the input on disabled flips and drops keyboard focus mid-typing. Gate the action via the Button + the early-return in `SendAsync`; let the user keep typing the next message while the AI is replying.
- Pass the **full transcript** to the LLM, not just the last user message.
- `Emerge.AskAsync(command)` is the one-shot shortcut (Claude45Haiku default) — use it for short transformations like a chat reply. Reach for the full `Emerge.Run<T>(model, ctx, pass => …).FinalAsync()` only when you need tools, multi-iteration agentic loops, or fine-grained pass control.
- Empty / whitespace input is a no-op.
- Wrap the LLM call in try/catch; surface the failure as a System message (visible) instead of swallowing.
- ScrollArea with `flex-1 min-h-0` keeps the input pinned to the bottom and scrolls only the message list.

## See also

- `busy-flag-loading` — generalised reactive busy pattern.
- `shared-list-ai-cleanup` — list mutation + AI transformation, similar reactive shape.
- `emergence` (top-level guide) — full Emerge.Run signatures including streaming.
