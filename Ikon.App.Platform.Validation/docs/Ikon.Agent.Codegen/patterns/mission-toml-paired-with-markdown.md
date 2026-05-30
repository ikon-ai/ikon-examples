<!-- mined-from: Ikon.App.Threads -->
# Mission Doc + Config TOML — Two Files, Two Audiences

Each thread/agent run owns two paired artifacts: a Markdown `mission.md` for humans (Goal + Behavior), a `mission.toml` for the runtime (agent name, schedule, priority, max iterations). When the user edits the TOML, the app reparses it and applies the changes to the live thread; the Markdown is just guidance for the LLM.

## When to use

Long-running agent threads where you want both human-editable narrative ("what's the goal?") and structured config ("run every 1h, priority high"). Same artifact store, same edit UX, two clearly-typed roles.

## Snippet

```csharp
private async Task CreateMissionArtifacts(ThreadInfo thread, string title)
{
    var missionMd = $"## Goal\n{title}\n\n## Behavior\nEngage with the topic. " +
                    $"If the goal is clear, work on it. If ambiguous, ask one clarifying question.";
    var mdArt = await _artifactStore.CreateArtifactAsync(thread.Id,
        ArtifactNames.MissionMd, ArtifactTypes.Mission, missionMd);
    await _threadStore.SetMissionArtifactIdAsync(thread.Id, mdArt.Id);

    var toml = GenerateMissionToml(thread);
    var tomlArt = await _artifactStore.CreateArtifactAsync(thread.Id,
        ArtifactNames.MissionToml, ArtifactTypes.Config, toml);
}

private async Task ApplyMissionConfig(string threadId, string tomlContent)
{
    try
    {
        var config = Toml.From<MissionConfig>(tomlContent);

        if (!string.IsNullOrWhiteSpace(config.Agent.Name))
            await _threadStore.SetAgentNameAsync(threadId, config.Agent.Name);

        if (config.Schedule.Kind == "recurring" && !string.IsNullOrWhiteSpace(config.Schedule.Every))
        {
            var ms = ParseDuration(config.Schedule.Every);
            await _threadStore.UpdateThreadScheduleAsync(threadId,
                new ThreadSchedule { Kind = ThreadScheduleKind.Recurring, EveryMs = ms });
        }

        if (Enum.TryParse<ThreadPriority>(config.Config.Priority, true, out var priority))
            await _threadStore.UpdateThreadPriorityAsync(threadId, priority);
    }
    catch (Exception ex) { Log.Instance.Warning($"Apply mission config: {ex.Message}"); }
}

private static long ParseDuration(string s)
{
    s = s.Trim().ToLowerInvariant();
    if (s.EndsWith("ms") && long.TryParse(s[..^2], out var ms)) return ms;
    if (s.EndsWith('s') && double.TryParse(s[..^1], out var sec)) return (long)(sec * 1000);
    if (s.EndsWith('m') && double.TryParse(s[..^1], out var m)) return (long)(m * 60_000);
    if (s.EndsWith('h') && double.TryParse(s[..^1], out var h)) return (long)(h * 3_600_000);
    if (s.EndsWith('d') && double.TryParse(s[..^1], out var d)) return (long)(d * 86_400_000);
    return 0;
}
```

## Notes

- The TOML is the single source of truth for runtime config — when a user saves the artifact, you reparse and propagate to the typed thread store. No second edit form.
- Markdown stays free-form so the LLM treats it as instruction; TOML is structured so the runtime can act on it deterministically.
- Always wrap `Toml.From<T>` in try/catch — the user is editing it by hand.
- Human-friendly duration strings (`5m`, `1h`, `2d`) → ms via a small parser. Round-trip back to the friendliest unit when generating TOML.

## See also

- `inline-list-cell-edit`
- `multi-step-wizard`
