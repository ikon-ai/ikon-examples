# Databases

## Databases

Managed PostgreSQL database connections.

Configure in `ikon-config.toml`:

```toml
Databases = ["mydb:postgres"]
```

### Usage

`AppDatabaseConnection.Create` returns a standard ADO.NET `DbConnection`. The caller is responsible for opening and disposing it.

```csharp
await using var connection = AppDatabaseConnection.Create(app, "mydb");
await connection.OpenAsync();

await using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM users";
var count = await cmd.ExecuteScalarAsync();
```
