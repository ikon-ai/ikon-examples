<!-- mined-from: PolymarketMirror -->
# Heuristic Classify-And-Tag — Static Rules Tagging Each Record Post-Fetch

Right after fetching raw records from an external API, walk the list and apply a static heuristic classifier (`ClassifyTradingProfile`) that tags each record with a category string ("Whale", "Sniper", "Active Trader", "Arbitrage", "Unclassified"). A second function (`SuggestTradeSignal`) maps the tag to a human-readable advisory message. Tags drive a downstream filter dropdown and a colored chip on each row.

## When to use

You ingest a list of opaque records (transactions, users, sensors, alerts) and want to enrich them with a derived category before showing them. Heuristic rules beat an LLM call when (a) classification is local-only and cheap, (b) the rule set is stable, and (c) you need the tag synchronously for sorting/filtering. Promote to LLM-classification when the rules become a switch with 20+ branches.

## Snippet

```csharp
private async Task CrawlWalletsAsync()
{
    if (_isCrawling.Value) return;
    _isCrawling.Value = true;
    _status.Value = "Fetching leaderboard data...";

    try
    {
        var crawled = await FetchLeaderboardWalletsAsync();
        if (crawled.Count == 0) { _status.Value = "Failed to fetch"; return; }

        foreach (var wallet in crawled)
        {
            wallet.TradingProfile = ClassifyTradingProfile(wallet);
            wallet.LastSignal = SuggestTradeSignal(wallet);
            wallet.LastSeenUtc = DateTime.UtcNow;
        }

        _wallets.ReplaceAll(crawled);
        await SaveWalletStoreAsync();
        _status.Value = $"Loaded {_wallets.Count} wallets";
    }
    finally { _isCrawling.Value = false; }
}

private static string ClassifyTradingProfile(WalletProfile wallet)
{
    if (wallet.Volume > 1_000_000) return "Whale";
    if (wallet.RoiPercent > 50 && wallet.Volume < 100_000) return "Sniper";
    if (wallet.MarketsTraded > 50) return "Active Trader";
    if (wallet.RoiPercent > 20 && wallet.RoiPercent < 50) return "Arbitrage";
    return "Unclassified";
}

private static string SuggestTradeSignal(WalletProfile wallet)
{
    return wallet.TradingProfile switch
    {
        "Whale"         => "Large position trader - follow major directional bets with caution",
        "Sniper"        => "High-conviction trader - mirror early entries in high-volume markets",
        "Active Trader" => "Diversified approach - good for learning market patterns",
        "Arbitrage"     => "Consistent returns - follow for steady gains in correlated markets",
        _               => "Analyze recent trades before mirroring"
    };
}
```

## Notes

- Order rules from most-specific to most-general: "Whale" before "Active Trader" so a high-volume frequent trader gets the better label.
- Keep the classifier static and pure — no DB access, no LLM, no async. That makes it cheap to call inside `OrderBy` clauses or filter predicates if needed.
- Pair the tag with an actionable signal string. The tag alone is a label; "follow major directional bets with caution" is value.
- Never silently leave fields default — set "Unclassified" / "No actionable signal available" so list rendering shows *something* and downstream filters can drop those rows explicitly.
- Persist the tag onto the record before saving the store. Re-classifying on every page render wastes work and obscures bugs in the rules.

## See also

- `filter-button-group` — the downstream filter that lets users pick by tag
- `status-badge-from-enum` — how to render the tag as a colored chip on each row
