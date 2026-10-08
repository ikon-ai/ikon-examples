# A Creator Discovery Crawler in a Single File

*Published 2026-03-19*

Finding the right social media creators for a marketing campaign is usually slow manual work. Someone opens a search engine, types keywords, scrolls through profiles, copies URLs into a spreadsheet, visits each one to check follower counts and bio relevance, then emails the team a CSV. The automated version needs a scraping cluster, a job queue, a database, a separate AI service for scoring, a proxy rotation layer, and a dashboard to view results. That is a lot of infrastructure for a job with four steps: search, scrape, rank and export.

This post looks at an influencer discovery tool built as a single Ikon AI App: about two thousand lines in one file, with no database, job queue or separate frontend. You describe your product in plain language, and the tool finds TikTok creators who match, scores each one with written reasoning, adds data from their other social platforms, and shows the results in a sortable, filterable interface with export to JSON and Mailchimp CSV. Multiple users can watch the crawl progress in real time.

## What you experience

You type a description of your product and the kind of creator you are looking for, such as "cozy roguelite adventure game with a wizard cat and antique shop." The tool uses AI to turn your description into search keywords, including phrasings you might not think of: "cozy roguelite," "wizard cat game," "indie RPG," "antique shop sim," "wholesome gaming."

Then the crawl begins. You watch it in real time in a color-coded, terminal-style event log: blue entries for search activity, purple for profile scraping, cyan for AI analysis, green for successes, yellow for warnings. Each entry has a millisecond timestamp. When the scraper hits a verification page, you see it immediately in yellow. When the AI finds a business email during enrichment, it appears in green.

A placeholder card appears in the results as soon as a profile URL is discovered. The card fills in with scraped data when the profile loads, and then with AI scores and enrichment data as they arrive. A crawl can take several minutes, and the log and cards show its progress the whole time.

## Multiuser without extra code

There is no multiuser code in this application. When the scraper finds a new profile, every connected user's results update. When the AI finishes scoring a batch, every user sees the scores appear. The shared crawl state (running status, profiles found, profiles scraped, event log and result cards) reaches every user automatically.

Some state is per user. Each person can switch tabs, select profiles, apply filters and choose a sort order independently. One user can be viewing the search configuration while another browses results sorted by follower count. The app has no synchronization code for either the shared crawl or each user's own view.

## AI keyword generation from natural language

You do not need to think in search engine terms. The tool sends your product description to an AI model, which generates twelve to eighteen keyword pairs of two to three words each. They cover different phrasings of the same idea, related terms, synonyms and adjacent topics. The AI generates more keywords rather than fewer, so the search covers more ground.

A creator search finds more of the right people when the keywords vary. A person might think of five keyword combinations. The AI's longer list also finds creators who describe their niche in unexpected ways.

## Three-stage parallel pipeline

The tool is built as three stages connected by queues, all running at the same time:

**Stage one: Discovery.** The system searches TikTok's native user search by driving a headless browser through each keyword, scrolling to load more results, and extracting profile links. Then it runs the same keywords through web search engines as a second source. Each source finds creators the other misses. TikTok's search returns people who match TikTok's own relevance algorithm, while web search finds creators whose profiles are indexed externally but might not rank well in TikTok's internal search.

Every discovered URL is normalized and deduplicated. The tool tracks "query hits", the number of different keyword searches that returned the same profile. A creator who appears in six out of twelve keyword searches is likely more relevant than one who appeared in just one. The most-matched profiles get processed first.

**Stage two: Scraping.** As URLs are discovered, the system loads each profile page, extracts email addresses, pulls follower counts, and gathers bio information. Scraping runs at the same time as discovery, so profiles from earlier keywords are being scraped while later keywords are still being searched.

**Stage three: Enrichment.** As profiles are scraped, they are batched and sent to an AI for scoring, then enriched with web search results for cross-platform presence.

The queues control the flow between the stages. If the scraper falls behind, discovery pauses until there is room in the queue. The queue size is the only setting to tune.

## AI scoring with written reasoning

The AI receives the campaign goal and a batch of profiles to evaluate:

```csharp
var prompt = $"""
    Analyze these TikTok influencer profiles and rank them for a marketing campaign.

    CAMPAIGN TARGET: {criteria}

    PROFILES TO RANK:
    {string.Join("\n", profileSummaries)}

    SCORING:
    - 80-100: Excellent fit for campaign based on bio/content
    - 60-79: Good fit with relevant content
    - 40-59: Moderate fit, some relevance
    - 20-39: Weak fit
    - 1-19: Poor fit
    """;
```

For each profile, the AI returns a numeric score and a written explanation. Instead of only "85", you get something like "This creator specializes in indie game reviews with a focus on cozy and roguelite genres, frequently features Steam demos, and has an engaged comment section asking for game recommendations."

The reasoning is visible in the interface, so you can understand why the AI ranked one creator above another and disagree if the logic is wrong. The scoring depends on the strategy you choose. "Small Creators + High Fit" tells the AI to prefer creators under 500K followers and to put content relevance first, while "Large Creators + High Reach" tells it to put audience size first.

## Cross-platform enrichment

After scoring, each profile goes through a second AI pass. A web search for the creator's handle pulls results from other platforms. The AI extracts cross-platform social handles (Instagram, YouTube, Twitter/X, Twitch, LinkedIn), additional email addresses, content niches, location hints, a profile summary, and a campaign fit analysis.

After enrichment, each creator's card shows their presence and reach on other platforms as well as TikTok.

## Session persistence and shareable URLs

Sessions are saved to cloud storage automatically every minute during active crawls. Each save holds the complete state: search query, ranking criteria, region filter, processed keywords, and every influencer card with its score, reasoning, emails, social presence, and thumbnails.

Each save also creates a public URL that anyone can open without logging in. If the browser crashes or the connection drops, the latest state is recoverable. If a colleague needs to see the results, you send them a link.

## Export to JSON and Mailchimp CSV

There are two export formats for different workflows. JSON export writes the full dataset, every field for every profile, to a downloadable file. Mailchimp CSV export generates a file with standard Mailchimp headers plus custom fields for score, TikTok handle, follower count, location, content niches, and AI notes. Only profiles with email addresses are included in the CSV, since you need an email to start a Mailchimp campaign. The export button turns into a download link once the file is generated.

## What this would take on a traditional stack

The equivalent system on a conventional stack requires: a scraping service with a headless browser pool and proxy rotation, a job queue to manage the pipeline stages, a database to store profiles and sessions and crawl state, an AI integration service with prompt templates and structured output parsing, a REST API, a frontend application with state management and real-time updates, an export service, and authentication for the sharing feature. That is at least five or six services, three or four infrastructure dependencies and a frontend application. Building it typically takes weeks, plus more time on operational problems such as reconnection handling, stale state and race conditions.

Here, the entire application is one file. It has no API, because the app code has no client-server boundary to cross, and no job queue, because in-process queues with backpressure do the same work. It has no database, because the asset system handles persistence, and no WebSocket configuration, because the framework handles the transport. There is one unit to deploy and operate.

## Takeaway

Plenty of influencer discovery platforms crawl profiles and score them with AI. What is unusual about this tool is how much it does for how little effort. It has a parallel three-stage pipeline with backpressure, multi-source search with deduplication, AI keyword generation, AI scoring with written reasoning, cross-platform enrichment, session persistence with shareable URLs, two export formats, color-coded real-time logging and multiuser support, all in about two thousand lines of a single file. Each of those features normally runs as a different service maintained by a different person. Here they are one application, connected by queues and shared state instead of network calls and message brokers. Because the scraping, AI orchestration, UI and state management all run in one process, the separate services and the integration code between them are not needed.
