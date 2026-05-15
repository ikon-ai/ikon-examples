<!-- mined-from: Ikon.App.Examples.VRMChat -->
# VRM Avatar Expression Motion — LLM Picks Pose Per Reply

A VRM 3D character is rendered as a full-screen background while the LLM returns a `ChatReply` with text *plus* a chosen `Motion` and `Expression` from a fixed vocabulary. Setting the reactive motion/expression directly drives the avatar — same call that gave you the chat text also animates the character.

> **Requires app-local code**: `view.VRMCanvas(...)` is a custom React component registered via `view.AddNode("custom.vrm-canvas", ...)`. To use this pattern verbatim, copy `VRMExtensions.cs` and the matching `frontend-node/src/customNodes/VRMCanvas.tsx` from `Ikon.App.Examples.VRMChat`. Without those, the build fails with CS1061 on `view.VRMCanvas`. The motion/expression vocabulary in this pattern is otherwise transferable — even apps that render the avatar a different way (e.g. 2D Live2D) can reuse the LLM-picks-emotion idea.

## When to use

You're building a conversational AI with a visible 3D or 2D character (assistant, NPC, tutor) and want the avatar's posture and face to track the emotional tone of each response. Folding the motion choice into the same `Emerge.Run<T>()` call avoids a second LLM round-trip.

## Snippet

```csharp
public class ChatReply
{
    public string Message { get; set; } = "";
    public string Motion { get; set; } = "idle";
    public string Expression { get; set; } = "neutral";
}

private readonly Reactive<string> _currentExpression = new("");
private readonly Reactive<string> _currentMotion = new("");

UI.Root(style: ["font-sans h-screen w-screen overflow-hidden relative"], content: view =>
{
    var currentModel = AvailableModels[_selectedModelIndex.Value];

    view.VRMCanvas(
        source: currentModel.Path,
        isListening: _isListening.Value,
        expression: _currentExpression.Value,
        motion: _currentMotion.Value,
        viewMode: "fullBody",
        style: ["absolute inset-0 w-full h-full"]);
    // ... chat overlay sits on top of the canvas
});

var (reply, updatedContext) = await Emerge.Run<ChatReply>(LLMModel.Gpt41, _chatContext, pass =>
{
    pass.SystemPrompt = "You are a friendly VRM avatar assistant. "
        + "For each reply, also choose a Motion and Expression for your avatar.\n"
        + "Available motions: idle, thinking, excited, shy, confident, waving, listening, talking, stretching, looking_around\n"
        + "Available expressions: happy, angry, sad, relaxed, surprised, neutral";
    pass.Command = userText;
}).FinalAsync(ct);

_currentMotion.Value = reply.Motion;
_currentExpression.Value = reply.Expression;
_currentMotion.Value = "talking";
await SpeakTextAsync(reply.Message);
_currentMotion.Value = "idle";
_currentExpression.Value = "neutral";
```

## Notes

- The motion/expression vocabulary lives *only* in the system prompt — the model picks from those strings; the VRM component matches them to clip names.
- Override the LLM's choice with hard-coded states for activity transitions: `"thinking"` while waiting for the response, `"talking"` while TTS is playing, `"idle"` after.
- Pair with a separate idle loop (see `idle-driven-llm-action-loop`) so the avatar isn't frozen between turns.
- `viewMode: "fullBody" | "portrait" | "face"` switches camera framing.

## See also

- `idle-driven-llm-action-loop`
- `voice-loop`
- `chat-with-tool-calls`
