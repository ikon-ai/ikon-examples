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

### Schema management

The platform provisions the database and hands you a connection — defining and maintaining the schema is the app's responsibility, and the data-access library is your choice (raw `DbConnection`, Dapper, EF Core, …).

The recommended default is **idempotent DDL run once at app startup**: keep your `CREATE TABLE IF NOT EXISTS` (and `CREATE INDEX IF NOT EXISTS`) statements in one place and execute them when the app starts.

```csharp
await using var connection = AppDatabaseConnection.Create(app, "mydb");
await connection.OpenAsync();

await using var cmd = connection.CreateCommand();
cmd.CommandText = """
    CREATE TABLE IF NOT EXISTS users (
        id   BIGSERIAL PRIMARY KEY,
        name TEXT NOT NULL
    );
    """;
await cmd.ExecuteNonQueryAsync();
```

Evolve the schema **additively** — append more `IF NOT EXISTS` statements (and `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`) so the startup DDL stays safe to re-run on every boot.

EF Core is fully supported as an opt-in for apps that want a `DbContext` and migrations, but it is not required. The `ikon app db ef-migrate-*` CLI commands are EF-Core-only; `ikon app db reset` works for any app — it drops all data, and your app recreates the schema on next start.
