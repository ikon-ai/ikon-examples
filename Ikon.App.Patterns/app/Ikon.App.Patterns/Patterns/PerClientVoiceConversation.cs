namespace Ikon.App.Patterns.Patterns;

// Pattern: per-client-voice-conversation — see docs/patterns/per-client-voice-conversation.md.
// The gallery does not call WireVoice, since turn detection is one setup call per app; the typed
// point runs the same turn, so the demo answers without a microphone.
internal sealed class PerClientVoiceConversation(IAppBase app) : IPatternDemo
{
    public string Slug => "per-client-voice-conversation";
    public string Title => "A voice conversation per tab";
    public string Category => "Voice & audio";

    public void RenderDemo(IView view) => RenderConversation((UIView)view);

    #region example:pattern-per-client-voice-conversation
    private sealed record Line(string Speaker, string Text);

    private Audio Audio { get; } = new(app);

    // Every tab shares one instance, and each has its own conversation
    private readonly ClientReactiveList<Line> _lines = new();
    private readonly ClientReactive<bool> _listening = new(false);
    private readonly ClientReactive<bool> _thinking = new(false);

    // The latest turn per tab. A reply compares its turn with this before it speaks, so a reply the
    // person talked over is dropped instead of being spoken late.
    private readonly ConcurrentDictionary<int, int> _turns = new();

    /// <summary>Call once from Main.</summary>
    private void WireVoice()
    {
        // pauseWhileAppSpeaking: false keeps the microphone live while the app talks, so a person
        // can interrupt it
        Audio.UseTurnDetection(SpeechRecognizerModel.WhisperLarge3Turbo, pauseWhileAppSpeaking: false);

        // Audio events run in the speaking client's scope, so .Value here is that tab's value
        Audio.TurnStartedAsync += async args =>
        {
            BeginTurn(args.ClientSessionId);
            _listening.Value = true;
        };

        Audio.SpeechRecognizedAsync += async args =>
        {
            _listening.Value = false;
            await TakeTurnAsync(args.ClientSessionId, args.Text);
        };

        Audio.SpeechNotRecognizedAsync += async args => _listening.Value = false;

        app.OnClientLeft(async ctx => _turns.TryRemove(ctx.SessionId, out _));
    }

    // A new turn makes every reply still in progress stale, and cuts the line being spoken
    private int BeginTurn(int clientSessionId)
    {
        Audio.Stop(MediaTargets.To(clientSessionId), "speech");
        return _turns.AddOrUpdate(clientSessionId, 1, (_, turn) => turn + 1);
    }

    // Speech and typed text both arrive here
    private async Task TakeTurnAsync(int clientSessionId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var turn = BeginTurn(clientSessionId);
        _lines.AddFor(clientSessionId, new Line("You", text.Trim()));

        // The reply takes seconds and the handler returns at once
        _ = Task.Run(() => ReplyAsync(clientSessionId, turn));
    }

    private async Task ReplyAsync(int clientSessionId, int turn)
    {
        // No client scope is relied on here: every write names the tab with a ...For method
        _thinking.SetFor(clientSessionId, true);

        try
        {
            var transcript = string.Join("\n", _lines.ValueFor(clientSessionId).TakeLast(20).Select(l => $"{l.Speaker}: {l.Text}"));
            var reply = await Emerge.Run<string>(LLMModel.Claude45Haiku, pass =>
            {
                pass.SystemPrompt = "You argue the other side of a debate. Answer the last point in two short spoken sentences.";
                pass.Command = transcript;
            });

            // The person spoke again while the model answered; that newer turn gets its own reply
            if (_turns.GetValueOrDefault(clientSessionId) != turn)
            {
                return;
            }

            _lines.AddFor(clientSessionId, new Line("Opponent", reply));
            Audio.Speak(MediaTargets.To(clientSessionId), reply);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Reply to turn {turn} of client {clientSessionId} failed, the person can say it again: {ex.Message}");
            _lines.AddFor(clientSessionId, new Line("Opponent", "Sorry, I lost that. Say it again?"));
        }
        finally
        {
            // A newer turn has its own reply running, which clears the flag when it ends
            if (_turns.GetValueOrDefault(clientSessionId) == turn)
            {
                _thinking.SetFor(clientSessionId, false);
            }
        }
    }

    private void RenderConversation(UIView view)
    {
        view.Column(["gap-3 w-full max-w-xl"], content: col =>
        {
            var lines = _lines.Value;

            for (var i = 0; i < lines.Count; i++)
            {
                col.Text([Text.Body], $"{lines[i].Speaker}: {lines[i].Text}", key: $"line-{i}");
            }

            col.Text([Text.Caption], _listening.Value ? "Listening…" : _thinking.Value ? "Thinking…" : "Tap the microphone once and talk, or type your point.");

            col.Row(["gap-2 items-center"], content: row =>
            {
                // Echo cancellation is on by default here, so the app's own voice does not start a turn
                row.MicToggleButton(text: "Talk");
                row.TextField(["default", "flex-1"], placeholder: "Your point", ariaLabel: "Your point", clearOnSubmit: true,
                    onSubmit: async text => await TakeTurnAsync(ReactiveScope.ClientId, text));
            });
        });
    }
    #endregion
}
