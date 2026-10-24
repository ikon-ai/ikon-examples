namespace Ikon.App.Patterns.Patterns;

// Pattern: toast-notifications — see docs/patterns/toast-notifications.md.
internal sealed class ToastNotifications : IPatternDemo
{
    public string Slug => "toast-notifications";
    public string Title => "Toast notifications";
    public string Category => "Feedback";

    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-toast-notifications
    // One queue for the app. Each client sees only the toasts its own handlers raise
    private readonly Toasts _toasts = new();

    private readonly ReactiveList<string> _tasks = new(["Draft the brief", "Book the venue", "Send the invites"]);

    // What this client removed last, kept until its Undo toast closes
    private readonly ClientReactive<(int Index, string Task)?> _removed = new(((int, string)?)null);

    private async Task SaveAsync()
    {
        // ... the awaited save ...
        _toasts.Success("Draft saved");
    }

    private void Remove(int index)
    {
        var task = _tasks[index];
        _tasks.RemoveAt(index);
        _removed.Value = (index, task);
    }

    private void Undo((int Index, string Task) removed)
    {
        _tasks.Insert(Math.Min(removed.Index, _tasks.Count), removed.Task);
        _removed.Value = null;
    }

    private void Render(IView view)
    {
        view.Column(["w-full gap-3"], content: view =>
        {
            view.Row(["gap-2 flex-wrap"], content: row =>
            {
                row.Button([Button.PrimaryMd], "Save draft", onClick: SaveAsync);
                row.Button([Button.OutlineMd], "Send a test alert", onClick: () => _toasts.Info("Test alert sent"));
                row.Button([Button.OutlineMd], "Upload a large file",
                    onClick: () => _toasts.Error("Couldn't upload the file", "Files can be up to 25 MB."));
            });

            for (var i = 0; i < _tasks.Count; i++)
            {
                var index = i;
                view.Row(["items-center justify-between gap-2"], content: row =>
                {
                    row.Text([Text.Body], _tasks[index]);
                    row.Button([Button.GhostSm], "Remove", onClick: () => Remove(index));
                });
            }

            // Mount the queue once, at the root of the UI
            view.ToastHost(_toasts);

            // A toast with an action is a view.Toast you control. It closes on Undo or when its time runs out
            if (_removed.Value is { } removed)
            {
                view.Toast(open: true, durationMs: 8000, key: $"undo-{removed.Index}-{removed.Task}",
                    onOpenChange: async open =>
                    {
                        if (!open)
                        {
                            _removed.Value = null;
                        }
                    },
                    content: toast => toast.Row(["items-center gap-3"], content: row =>
                    {
                        row.Text([Text.Body], $"Removed \"{removed.Task}\"");
                        row.Button([Button.OutlineSm], "Undo", onClick: () => Undo(removed));
                    }));
            }
        });
    }
    #endregion
}
