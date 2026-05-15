<!-- mined-from: MiniAgent -->
# Plan Approval Modal — Pause The Agent For Human Review

When the agent has produced a plan, halt before execution and show the user a modal with three actions: **Refine** (free-text feedback that re-runs planning), **Looks Good** (approve and execute), **Start Over** (cancel and start fresh). The modal is gated by `_pendingApproval.Value != null` and rendered as a `Dialog` with markdown-rendered body.

## When to use

Agentic apps where you want a human-in-the-loop checkpoint between planning and execution. Critical for code-modification agents, anything that touches production, or domains where plan errors are expensive.

## Snippet

```csharp
private readonly ClientReactive<PendingApproval?> _pendingApproval = new(null);
private readonly ClientReactive<string> _planFeedback = new("");

private void RenderPlanApprovalModal(UIView view)
{
    if (_pendingApproval.Value == null) return;

    view.Dialog(
        open: true,
        onOpenChange: async open =>
        {
            if (open == false) await HandlePlanApprovalAsync(false);
        },
        overlayStyle: ["fixed inset-0 bg-black/70 backdrop-blur-sm z-50"],
        contentStyle: [
            "fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50",
            "w-full max-w-3xl max-h-[85vh] overflow-hidden flex flex-col",
            "bg-zinc-900 border border-zinc-700/50 rounded-2xl",
        ],
        content: dialog =>
        {
            dialog.Box(["p-6 border-b border-zinc-800"], content: header =>
            {
                header.Text([Text.H2, "text-zinc-100"], "Review Plan");
                header.Text([Text.Body, "text-zinc-400"], "Review the proposed implementation plan before execution.");
            });

            dialog.Box(["flex-1 p-6 overflow-y-auto"], content: scroll =>
            {
                scroll.Box(["rounded-xl p-5 bg-zinc-800/50 border border-zinc-700/50"], content: c =>
                    c.Markdown([Text.Body, "text-zinc-200 prose-invert prose-sm max-w-none"], _pendingApproval.Value.Plan));
            });

            dialog.Box(["p-6 border-t border-zinc-800"], content: footer =>
            {
                footer.Row(["gap-3 mb-4"], content: row =>
                {
                    row.TextField(
                        ["flex-1 px-4 py-2.5 rounded-xl bg-zinc-800 border border-zinc-700"],
                        value: _planFeedback.Value,
                        placeholder: "Suggest changes to the plan...",
                        onValueChange: async v => _planFeedback.Value = v ?? "");

                    footer.Button(
                        ["px-4 py-2.5 rounded-xl bg-amber-500/20 text-amber-400 border border-amber-500/30"],
                        label: "Refine Plan",
                        disabled: string.IsNullOrWhiteSpace(_planFeedback.Value),
                        onClick: async () => await HandlePlanRefinementAsync());
                });

                footer.Row(["justify-end gap-3"], content: buttons =>
                {
                    buttons.Button(["px-4 py-2.5 rounded-xl bg-red-500/10 text-red-400 border border-red-500/30"],
                        label: "Start Over", onClick: async () => await HandlePlanApprovalAsync(false));

                    buttons.Button(["px-5 py-2.5 rounded-xl bg-gradient-to-r from-emerald-500 to-cyan-500 text-white"],
                        label: "Looks Good, Execute", onClick: async () => await HandlePlanApprovalAsync(true));
                });
            });
        });
}
```

## Notes

- Header / scrolling body / footer is the standard three-section dialog layout — `flex-col` with `flex-1 overflow-y-auto` on the body lets long plans scroll without resizing the dialog.
- Plan body uses `Markdown` not `Text` — the LLM emits markdown lists/headings naturally.
- The "Refine" action is *not* a third button on the bottom row — it's tied to the feedback input above so the affordance is "type → submit".
- `onOpenChange == false` (ESC, backdrop, X) maps to `Start Over` — be explicit about what dismissal means.
- Always disable Refine until feedback is non-empty; the agent has no signal otherwise.

## See also

- `streaming-agent-status` — the activity bar that runs while the agent IS executing the approved plan
- `destructive-confirm-dialog` — same dialog shape for destructive actions
