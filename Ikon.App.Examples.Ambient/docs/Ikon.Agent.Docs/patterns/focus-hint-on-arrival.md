<!-- mined-from: Ikon.App.Patterns -->
# Announcing What Just Arrived — FocusHint And Live Regions

Content that appears **without the user acting for it** — a new message, a status change, a result
that finished — is invisible to a screen reader by default. The reader is not looking at the part
of the page that changed, and nothing tells them it did.

`view.FocusHint` maps to an ARIA live region and closes that gap. It is the accessibility half of
every pattern that pushes content onto the screen: a chat transcript, a toast, a status pill, a
live leaderboard.

## When to use

Anywhere the app changes the screen on its own. Not for content the user navigated to — they know
they went there, and announcing it is noise.

## Notes

- **`FocusPriority.Assertive` INTERRUPTS** whatever is being read. Reserve it for something that
  genuinely cannot wait. `Polite` queues behind the current utterance and is right for almost
  everything; a screen full of assertive regions is unusable.
- **`Ranking` orders competing hints** when several arrive together, so the most important one is
  announced first rather than whichever rendered last.
- **`Cooldown` suppresses re-announcing a region while it churns.** Without it a list that updates
  every second talks continuously, which is worse than silence — users switch the screen reader off.
- `FocusOnly` moves focus without announcing, for when the visible change already says what
  happened and a second spoken copy would be redundant.
- This is not a substitute for accessible names on controls. `label:` on a field, `aria-label` via
  `props:` on an icon-only button, and a real label on a Checkbox all still apply — a live region
  announces *change*, not *identity*.

## Snippet

```csharp
private readonly ClientReactiveList<Alert> _alerts = new();

private void Render(IView view)
{
    view.Column(["gap-2"], content: col =>
    {
        foreach (var alert in _alerts)
        {
            col.Column(["gap-1"], key: alert.Id, content: item =>
            {
                item.Text(text: alert.Text);

                // FocusHint maps to an ARIA live region. Content that appears without the
                // user acting for it is invisible to a screen reader otherwise -- they are
                // not looking at the part of the page that changed.
                item.FocusHint(new FocusHintProps
                {
                    // Assertive INTERRUPTS whatever is being read. Reserve it for something
                    // that cannot wait; Polite queues behind the current utterance and is
                    // right for almost everything.
                    Priority = alert.Urgent ? FocusPriority.Assertive : FocusPriority.Polite,

                    // Ranking orders competing hints when several arrive together, so the
                    // most important one is announced first rather than the last to render.
                    Ranking = alert.Urgent ? 100 : 0,

                    // Cooldown suppresses re-announcing the same region while it churns --
                    // without it a list that updates every second talks continuously.
                    Cooldown = TimeSpan.FromSeconds(2),

                    // FocusOnly moves focus without announcing, for when the visible change
                    // already says what happened.
                    FocusOnly = false,
                });
            });
        }
    });
}
```

## See also

- `toast-notifications` — the visible half of the same event.
- `connection-status-pill` — a status that changes without the user asking.
