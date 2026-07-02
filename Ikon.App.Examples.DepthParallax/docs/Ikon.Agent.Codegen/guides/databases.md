# Databases

## Databases

Managed PostgreSQL database connections.

**One command sets up both the toml entry AND the platform provisioning** if your code uses a database named `"mydb"` (whether via EF Core's `app.Databases` or `AppDatabaseConnection.Create(app, "mydb")`):

```
ikon app config --add-database mydb:postgres
```

That adds `"mydb:postgres"` to `Databases` in `ikon-config.development.toml` (preserving the rest of the file — `[Target]`, `[Auth]`, `[Activation]`), then triggers normal provisioning so the database comes online. Idempotent — repeat the flag for additional databases or re-run safely.

For staging/production envs, repeat with `--target staging` / `--target production` — there is no inheritance across env-specific tomls.

Manual two-step path (only if you need to inspect/edit the toml first): `read` the env-specific toml → `edit` the `Databases = []` line to add `"mydb:postgres"` → `ikon app config`. NEVER `write` the toml end-to-end — that destroys the `[Target]` section and `ikon app config` will revert it.

Code that references a database but skips this setup leaves the app broken at runtime. The Critic should reject any pass where the C# uses a database `"X"` — EF Core's `app.Databases.First(d => d.Name == "X")` or `AppDatabaseConnection.Create(app, "X")` — but `ikon-config.development.toml` doesn't declare `"X:postgres"` in `Databases`.

### Data access — prefer EF Core

The platform provisions the database and exposes its connection string in `app.Databases`; **defining and evolving the schema is the app's job.** For anything past a single flat table — related entities, or a schema that will change over time — use **EF Core**: typed LINQ instead of hand-written SQL, and versioned migrations applied automatically at startup. Add the packages (EF Core 10 + the Npgsql provider):

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.2" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

EF Core isn't in `GlobalUsings`, so add `using Microsoft.EntityFrameworkCore;` (and `using Microsoft.EntityFrameworkCore.Design;` for the factory) to files that use it.

Define a `DbContext` with your entities, build it **per operation** (never a long-lived field) from the platform connection string, and migrate once at startup:

```csharp
public class Note { public long Id { get; set; } public string Text { get; set; } = ""; public DateTime CreatedAt { get; set; } }

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

private AppDbContext CreateDbContext()
{
    var info = app.Databases.First(d => d.Name == "mydb");
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(info.ConnectionString).Options;
    return new AppDbContext(options);
}

// at startup — applies every pending migration before the app serves traffic:
app.OnStarting(async () => { await using var db = CreateDbContext(); await db.Database.MigrateAsync(); });
```

Read and write with LINQ:

```csharp
await using var db = CreateDbContext();
db.Notes.Add(new Note { Text = text, CreatedAt = DateTime.UtcNow });
await db.SaveChangesAsync();
var recent = await db.Notes.OrderByDescending(n => n.CreatedAt).Take(20).ToListAsync();
```

**Migrations workflow.** Add a design-time factory once so `dotnet ef` can build the context — the `ikon app db ef-migrate-*` commands drive `dotnet ef` and inject the real connection string as `IKON_DB`:

```csharp
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("IKON_DB") ?? throw new InvalidOperationException("IKON_DB is not set");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options);
    }
}
```

After each model change, run `ikon app db ef-migrate-add <Name>` and commit the generated migration — startup `MigrateAsync()` applies it on the next deploy. `ikon app db ef-migrate-apply` applies it now, `ikon app db ef-migrate-list` shows status, `ikon app db ef-migrate-remove` drops the last unapplied one, and `ikon app db reset` drops all data.

### Raw SQL (lightweight alternative)

For a table or two with no schema churn, skip EF Core. `AppDatabaseConnection.Create(app, "mydb")` returns a standard ADO.NET `DbConnection` — open and dispose it **per operation** (never hold one as a field); create the schema with idempotent DDL at startup:

```csharp
await using var connection = AppDatabaseConnection.Create(app, "mydb");
await connection.OpenAsync();
await using var cmd = connection.CreateCommand();
cmd.CommandText = "CREATE TABLE IF NOT EXISTS users (id BIGSERIAL PRIMARY KEY, name TEXT NOT NULL);";
await cmd.ExecuteNonQueryAsync();
```

Evolve additively — append more `... IF NOT EXISTS` statements (and `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`) so the startup DDL stays safe to re-run on every boot.
