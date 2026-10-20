using System.Security.Cryptography;
using Dapper;

namespace Ikon.App.Patterns.Patterns;

// Pattern: device-ingest — see docs/patterns/device-ingest.md.
// The endpoint is served only from an [App] class, so here it compiles and the demo draws the
// dashboard; its button stands in for a device and writes the live view only, with no database.
internal sealed class DeviceIngest(IAppBase app) : IPatternDemo
{
    public string Slug => "device-ingest";
    public string Title => "Readings posted by devices";
    public string Category => "Device & sensors";

    public void RenderDemo(IView view)
    {
        RenderSensors((UIView)view);
        view.Row(["gap-2 mt-3 flex-wrap"], content: row =>
        {
            row.Button([Button.OutlineMd], text: "Simulate a reading", onClick: async () =>
            {
                var sensorId = $"greenhouse-{Random.Shared.Next(1, 4)}";
                _latest[sensorId] = new LatestReading(sensorId, Math.Round(18 + Random.Shared.NextDouble() * 8, 1),
                    Math.Round(40 + Random.Shared.NextDouble() * 30), Math.Round(20 + Random.Shared.NextDouble() * 40), DateTime.UtcNow);
            });

            // IssueKeyAsync needs the app's database, so this button makes and shows a key the same
            // way without storing its hash.
            row.Button([Button.OutlineMd], text: "Issue a key for greenhouse-1", onClick: async () =>
                _issuedKey.Value = new IssuedKey("greenhouse-1", NewDeviceKey()));
        });
        RenderIssuedKey((UIView)view);
    }

    #region example:pattern-device-ingest
    // What a device posts as JSON. Public, because a public handler cannot take a less accessible type (CS0051)
    public sealed record SensorReading(string SensorId, double? Temperature, double? Humidity, double? SoilMoisture);

    private sealed record LatestReading(string SensorId, double? Temperature, double? Humidity, double? SoilMoisture, DateTime ReceivedAt);

    // The last reading of each sensor, which every open dashboard draws
    private readonly ReactiveDictionary<string, LatestReading> _latest = new();

    // POST https://{app}.ikonai.app/api/readings with an X-Device-Key header. Public, because a
    // device cannot carry a signed grant: the shared key is the authorization.
    [HttpPost("/readings", Auth = EndpointAuth.Public)]
    public async Task<HttpResult> PostReading(SensorReading reading, Ikon.App.HttpRequest request)
    {
        // TryGetValue, not the indexer: a missing header must be a 401, not a 500
        if (!app.Secrets.TryGet("DEVICE_KEY", out var deviceKey)
            || !request.Headers.TryGetValue("X-Device-Key", out var sentKey)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sentKey), Encoding.UTF8.GetBytes(deviceKey)))
        {
            return HttpResult.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(reading.SensorId) || reading.SensorId.Length > 64)
        {
            return HttpResult.BadRequest("sensorId is required, at most 64 characters");
        }

        // The server's clock, not the device's: a cheap sensor's clock drifts or starts at 1970
        var receivedAt = DateTime.UtcNow;

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            INSERT INTO readings (sensor_id, temperature, humidity, soil_moisture, received_at)
            VALUES (@SensorId, @Temperature, @Humidity, @SoilMoisture, @receivedAt)
            """, new { reading.SensorId, reading.Temperature, reading.Humidity, reading.SoilMoisture, receivedAt });

        // Stored first, so a dashboard never shows a reading the database lost
        _latest[reading.SensorId] = new LatestReading(reading.SensorId, reading.Temperature, reading.Humidity, reading.SoilMoisture, receivedAt);
        return HttpResult.Ok();
    }

    // Run once from app.OnStarting: the table, and each sensor's last reading for the dashboard
    private async Task LoadLatestReadingsAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS readings (
                id bigserial PRIMARY KEY,
                sensor_id text NOT NULL,
                temperature double precision,
                humidity double precision,
                soil_moisture double precision,
                received_at timestamptz NOT NULL);
            CREATE INDEX IF NOT EXISTS readings_by_sensor ON readings (sensor_id, received_at DESC);
            """);

        var rows = await db.QueryAsync<LatestReading>("""
            SELECT DISTINCT ON (sensor_id) sensor_id AS SensorId, temperature AS Temperature, humidity AS Humidity,
                soil_moisture AS SoilMoisture, received_at AS ReceivedAt
            FROM readings ORDER BY sensor_id, received_at DESC
            """);

        _latest.ReplaceAll(rows.Select(row => KeyValuePair.Create(row.SensorId, row)));
    }

    private void RenderSensors(UIView view)
    {
        view.Column([Card.Default, "gap-2 p-4 w-full max-w-2xl"], content: col =>
        {
            col.Text([Text.H3], "Sensors");

            if (_latest.Count == 0)
            {
                col.EmptyState([EmptyState.RootSm], title: "No readings yet", description: "Devices post to /api/readings.");
                return;
            }

            foreach (var reading in _latest.Values.OrderBy(r => r.SensorId, StringComparer.Ordinal))
            {
                col.Row(["items-center justify-between gap-3 flex-wrap"], key: reading.SensorId, content: row =>
                {
                    row.Text([Text.Body, "font-medium"], reading.SensorId);
                    var values = new[] { (reading.Temperature, "°C"), (reading.Humidity, "% humidity"), (reading.SoilMoisture, "% soil") }
                        .Where(value => value.Item1 is not null)
                        .Select(value => $"{value.Item1:0.#} {value.Item2}");
                    row.Text([Text.Caption], string.Join(" · ", values));
                    row.Text([Text.Caption], $"{reading.ReceivedAt:HH:mm} UTC");
                });
            }
        });
    }
    #endregion

    #region example:pattern-device-ingest-device-keys
    private sealed record IssuedKey(string DeviceId, string Key);

    // The key just issued, shown once and only to the person who issued it
    private readonly ClientReactive<IssuedKey?> _issuedKey = new(null);

    // 32 random bytes: too long to guess, so a plain SHA-256 of it is safe to store. A password a
    // person chose would need a slow hash instead.
    private static string NewDeviceKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string HashKey(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    // Run once from app.OnStarting, where the readings table is created
    private async Task CreateDevicesTableAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS devices (
                device_id text PRIMARY KEY,
                key_hash text NOT NULL UNIQUE,
                issued_at timestamptz NOT NULL);
            """);
    }

    // Issues a device's first key, or rotates it: the new hash replaces the old one, so the old key
    // stops working at once. Call it from an admin-only button.
    private async Task IssueKeyAsync(string deviceId)
    {
        var key = NewDeviceKey();
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            INSERT INTO devices (device_id, key_hash, issued_at) VALUES (@deviceId, @keyHash, now())
            ON CONFLICT (device_id) DO UPDATE SET key_hash = EXCLUDED.key_hash, issued_at = EXCLUDED.issued_at
            """, new { deviceId, keyHash = HashKey(key) });
        _issuedKey.Value = new IssuedKey(deviceId, key);
    }

    // The device a request's key belongs to, or null. It replaces the DEVICE_KEY check.
    private async Task<string?> DeviceForKeyAsync(Ikon.App.HttpRequest request)
    {
        if (!request.Headers.TryGetValue("X-Device-Key", out var sentKey) || sentKey.Length is 0 or > 128)
        {
            return null;
        }

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        var deviceIds = await db.QueryAsync<string>("SELECT device_id FROM devices WHERE key_hash = @keyHash",
            new { keyHash = HashKey(sentKey) });
        return deviceIds.FirstOrDefault();
    }

    private void RenderIssuedKey(UIView view)
    {
        if (_issuedKey.Value is not { } issued)
        {
            return;
        }

        view.Column([Card.Default, "gap-2 p-4 w-full max-w-2xl mt-3"], content: col =>
        {
            col.Text([Text.BodyStrong], $"Key for {issued.DeviceId}");
            col.Text([Text.Caption], "Copy it into the device now. It is not shown again. If it is lost, issue a new key.");
            col.Text([Text.Code, "break-all"], issued.Key);
            col.Row(["gap-2"], content: row =>
            {
                row.ActionButton([Button.OutlineSm], action: ActionKind.CopyToClipboard, text: "Copy",
                    options: new CopyToClipboardActionOptions { Text = issued.Key });
                row.Button([Button.GhostSm], text: "Done", onClick: async () => _issuedKey.Value = null);
            });
        });
    }
    #endregion
}
