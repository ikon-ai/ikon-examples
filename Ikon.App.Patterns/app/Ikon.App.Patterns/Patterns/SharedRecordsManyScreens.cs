using Dapper;

namespace Ikon.App.Patterns.Patterns;

// Pattern: shared-records-many-screens — see docs/patterns/shared-records-many-screens.md.
// The doc quotes the example region below. The demo starts from sample rows. A running app writes
// the same rows to its stock table at start, but only while the table is empty, so "Sold one" sells
// from the table and a sale someone made is never undone. The render smoke test never starts the app
// and draws the rows from memory.
internal sealed class SharedRecordsManyScreens(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "shared-records-many-screens";
    public string Title => "Shared records, many screens";
    public string Category => "Multi-user & games";
    public void RenderDemo(IView view) => Render((UIView)view);

    private static readonly StockItem[] SampleStock =
    [
        new StockItem("Harbour", "TUBE-28", "Inner tube 28\"", 6, 3),
        new StockItem("Harbour", "CHAIN-11", "Chain, 11-speed", 2, 2),
        new StockItem("Old Town", "TUBE-28", "Inner tube 28\"", 12, 3),
    ];

    public static SharedRecordsManyScreens WithSampleStock(IApp<SessionIdentity, ClientParameters> app)
    {
        var demo = new SharedRecordsManyScreens(app);
        demo._stock.AddRange(SampleStock);
        app.OnStarting(demo.SeedOnceAsync);
        return demo;
    }

    // Every visitor shares the gallery's database, so nothing here overwrites a row already there.
    // ON CONFLICT keeps two instances starting at once from failing.
    private async Task SeedOnceAsync()
    {
        if (SnapshotCapture.IsCaptureProcess)
        {
            return;
        }

        try
        {
            await LoadStockAsync();

            await using (var connection = await app.DatabaseAsync())
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync("""
                    INSERT INTO stock (shop, sku, name, quantity, low_at)
                    SELECT * FROM unnest(@shops, @skus, @names, @quantities, @lowAts)
                    WHERE NOT EXISTS (SELECT 1 FROM stock)
                    ON CONFLICT (shop, sku) DO NOTHING
                    """,
                    new
                    {
                        shops = SampleStock.Select(s => s.Shop).ToArray(),
                        skus = SampleStock.Select(s => s.Sku).ToArray(),
                        names = SampleStock.Select(s => s.Name).ToArray(),
                        quantities = SampleStock.Select(s => s.Quantity).ToArray(),
                        lowAts = SampleStock.Select(s => s.LowAt).ToArray(),
                    });
            }

            await LoadStockAsync();
        }
        catch (Exception ex)
        {
            // The list still draws from the sample rows; only "Sold one" needs the table. Rows a
            // load already read are the real ones and stay.
            if (_stock.Count == 0)
            {
                _stock.ReplaceAll(SampleStock);
            }

            Log.Instance.Warning($"Seeding the shared-records-many-screens demo's stock table failed, so its sample rows show and \"Sold one\" fails: {ex.Message}");
        }
    }

    #region example:pattern-shared-records-many-screens
    private sealed record StockItem(string Shop, string Sku, string Name, int Quantity, int LowAt);

    private static readonly string[] Shops = ["Harbour", "Old Town", "Station"];

    // With SessionIdentity() declared without fields, every screen in every shop joins this one
    // instance, and all of them show the stock in this one list. The database holds the records. The
    // list is a copy of them in memory, loaded once and changed only after a row has been written.
    private readonly ReactiveList<StockItem> _stock = new();

    // Which shop a screen shows is that screen's own choice, kept per client.
    private readonly ClientReactive<string> _shop = new(Shops[0]);

    /// <summary>Call from app.OnStarting.</summary>
    private async Task LoadStockAsync()
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS stock (
                shop text, sku text, name text NOT NULL, quantity int NOT NULL, low_at int NOT NULL,
                PRIMARY KEY (shop, sku))
            """);
        _stock.ReplaceAll(await connection.QueryAsync<StockItem>(
            "SELECT shop AS Shop, sku AS Sku, name AS Name, quantity AS Quantity, low_at AS LowAt FROM stock ORDER BY name"));
    }

    private async Task SellOneAsync(StockItem item)
    {
        // The UPDATE only applies while quantity > 0, so two screens selling the last one at once leave it at 0, not -1.
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        var left = await connection.ExecuteScalarAsync<int?>(
            "UPDATE stock SET quantity = quantity - 1 WHERE shop = @Shop AND sku = @Sku AND quantity > 0 RETURNING quantity",
            new { item.Shop, item.Sku });

        if (left is not { } quantity)
        {
            return;
        }

        _stock.ReplaceWhere(row => row.Shop == item.Shop && row.Sku == item.Sku, row => row with { Quantity = quantity });

        if (quantity == item.LowAt)
        {
            await AlertShopAsync(item.Shop, $"{item.Name} is down to {quantity}");
        }
    }

    // The alert goes to every screen showing that shop right now, found from each screen's own choice,
    // so there is no list of screens to keep up to date as they join and leave.
    private async Task AlertShopAsync(string shop, string message)
    {
        foreach (var clientId in app.Clients.Ids.Where(id => _shop.ValueFor(id) == shop).ToList())
        {
            await app.Notifications.SendToSessionAsync(clientId, new NotificationContent("Low stock", message, Tag: $"low-stock-{shop}"));
        }
    }

    private void Render(UIView view)
    {
        view.Column(["gap-3 max-w-md"], content: view =>
        {
            view.Select(label: "This screen's shop", options: Shops.Select(shop => new SelectOption(shop, shop)).ToList(),
                value: _shop.Value, onValueChange: shop =>
                {
                    _shop.Value = shop;
                    return Task.CompletedTask;
                });

            var items = _stock.Where(row => row.Shop == _shop.Value).ToList();

            if (items.Count == 0)
            {
                view.EmptyState([EmptyState.RootSm], title: $"No stock recorded for {_shop.Value} yet", description: "Rows you record for this shop show up here.");
            }

            foreach (var item in items)
            {
                view.Row(["items-center justify-between gap-3"], content: row =>
                {
                    row.Text([item.Quantity <= item.LowAt ? "text-destructive" : "text-foreground"], $"{item.Name}: {item.Quantity}");
                    row.Button([Button.OutlineMd], text: $"Sold one {item.Name}", disabled: item.Quantity == 0,
                        onClick: async () => await SellOneAsync(item));
                });
            }
        });
    }
    #endregion
}
