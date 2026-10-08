# A Live Prediction Markets Dashboard in Under a Thousand Lines

*Published 2026-03-19*

This prediction markets dashboard crawls a public leaderboard, classifies wallets by trading behavior, gets copy-trading recommendations from an AI model, saves snapshots to the cloud, and shows a filterable, searchable interface to every connected viewer at once. It is one file, with no API routes, client-side data fetching, state management library, or real-time infrastructure setup.

The dashboard has the same parts as any other dashboard: data fetching, pagination, data transformation, filtering, AI integration, and persistence. The difference is how little code it takes to connect them.

## What you get

The dashboard pulls wallet data from a public prediction markets leaderboard. It shows each wallet's rank, profit and loss, trading volume, win rate, and return on investment. Each wallet is classified by its trading behavior into one of four profiles: Whale, Sniper, Active Trader, or Arbitrage. You can filter by profile type, sort by different metrics, search by address or username, and set a minimum win rate.

Click any wallet row and a detail panel opens with the full profile: trading history, performance metrics, classification rationale, and links to external pages.

## Shared by default

Every connected viewer sees the same dashboard, data, and AI analysis, updated in real time. When one analyst starts a data refresh, every viewer sees the progress, the wallet list filling in, and the final count. When someone asks the AI for insights, the response appears for everyone.

The application has no broadcast logic, event channels, or conflict resolution code. The platform keeps the state in sync for every viewer.

## Loading in two phases

The dashboard loads in two phases. Phase one quickly fetches the leaderboard: rank, profit/loss, volume, and address. You can browse, filter, and sort as soon as it finishes.

Phase two runs in the background. It fetches detailed activity data for the top wallets and computes win rates from their trading history. The dashboard updates as each wallet's details arrive, so you can start working before everything has loaded.

On startup, the dashboard also checks for a previously saved snapshot and loads it instantly, so you see data the moment you open the page. You can refresh on demand whenever you want current numbers.

## Wallet classification

Each wallet is classified into one of these trading profiles:

- **Whale** -- moves over a million in volume. Large position traders.
- **Sniper** -- high return on investment on relatively small positions. High-conviction, early-entry traders.
- **Active Trader** -- diversified across many markets.
- **Arbitrage** -- moderate returns, likely exploiting price differences.

The classification code is short enough to read as its own documentation:

```csharp
private static string ClassifyTradingProfile(WalletProfile wallet)
{
    if (wallet.Volume > 1_000_000) return "Whale";
    if (wallet.RoiPercent > 50 && wallet.Volume < 100_000) return "Sniper";
    if (wallet.MarketsTraded > 50) return "Active Trader";
    if (wallet.RoiPercent > 20 && wallet.RoiPercent < 50) return "Arbitrage";
    return "Unclassified";
}
```

It uses fixed thresholds rather than machine learning, and the groups it produces are useful for filtering and analysis.

Each profile also gets a brief signal description, like "High-conviction trader -- mirror early entries in high-volume markets."

## AI analysis

The dashboard has an AI insights panel. You type a question, such as "Which wallets should I mirror this week and why?", and the app sends it to an AI model together with the top wallets from the current filter. The model receives the actual numbers for profit/loss, volume, and win rate. It returns a short summary, three to five trade ideas based on the top traders' patterns, and three wallet addresses to watch.

The model is set up to give conservative recommendations based on the data rather than creative speculation.

## Filtering, sorting, and search

You can narrow and order the data by time period (all time, month, week, day), sort order (profit/loss, volume, win rate), profile type, minimum win rate, and free-text search across addresses and usernames.

When you change a dropdown or type a search query, the list is recalculated and updated immediately for every connected viewer.

## Cloud persistence

The dashboard saves wallet data to the cloud after each crawl and loads the most recent snapshot on startup. That is all the persistence code does, and it needs no database schema, migrations, or configuration.

## What the traditional stack looks like

The traditional version of this dashboard is well understood. It has a React frontend, a backend with API endpoints for crawling, filtering, and AI calls, a state management library on the client, and data fetching with loading states and error handling. Real-time updates need WebSocket or server-sent event infrastructure. Showing the same data to several people needs a publish/subscribe layer, and persistence needs a database with migrations.

None of those pieces are hard on their own, and Ikon makes most of them unnecessary. There are no API routes because the server renders the interface directly. There is no client-side state management because state lives on the server. There is no real-time setup because the platform handles it. There is no serialization boundary to maintain because the same language defines the data, the logic, and the interface.

The code that fetches the leaderboard data, filters it, and builds the AI prompt looks the same as it would anywhere. What goes away is the code that connects the browser to the server: the fetch calls, the loading indicators, the error boundaries, the state synchronization, the reconnection logic, and keeping client and server types in step.

## Takeaway

A prediction markets dashboard is not a new kind of application. It crawls data, classifies it, shows it in a filterable list, and lets an AI analyze it. On a traditional stack, building it takes much more work than that simple description suggests. API endpoints, client state management, real-time infrastructure, and deploying separate services are not hard problems, but each new feature adds more of that work.

On Ikon, the whole application is a single file under a thousand lines. The crawl logic, the classification, the filtering, the AI integration, the persistence, the expandable detail views, and the multiuser support are all in one place and one language, with no infrastructure to configure. None of these features are unique. What is unusual is getting all of them with so little setup code.
