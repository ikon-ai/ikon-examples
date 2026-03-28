using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;

return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParams(string Name = "Ikon");

public sealed class WalletProfile
{
    public int Rank { get; set; }
    public string Address { get; set; } = "";
    public string UserName { get; set; } = "";
    public string ProfileImage { get; set; } = "";
    public double WinRate { get; set; }
    public double RoiPercent { get; set; }
    public double RealizedPnlUsd { get; set; }
    public double Volume { get; set; }
    public int MarketsTraded { get; set; }
    public string TradingProfile { get; set; } = "Unclassified";
    public string LastSignal { get; set; } = "No actionable signal available";
    public DateTime LastSeenUtc { get; set; }
    public List<TradeActivity> RecentTrades { get; set; } = [];
    public bool WinRateLoaded { get; set; }
}

public sealed class TradeActivity
{
    public DateTime TimestampUtc { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Side { get; set; } = "";
    public double UsdcSize { get; set; }
    public double Price { get; set; }
}

public sealed class WalletStore
{
    public DateTime UpdatedAtUtc { get; set; }
    public List<WalletProfile> Wallets { get; set; } = [];
}

public sealed class InsightResponse
{
    public string Summary { get; set; } = "";
    public List<string> SuggestedTrades { get; set; } = [];
    public List<string> MirrorCandidates { get; set; } = [];
}

public sealed class LeaderboardEntry
{
    [JsonPropertyName("rank")]
    public string Rank { get; set; } = "";

    [JsonPropertyName("proxyWallet")]
    public string ProxyWallet { get; set; } = "";

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = "";

    [JsonPropertyName("pnl")]
    public double Pnl { get; set; }

    [JsonPropertyName("vol")]
    public double Vol { get; set; }

    [JsonPropertyName("profileImage")]
    public string ProfileImage { get; set; } = "";
}

public sealed class ActivityEntry
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = "";

    [JsonPropertyName("side")]
    public string Side { get; set; } = "";

    [JsonPropertyName("usdcSize")]
    public double UsdcSize { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; }
}

[App]
public class PolymarketMirror(IApp<SessionIdentity, ClientParams> app)
{
    private const string WalletStorePath = "polymarket-wallet-store.json";

    private UI UI { get; } = new(app, new Theme());
    private readonly HttpClient _httpClient = new();

    private readonly Reactive<List<WalletProfile>> _wallets = new([]);
    private readonly Reactive<bool> _isCrawling = new(false);
    private readonly Reactive<string> _status = new("Ready");
    private readonly Reactive<string> _searchQuery = new("");
    private readonly Reactive<string> _profileFilter = new("all");
    private readonly Reactive<string> _chatQuestion = new("Which wallets should I mirror this week and why?");
    private readonly Reactive<string> _chatAnswer = new("Run crawl and ask for strategy insights");
    private readonly Reactive<string> _minWinRate = new("0");
    private readonly Reactive<string> _selectedWalletAddress = new("");
    private readonly Reactive<bool> _walletDialogOpen = new(false);
    private readonly Reactive<bool> _isLoadingDetails = new(false);

    private readonly Reactive<int> _maxWallets = new(200);
    private readonly Reactive<string> _timePeriod = new("ALL");
    private readonly Reactive<string> _sortBy = new("pnl");

    public async Task Main()
    {
        await LoadWalletStoreAsync();

        UI.Root([Page.Default, "bg-zinc-950 text-zinc-100"], content: view =>
        {
            view.Column([Container.Xl2, Layout.Column.Lg, "py-8 px-4"], content: view =>
            {
                RenderHeader(view);
                RenderMainContent(view);
                RenderAiChat(view);

                if (_walletDialogOpen.Value)
                {
                    RenderWalletDialog(view);
                }
            });
        });
    }

    private void RenderHeader(UIView view)
    {
        view.Column([Card.Default, "p-6 bg-zinc-900/80 border-zinc-800"], content: view =>
        {
            view.Text(["text-3xl font-bold text-zinc-100"], "Polymarket Mirror Intelligence");
            view.Text([Text.Muted], "Crawl top wallets from Polymarket leaderboard and analyze trading patterns");

            view.Row(["flex flex-row flex-wrap gap-4 mt-4"], content: view =>
            {
                view.Column([FormField.Root, "min-w-[160px]"], content: view =>
                {
                    view.Text([FormField.Label], $"Max Wallets: {_maxWallets.Value}");
                    view.Slider(
                        [Slider.Default],
                        min: 50,
                        max: 500,
                        step: 50,
                        value: [_maxWallets.Value],
                        onValueChange: v =>
                        {
                            if (v.Count > 0)
                            {
                                _maxWallets.Value = (int)v[0];
                            }

                            return Task.CompletedTask;
                        },
                        content: view =>
                        {
                            view.SliderTrack([Slider.Track], content: view =>
                            {
                                view.SliderRange([Slider.Range]);
                            });
                            view.SliderThumb([Slider.Thumb]);
                        });
                });

                view.Column([FormField.Root, "min-w-[140px]"], content: view =>
                {
                    view.Text([FormField.Label], "Time Period");
                    view.Select(
                        triggerStyle: ["w-full"],
                        options:
                        [
                            new SelectOption("ALL", "All Time"),
                            new SelectOption("MONTH", "Month"),
                            new SelectOption("WEEK", "Week"),
                            new SelectOption("DAY", "Day")
                        ],
                        value: _timePeriod.Value,
                        onValueChange: async value => _timePeriod.Value = value);
                });

                view.Column([FormField.Root, "min-w-[140px]"], content: view =>
                {
                    view.Text([FormField.Label], "Sort By");
                    view.Select(
                        triggerStyle: ["w-full"],
                        options:
                        [
                            new SelectOption("pnl", "PnL"),
                            new SelectOption("volume", "Volume"),
                            new SelectOption("winrate", "Win Rate")
                        ],
                        value: _sortBy.Value,
                        onValueChange: async value => _sortBy.Value = value);
                });

                view.Column([FormField.Root, "min-w-[100px]"], content: view =>
                {
                    view.Text([FormField.Label], "Min Win %");
                    view.TextField([Input.Default], value: _minWinRate.Value, onValueChange: async value => _minWinRate.Value = value);
                });

                view.Column([FormField.Root, "min-w-[180px]"], content: view =>
                {
                    view.Text([FormField.Label], "Profile Filter");
                    view.Select(
                        triggerStyle: ["w-full"],
                        options:
                        [
                            new SelectOption("all", "All"),
                            new SelectOption("arbitrage", "Arbitrage"),
                            new SelectOption("whale", "Whale"),
                            new SelectOption("active", "Active Trader"),
                            new SelectOption("sniper", "Sniper")
                        ],
                        value: _profileFilter.Value,
                        onValueChange: async value => _profileFilter.Value = value);
                });

                view.Column([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                {
                    view.Text([FormField.Label], "Search");
                    view.TextField([Input.Default], value: _searchQuery.Value, placeholder: "Address or username...", onValueChange: async value => _searchQuery.Value = value);
                });
            });

            view.Row(["flex flex-row gap-2 mt-4"], content: view =>
            {
                view.Button([Button.PrimaryMd], _isCrawling.Value ? "Crawling..." : "Crawl Wallets", disabled: _isCrawling.Value, onClick: CrawlWalletsAsync);
                view.Button([Button.SecondaryMd], "Generate AI Insights", disabled: _wallets.Value.Count == 0, onClick: GenerateInsightsAsync);
            });

            view.Text([Text.Small, "mt-3 text-zinc-400"], _status.Value);
        });
    }

    private void RenderMainContent(UIView view)
    {
        var filtered = GetFilteredWallets();

        view.Column([Card.Default, "p-6 bg-zinc-900/80 border-zinc-800"], content: view =>
        {
            view.Row([Layout.Row.SpaceBetween, "items-center mb-4"], content: view =>
            {
                view.Text([Text.H3], $"Wallets ({filtered.Count})");

                if (_wallets.Value.Count > 0)
                {
                    var avgWinRate = filtered.Where(w => w.WinRateLoaded).Select(w => w.WinRate).DefaultIfEmpty(0).Average();
                    var totalPnl = filtered.Sum(w => w.RealizedPnlUsd);
                    view.Row(["gap-4 text-sm text-zinc-400"], content: view =>
                    {
                        view.Text([], $"Avg Win Rate: {avgWinRate:0.0}%");
                        view.Text([], $"Total PnL: ${totalPnl:N0}");
                    });
                }
            });

            if (filtered.Count == 0)
            {
                view.Text([Text.Muted, "py-8 text-center"], _wallets.Value.Count == 0
                    ? "Click 'Crawl Wallets' to fetch data from Polymarket leaderboard"
                    : "No wallets match the current filters");
            }
            else
            {
                view.Column(["divide-y divide-zinc-800"], content: view =>
                {
                    foreach (var wallet in filtered.Take(50))
                    {
                        RenderWalletCard(view, wallet);
                    }
                });

                if (filtered.Count > 50)
                {
                    view.Text([Text.Small, "mt-4 text-center text-zinc-500"], $"Showing 50 of {filtered.Count} wallets. Refine filters to see more.");
                }
            }
        });
    }

    private void RenderWalletCard(UIView view, WalletProfile wallet)
    {
        var pnlColor = wallet.RealizedPnlUsd >= 0 ? "text-emerald-400" : "text-red-400";
        var winRateColor = wallet.WinRate >= 60 ? "text-emerald-400" : wallet.WinRate >= 50 ? "text-yellow-400" : "text-zinc-400";

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

                view.Button([Button.GhostMd, "ml-2"], "View", onClick: async () =>
                {
                    _selectedWalletAddress.Value = wallet.Address;
                    _walletDialogOpen.Value = true;

                    if (!wallet.WinRateLoaded)
                    {
                        await FetchWalletDetailsAsync(wallet);
                    }
            });
        });
    }

    private void RenderWalletDialog(UIView view)
    {
        var wallet = GetSelectedWallet();

        if (wallet == null)
        {
            return;
        }

        view.Dialog(
            open: _walletDialogOpen.Value,
            onOpenChange: async open => _walletDialogOpen.Value = open ?? false,
            overlayStyle: [AlertDialog.Overlay],
            contentStyle: [AlertDialog.Content, "max-w-2xl max-h-[80vh] overflow-y-auto bg-zinc-900 text-zinc-100 border border-zinc-700"],
            contentSlot: content =>
            {
                content.Column([Layout.Column.Md], content: col =>
                {
                    col.Row([Layout.Row.SpaceBetween, "items-start mb-4"], content: row =>
                    {
                        row.Column([], content: col =>
                        {
                            var displayName = !string.IsNullOrWhiteSpace(wallet.UserName)
                                ? wallet.UserName
                                : ShortenAddress(wallet.Address);
                            row.Text(["text-xl font-bold text-white"], displayName);

                            if (!string.IsNullOrWhiteSpace(wallet.TradingProfile) && wallet.TradingProfile != "Unclassified")
                            {
                                row.Text(["text-sm px-2 py-0.5 rounded-full bg-emerald-600 text-white ml-2"], wallet.TradingProfile);
                            }
                        });

                        row.Button(
                            [Button.GhostMd, Button.Size.Icon, "text-zinc-400 hover:text-white"],
                            onClick: async () => _walletDialogOpen.Value = false,
                            content: v => v.Icon([Icon.Default], name: "x"));
                    });

                    col.Column([Layout.Column.Sm, "bg-zinc-800 rounded-lg p-4 border border-zinc-700"], content: col =>
                    {
                        col.Text([Text.Small, "text-zinc-400 font-medium"], "Wallet Address");
                        col.Text(["font-mono text-sm break-all text-zinc-200"], wallet.Address);
                    });

                    col.Row(["grid grid-cols-2 gap-4 mt-4"], content: row =>
                    {
                        RenderStatCard(row, "Rank", $"#{wallet.Rank}", "text-white");
                        RenderStatCard(row, "PnL", $"${wallet.RealizedPnlUsd:N0}", wallet.RealizedPnlUsd >= 0 ? "text-emerald-400" : "text-red-400");
                        RenderStatCard(row, "Volume", $"${wallet.Volume:N0}", "text-white");
                        RenderStatCard(row, "Win Rate", wallet.WinRateLoaded ? $"{wallet.WinRate:0.0}%" : (_isLoadingDetails.Value ? "Loading..." : "N/A"),
                            wallet.WinRate >= 60 ? "text-emerald-400" : wallet.WinRate >= 50 ? "text-yellow-400" : "text-zinc-400");
                        RenderStatCard(row, "Markets Traded", wallet.MarketsTraded.ToString(), "text-white");
                        RenderStatCard(row, "ROI", $"{wallet.RoiPercent:0.0}%", wallet.RoiPercent >= 0 ? "text-emerald-400" : "text-red-400");
                    });

                    if (!string.IsNullOrWhiteSpace(wallet.LastSignal) && wallet.LastSignal != "No actionable signal available")
                    {
                        col.Column([Layout.Column.Xs, "mt-4 bg-zinc-800 rounded-lg p-4 border border-zinc-700"], content: col =>
                        {
                            col.Text([Text.Small, "text-zinc-400 font-medium"], "Trading Signal");
                            col.Text(["text-zinc-200"], wallet.LastSignal);
                        });
                    }

                    if (wallet.RecentTrades.Count > 0)
                    {
                        col.Column([Layout.Column.Sm, "mt-4"], content: col =>
                        {
                            col.Text(["text-base font-semibold text-white"], "Recent Activity");
                            col.Column(["divide-y divide-zinc-700 mt-2 bg-zinc-800 rounded-lg border border-zinc-700"], content: col =>
                            {
                                foreach (var trade in wallet.RecentTrades.Take(5))
                                {
                                    col.Row(["py-3 px-4 text-sm"], content: row =>
                                    {
                                        row.Column(["flex-1 min-w-0"], content: col =>
                                        {
                                            col.Text(["truncate text-zinc-200"], trade.Title);
                                            col.Row(["gap-2 text-xs text-zinc-400"], content: row =>
                                            {
                                                row.Text([], trade.Type);

                                                if (!string.IsNullOrWhiteSpace(trade.Side))
                                                {
                                                    row.Text([], $"• {trade.Side}");
                                                }

                                                if (!string.IsNullOrWhiteSpace(trade.Outcome))
                                                {
                                                    row.Text([], $"• {trade.Outcome}");
                                                }
                                            });
                                        });
                                        row.Text(["text-right text-zinc-300"], $"${trade.UsdcSize:N0}");
                                    });
                                }
                            });
                        });
                    }

                    col.Row(["mt-6 gap-2"], content: row =>
                    {
                        row.Button([Button.SecondaryMd], "Polymarket Profile", href: GetPolymarketProfileUrl(wallet.Address));
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

    private void RenderAiChat(UIView view)
    {
        view.Column([Card.Default, "p-6 bg-zinc-900/80 border-zinc-800"], content: view =>
        {
            view.Text([Text.H3], "AI Insights");
            view.Column([FormField.Root, "mt-3"], content: view =>
            {
                view.TextField([Input.Default], value: _chatQuestion.Value, onValueChange: async value => _chatQuestion.Value = value);
            });
            view.Button([Button.SecondaryMd, "mt-3"], "Ask", disabled: _wallets.Value.Count == 0, onClick: GenerateInsightsAsync);
            view.Text([Text.Muted, "mt-3 whitespace-pre-wrap"], _chatAnswer.Value);
        });
    }

    private List<WalletProfile> GetFilteredWallets()
    {
        var minWinRate = double.TryParse(_minWinRate.Value, out var parsed) ? parsed : 0;
        var filtered = _wallets.Value.AsEnumerable();

        if (minWinRate > 0)
        {
            filtered = filtered.Where(w => w.WinRateLoaded && w.WinRate >= minWinRate);
        }

        if (!string.IsNullOrWhiteSpace(_searchQuery.Value))
        {
            var query = _searchQuery.Value.ToLowerInvariant();
            filtered = filtered.Where(w =>
                w.Address.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(w.UserName) && w.UserName.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (_profileFilter.Value != "all")
        {
            filtered = filtered.Where(w => NormalizeProfile(w.TradingProfile) == _profileFilter.Value);
        }

        var sorted = _sortBy.Value switch
        {
            "volume" => filtered.OrderByDescending(w => w.Volume),
            "winrate" => filtered.OrderByDescending(w => w.WinRateLoaded ? w.WinRate : -1),
            _ => filtered.OrderByDescending(w => w.RealizedPnlUsd)
        };

        return sorted.ToList();
    }

    private async Task CrawlWalletsAsync()
    {
        if (_isCrawling.Value)
        {
            return;
        }

        _isCrawling.Value = true;
        _status.Value = "Fetching leaderboard data from Polymarket...";

        try
        {
            var crawled = await FetchLeaderboardWalletsAsync();

            if (crawled.Count == 0)
            {
                _status.Value = "Failed to fetch data from Polymarket API. Please try again later.";
                _isCrawling.Value = false;
                return;
            }

            foreach (var wallet in crawled)
            {
                wallet.TradingProfile = ClassifyTradingProfile(wallet);
                wallet.LastSignal = SuggestTradeSignal(wallet);
                wallet.LastSeenUtc = DateTime.UtcNow;
            }

            _wallets.Value = crawled;
            await SaveWalletStoreAsync();
            _status.Value = $"Loaded {_wallets.Value.Count} wallets from Polymarket leaderboard";

            _ = Task.Run(async () =>
            {
                await FetchWinRatesForTopWalletsAsync(20);
            });
        }
        catch (Exception ex)
        {
            _status.Value = $"Crawl failed: {ex.Message}";
        }
        finally
        {
            _isCrawling.Value = false;
        }
    }

    private async Task<List<WalletProfile>> FetchLeaderboardWalletsAsync()
    {
        var wallets = new List<WalletProfile>();
        var limit = 50;
        var targetCount = _maxWallets.Value;
        var timePeriod = _timePeriod.Value;

        for (var offset = 0; offset < targetCount; offset += limit)
        {
            var url = $"https://data-api.polymarket.com/v1/leaderboard?limit={limit}&offset={offset}&timePeriod={timePeriod}&orderBy=PNL";
            _status.Value = $"Fetching wallets {offset + 1}-{Math.Min(offset + limit, targetCount)}...";

            try
            {
                var entries = await _httpClient.GetFromJsonAsync<List<LeaderboardEntry>>(url);

                if (entries == null || entries.Count == 0)
                {
                    break;
                }

                foreach (var entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.ProxyWallet))
                    {
                        continue;
                    }

                    var roiPercent = entry.Vol > 0 ? (entry.Pnl / entry.Vol) * 100 : 0;

                    wallets.Add(new WalletProfile
                    {
                        Rank = int.TryParse(entry.Rank, out var rank) ? rank : wallets.Count + 1,
                        Address = entry.ProxyWallet,
                        UserName = entry.UserName ?? "",
                        ProfileImage = entry.ProfileImage ?? "",
                        RealizedPnlUsd = entry.Pnl,
                        Volume = entry.Vol,
                        RoiPercent = roiPercent,
                        WinRate = 0,
                        WinRateLoaded = false
                    });
                }

                if (entries.Count < limit)
                {
                    break;
                }

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _status.Value = $"Error fetching page at offset {offset}: {ex.Message}";
                break;
            }
        }

        return wallets;
    }

    private async Task FetchWinRatesForTopWalletsAsync(int count)
    {
        var walletsToFetch = _wallets.Value.Take(count).ToList();

        foreach (var wallet in walletsToFetch)
        {
            if (wallet.WinRateLoaded)
            {
                continue;
            }

            await FetchWalletDetailsAsync(wallet);
            await Task.Delay(200);
        }

        _status.Value = $"Loaded win rates for top {count} wallets";
    }

    private async Task FetchWalletDetailsAsync(WalletProfile wallet)
    {
        _isLoadingDetails.Value = true;

        try
        {
            var url = $"https://data-api.polymarket.com/activity?user={wallet.Address}&limit=100";
            var activities = await _httpClient.GetFromJsonAsync<List<ActivityEntry>>(url);

            if (activities == null || activities.Count == 0)
            {
                wallet.WinRateLoaded = true;
                wallet.WinRate = 0;
                wallet.MarketsTraded = 0;
                return;
            }

            var trades = activities.Where(a => a.Type == "TRADE").ToList();
            var redeems = activities.Where(a => a.Type == "REDEEM").ToList();

            wallet.MarketsTraded = trades.Select(t => t.Title).Distinct().Count();

            if (redeems.Count > 0)
            {
                var wins = redeems.Count(r => r.UsdcSize > 0);
                wallet.WinRate = (double)wins / redeems.Count * 100;
            }
            else if (trades.Count > 0)
            {
                wallet.WinRate = 50;
            }

            wallet.RecentTrades = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new TradeActivity
                {
                    TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(a.Timestamp).UtcDateTime,
                    Type = a.Type,
                    Title = a.Title,
                    Outcome = a.Outcome,
                    Side = a.Side,
                    UsdcSize = a.UsdcSize,
                    Price = a.Price
                })
                .ToList();

            wallet.WinRateLoaded = true;
        }
        catch
        {
            wallet.WinRateLoaded = true;
            wallet.WinRate = 0;
        }
        finally
        {
            _isLoadingDetails.Value = false;
        }
    }

    private static string ClassifyTradingProfile(WalletProfile wallet)
    {
        if (wallet.Volume > 1_000_000)
        {
            return "Whale";
        }

        if (wallet.RoiPercent > 50 && wallet.Volume < 100_000)
        {
            return "Sniper";
        }

        if (wallet.MarketsTraded > 50)
        {
            return "Active Trader";
        }

        if (wallet.RoiPercent > 20 && wallet.RoiPercent < 50)
        {
            return "Arbitrage";
        }

        return "Unclassified";
    }

    private static string SuggestTradeSignal(WalletProfile wallet)
    {
        if (wallet.TradingProfile == "Whale")
        {
            return "Large position trader - follow major directional bets with caution";
        }

        if (wallet.TradingProfile == "Sniper")
        {
            return "High-conviction trader - mirror early entries in high-volume markets";
        }

        if (wallet.TradingProfile == "Active Trader")
        {
            return "Diversified approach - good for learning market patterns";
        }

        if (wallet.TradingProfile == "Arbitrage")
        {
            return "Consistent returns - follow for steady gains in correlated markets";
        }

        return "Analyze recent trades before mirroring";
    }

    private async Task GenerateInsightsAsync()
    {
        var wallets = GetFilteredWallets();

        if (wallets.Count == 0)
        {
            _chatAnswer.Value = "No wallets match current filters";
            return;
        }

        var walletSummary = wallets.Take(20).Select(wallet =>
            $"#{wallet.Rank} {wallet.UserName ?? ShortenAddress(wallet.Address)} | pnl=${wallet.RealizedPnlUsd:N0} | vol=${wallet.Volume:N0} | winRate={wallet.WinRate:0}% | profile={wallet.TradingProfile}");

        var command = $"""
            You are a professional prediction market analyst.
            Provide concise recommendations for copy-trading and learning patterns.
            User question: {_chatQuestion.Value}

            Top wallet data:
            {string.Join("\n", walletSummary)}

            Return JSON with:
            - summary: short actionable summary (2-3 sentences)
            - suggestedTrades: 3-5 trade ideas based on top traders' patterns
            - mirrorCandidates: 3 wallet addresses worth monitoring
            """;

        try
        {
            var (result, _) = await Emerge.Run<InsightResponse>(
                LLMModel.Gpt41Mini,
                new KernelContext(),
                pass =>
                {
                    pass.Command = command;
                    pass.Temperature = 0.2f;
                }).FinalAsync();

            var builder = new StringBuilder();
            builder.AppendLine(result.Summary);

            if (result.MirrorCandidates.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Mirror candidates:");
                foreach (var wallet in result.MirrorCandidates)
                {
                    builder.AppendLine($"  {wallet}");
                }
            }

            if (result.SuggestedTrades.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Suggested trades:");
                foreach (var trade in result.SuggestedTrades)
                {
                    builder.AppendLine($"  {trade}");
                }
            }

            _chatAnswer.Value = builder.ToString();
        }
        catch (Exception ex)
        {
            _chatAnswer.Value = $"Insight generation failed: {ex.Message}";
        }
    }

    private AssetUri GetWalletStoreUri()
    {
        return new AssetUri(
            assetClass: AssetClass.CloudJson,
            path: WalletStorePath,
            spaceId: app.GlobalState.SpaceId,
            userId: app.GlobalState.PrimaryUserId,
            channelId: app.GlobalState.ChannelId);
    }

    private async Task LoadWalletStoreAsync()
    {
        try
        {
            var stored = await Asset.Instance.TryGetWithMetadataAsync<WalletStore>(GetWalletStoreUri());

            if (stored?.Content?.Wallets != null && stored.Content.Wallets.Count > 0)
            {
                _wallets.Value = stored.Content.Wallets;
                _status.Value = $"Loaded {_wallets.Value.Count} wallets from cache (last updated: {stored.Content.UpdatedAtUtc:g} UTC)";
            }
        }
        catch (Exception ex)
        {
            _status.Value = $"Failed to load cache: {ex.Message}";
        }
    }

    private async Task SaveWalletStoreAsync()
    {
        var store = new WalletStore
        {
            UpdatedAtUtc = DateTime.UtcNow,
            Wallets = _wallets.Value
        };

        await Asset.Instance.SetAsync(GetWalletStoreUri(), store);
    }

    private WalletProfile? GetSelectedWallet()
    {
        return _wallets.Value.FirstOrDefault(w => w.Address == _selectedWalletAddress.Value);
    }

    private static string GetPolymarketProfileUrl(string address)
    {
        return $"https://polymarket.com/profile/{address}";
    }

    private static string GetPolygonScanUrl(string address)
    {
        return $"https://polygonscan.com/address/{address}";
    }

    private static string NormalizeProfile(string profile)
    {
        return profile.Trim().ToLowerInvariant().Replace(" ", "-");
    }

    private static string ShortenAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length < 12)
        {
            return address;
        }

        return $"{address[..6]}...{address[^4..]}";
    }
}
