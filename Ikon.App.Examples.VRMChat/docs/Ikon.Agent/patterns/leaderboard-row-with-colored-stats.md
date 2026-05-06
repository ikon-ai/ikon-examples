<!-- mined-from: PolymarketMirror -->
# Leaderboard Row With Colored Stats — Rank, Name, Threshold-Tinted Numbers

A clickable list row showing rank, display name, and 4-5 numeric metrics inline. Each number's color is computed from a threshold (PnL positive = emerald, negative = red; win-rate >=60 = emerald, >=50 = yellow, else neutral). Clicking the row opens a detail dialog and triggers a lazy-load of expensive metrics.

## When to use

Dashboards over a ranked dataset (top traders, leaderboards, top customers, top services by latency) where the user's eye must immediately spot good vs bad rows without reading numbers. Avoid for short lists (<10 rows) — coloring becomes noise.

## Snippet

```csharp
private void RenderWalletCard(UIView view, WalletProfile wallet)
{
    var pnlColor = wallet.RealizedPnlUsd >= 0 ? "text-emerald-400" : "text-red-400";
    var winRateColor = wallet.WinRate >= 60 ? "text-emerald-400"
        : wallet.WinRate >= 50 ? "text-yellow-400"
        : "text-zinc-400";

    view.Box(["flex flex-row items-center cursor-pointer hover:bg-zinc-800/50 p-4 transition-colors"],
        onClick: async () =>
        {
            _selectedWalletAddress.Value = wallet.Address;
            _walletDialogOpen.Value = true;

            if (!wallet.WinRateLoaded)
            {
                await FetchWalletDetailsAsync(wallet);
            }
        },
        content: view =>
        {
            view.Text(["w-12 text-sm text-zinc-500 font-mono"], $"#{wallet.Rank}");

            view.Column(["flex-1 min-w-0"], content: view =>
            {
                view.Row(["gap-2 items-center"], content: view =>
                {
                    var displayName = !string.IsNullOrWhiteSpace(wallet.UserName)
                        ? wallet.UserName
                        : ShortenAddress(wallet.Address);
                    view.Text(["font-semibold truncate"], displayName);

                    if (!string.IsNullOrWhiteSpace(wallet.TradingProfile) && wallet.TradingProfile != "Unclassified")
                    {
                        view.Text(["text-xs px-2 py-0.5 rounded-full bg-zinc-800 text-zinc-300"], wallet.TradingProfile);
                    }
                });

                view.Row(["gap-4 mt-1 text-sm"], content: view =>
                {
                    view.Text([pnlColor], $"PnL: ${wallet.RealizedPnlUsd:N0}");
                    view.Text(["text-zinc-400"], $"Vol: ${wallet.Volume:N0}");

                    if (wallet.WinRateLoaded)
                    {
                        view.Text([winRateColor], $"Win: {wallet.WinRate:0}%");
                    }

                    view.Text(["text-zinc-500"], $"Trades: {wallet.MarketsTraded}");
                });
            });
        });
}

private static string ShortenAddress(string address)
{
    if (string.IsNullOrWhiteSpace(address) || address.Length < 12) return address;
    return $"{address[..6]}...{address[^4..]}";
}
```

## Notes

- Color thresholds belong in the render method, not on the data record — different views of the same record may want different bands. Keep the model neutral.
- Leave a stat colorless ("Trades", "Vol") to anchor the row visually; coloring every number reads as a Christmas tree.
- Lazy-load is keyed off a `WinRateLoaded` flag on the record — initial crawl returns a partial profile, the detail click triggers `FetchWalletDetailsAsync` which mutates the record then sets the flag. Avoids 200 sequential HTTP calls on first render.
- Use `ShortenAddress` (or any deterministic truncation) for monospace identifiers — full hashes make the row uneven.

## See also

- `kpi-card-grid` — same threshold-color trick scaled up to headline metric tiles
- `expandable-detail-card` — when you want details inline rather than in a dialog
