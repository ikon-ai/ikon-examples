# Endpoints & Databases

## Endpoints

Custom HTTP/HTTPS/WebSocket endpoints for webhooks, external APIs, and integrations.

Configure in `ikon-config.toml`:

```toml
Endpoints = ["api:https", "webhooks:http"]
```

### Creating and Starting an Endpoint

```csharp
var endpoint = new AppEndpointHost(host, "api");

endpoint.MapGet("/health", async ctx =>
{
    ctx.Response.ContentType = "text/plain";
    await ctx.Response.WriteAsync("OK");
});

endpoint.MapPost("/data", async ctx =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsync($"{{\"received\": true}}");
});

endpoint.MapWebSocket("/ws", async (ctx, webSocket) =>
{
    var buffer = new byte[4096];
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close) { break; }
        await webSocket.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, true, CancellationToken.None);
    }
});

await endpoint.StartAsync();
```

### Properties

- `endpoint.PublicUrl` - The public URL assigned by the platform (use this to share with external services)
- `endpoint.LocalPort` - The local port the endpoint is listening on

### Cleanup

Dispose endpoints when the app stops:

```csharp
app.StoppingAsync += async _ =>
{
    await endpoint.DisposeAsync();
};
```

## Databases

Managed PostgreSQL database connections.

Configure in `ikon-config.toml`:

```toml
Databases = ["mydb:postgres"]
```

### Usage

`AppDatabaseConnection.Create` returns a standard ADO.NET `DbConnection`. The caller is responsible for opening and disposing it.

```csharp
await using var connection = AppDatabaseConnection.Create(host, "mydb");
await connection.OpenAsync();

await using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM users";
var count = await cmd.ExecuteScalarAsync();
```
