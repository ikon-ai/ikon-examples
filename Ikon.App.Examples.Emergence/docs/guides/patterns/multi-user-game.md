# Multi-User Game — Per-Client + Shared State

The mix that makes Ikon's real-time-multi-user shape sing: shared state for the game (current question, scores) + per-client state for each player's pick.

## When to use

Trivia game, voting, shared canvas, polls, multiplayer puzzles, lobby + game flow. Anywhere players interact independently while seeing the same world.

## Snippet

```csharp
public sealed record Question(string Prompt, string[] Choices, int CorrectIndex);

private readonly Reactive<Question?> _current = new(null);
private readonly Reactive<int> _round = new(0);
private readonly Reactive<bool> _revealed = new(false);
private readonly Reactive<Dictionary<string, int>> _scores = new(new());
private readonly Reactive<Dictionary<string, int>> _picks = new(new()); // clientId -> pickedIndex
private readonly ClientReactive<int?> _myPick = new(null);

private async Task GenerateAsync(string topic)
{
    var (qs, _) = await Emerge.Run<List<Question>>(LLMModel.Claude46Sonnet, new KernelContext(),
        pass => { pass.Command = $"Generate 5 multiple-choice trivia questions on {topic}."; })
        .FinalAsync();
    if (qs is { Count: > 0 })
    {
        _current.Value = qs[0];
        _round.Value = 0;
    }
}

private void Pick(string clientId, int index)
{
    if (_revealed.Value) return;
    var picks = new Dictionary<string, int>(_picks.Value) { [clientId] = index };
    _picks.Value = picks;
    _myPick.Value = index;
}

private void Reveal()
{
    _revealed.Value = true;
    if (_current.Value is { } q)
    {
        var scores = new Dictionary<string, int>(_scores.Value);
        foreach (var (clientId, pick) in _picks.Value)
        {
            if (pick == q.CorrectIndex)
                scores[clientId] = scores.GetValueOrDefault(clientId) + 1;
        }
        _scores.Value = scores;
    }
}

// UI:
if (_current.Value is { } question)
{
    view.Column(["gap-4 p-6"], content: view =>
    {
        view.Text(["text-2xl font-semibold"], text: question.Prompt);
        view.Column(["gap-2"], content: view =>
        {
            for (int i = 0; i < question.Choices.Length; i++)
            {
                var idx = i;
                var picked = _myPick.Value == idx;
                var correct = _revealed.Value && idx == question.CorrectIndex;
                var wrong = _revealed.Value && picked && idx != question.CorrectIndex;
                var style = correct ? "bg-success text-success-foreground"
                          : wrong ? "bg-destructive text-destructive-foreground"
                          : picked ? "bg-primary/30" : "bg-surface";
                view.Button(style: [style, "transition-colors duration-150 hover:opacity-90 text-left p-3 rounded-lg"],
                    disabled: _revealed.Value,
                    onClick: () => Pick(app.ClientSessionId, idx),
                    content: v => v.Text(text: question.Choices[idx]));
            }
        });

        view.Row(["gap-2"], content: view =>
        {
            if (!_revealed.Value)
            {
                view.Button(style: [Button.SecondaryMd], onClick: Reveal,
                    content: v => v.Text(text: "Reveal"));
            }
            else
            {
                view.Button(style: [Button.Default],
                    onClick: () => { _round.Value++; _revealed.Value = false; _picks.Value = new(); /* load next question */ },
                    content: v => v.Text(text: "Next"));
            }
        });
    });
}

// Leaderboard
view.Column(["border-t p-4 gap-1"], content: view =>
{
    foreach (var (clientId, score) in _scores.Value.OrderByDescending(kv => kv.Value))
    {
        view.Row(["gap-3 items-center"], content: v =>
        {
            v.Text(["font-mono text-sm flex-1"], text: clientId[..8]);
            v.Text(["font-semibold"], text: $"{score}");
        });
    }
});
```

## Notes

- **Shared** (`Reactive<T>`): the question, scores, who picked what (visible to all so the host knows when to reveal). When a client picks, the dictionary mutation triggers a re-render on every connected client.
- **Per-client** (`ClientReactive<T>`): which choice the local player tapped. Each client has their own value; nobody else sees it.
- Always **reassign** Reactive dictionaries (`_picks.Value = new Dictionary<string,int>(_picks.Value) { [k] = v }`). Don't mutate in place.
- The `disabled: _revealed.Value` on choice buttons stops late picks.
- Per-client correct/wrong feedback is computed from `_revealed && _myPick == correct` — pure derivation, no extra state.

## See also

- `reactive-state` (top-level guide) — full lifecycle of Reactive vs ClientReactive vs UserReactive.
- `chatbot-streaming` — single-LLM-call pattern, simpler state.
- `kanban-multi-column` — also uses shared lists, not games.
