<!-- mined-from: PolymarketMirror -->
# Dialog Stat-Card Grid — Detail Modal With Conditional-Color Tiles

A detail dialog (opened by clicking a list row) shows the entity's headline metrics as a 2-column `StatCard` grid. Each card is one helper call — `RenderStatCard(row, label, value, valueColor)` — so you can add, reorder, or recolor metrics by editing one line. The same threshold-coloring as the parent list row applies, but in a larger, scannable card form.

## When to use

Detail modal that pops over a leaderboard / list when the user clicks a row. The card grid lets you show ~6 metrics readably in a constrained dialog width without resorting to a full data-table. Pairs naturally with `leaderboard-row-with-colored-stats` — the row gives the headline numbers, the dialog gives the full set.

## Snippet

```csharp
private void RenderWalletDialog(UIView view)
{
    var wallet = GetSelectedWallet();
    if (wallet == null) return;

    view.Dialog(
        open: _walletDialogOpen.Value,
        onOpenChange: async open => _walletDialogOpen.Value = open ?? false,
        overlayStyle: [AlertDialog.Overlay],
        contentStyle: [AlertDialog.Content, "max-w-2xl max-h-[80vh] overflow-y-auto bg-zinc-900 border border-zinc-700"],
        contentSlot: content =>
        {
            content.Column([Layout.Column.Md], content: col =>
            {
                col.Row([Layout.Row.SpaceBetween, "items-start mb-4"], content: row =>
                {
                    row.Text(["text-xl font-bold text-white"], wallet.UserName);
                    row.Button([Button.GhostMd, Button.Icon],
                        onClick: async () => _walletDialogOpen.Value = false,
                        content: v => v.Icon([Icon.Default], name: "x"));
                });

                col.Row(["grid grid-cols-2 gap-4 mt-4"], content: row =>
                {
                    RenderStatCard(row, "Rank",         $"#{wallet.Rank}",                 "text-white");
                    RenderStatCard(row, "PnL",          $"${wallet.RealizedPnlUsd:N0}",    wallet.RealizedPnlUsd >= 0 ? "text-emerald-400" : "text-red-400");
                    RenderStatCard(row, "Volume",       $"${wallet.Volume:N0}",            "text-white");
                    RenderStatCard(row, "Win Rate",
                        wallet.WinRateLoaded ? $"{wallet.WinRate:0.0}%" : (_isLoadingDetails.Value ? "Loading..." : "N/A"),
                        wallet.WinRate >= 60 ? "text-emerald-400" : wallet.WinRate >= 50 ? "text-yellow-400" : "text-zinc-400");
                    RenderStatCard(row, "Markets Traded", wallet.MarketsTraded.ToString(),  "text-white");
                    RenderStatCard(row, "ROI",          $"{wallet.RoiPercent:0.0}%",       wallet.RoiPercent >= 0 ? "text-emerald-400" : "text-red-400");
                });

                col.Row(["mt-6 gap-2"], content: row =>
                {
                    row.Button([Button.NeutralMd], "Polymarket Profile", href: GetPolymarketProfileUrl(wallet.Address));
                    row.Button([Button.GhostMd], "PolygonScan", href: GetPolygonScanUrl(wallet.Address));
                });
            });
        });
}

private static void RenderStatCard(UIView view, string label, string value, string valueColor)
{
    view.Column(["bg-zinc-800 rounded-lg p-3 border border-zinc-700"], content: col =>
    {
        col.Text(["text-xs text-zinc-400 font-medium uppercase tracking-wide"], label);
        col.Text([$"text-lg font-bold mt-1 {valueColor}"], value);
    });
}
```

## Notes

- `RenderStatCard` is a `static` helper that takes the value already formatted — keeps the call sites compact (one line per metric) and lets the call site decide the color from the same threshold logic the row uses.
- "N/A" / "Loading..." / value strings all live in the call-site ternary — don't push the "is data ready" decision into the helper.
- `max-h-[80vh] overflow-y-auto` on the dialog content lets long detail content (recent activity table) scroll without bursting the modal off the viewport.
- Add a row of deep-link buttons (profile pages, on-chain explorers) at the bottom — invaluable for "this card is a stub, give me the full external context".

## See also

- `kpi-card-grid` — same card grid as a top-of-page banner instead of in a modal
- `leaderboard-row-with-colored-stats` — the list row this dialog typically opens from
