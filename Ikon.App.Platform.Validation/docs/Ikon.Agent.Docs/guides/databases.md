# Databases

## Databases

Managed PostgreSQL database connections.

**Every app already has one.** `app.DatabaseAsync()` with no name gives you the built-in `app`
database, created the first time you ask for it. Nothing needs configuring, and an app that never
asks is never given one.

Create a second only when the app wants one under a name of its own — `await app.DatabaseAsync("mydb")`:

```
ikon app db create --name mydb --yes
```

Databases are **not** declared in `ikon-config.toml`. They belong to the space, so they survive
deploys and are never removed by editing a file; `ikon app db list` shows what the space holds and
`ikon app db delete --name mydb --yes` gives one up. Nothing is per-environment: each target has its
own space, so create the database against the target you mean (`--target staging`).

Code that references a database the space does not hold leaves the app broken at runtime. The Critic
should reject any pass where the C# uses a database `"X"` — EF Core's
`app.Databases.First(d => d.Name == "X")`, `await app.DatabaseAsync("X")`, or the older
`AppDatabaseConnection.Create(app, "X")` — without an `ikon app db create --name X` to match.

### Data access — prefer EF Core

The platform provisions the database and exposes its connection string in `app.Databases`, a list of `DatabaseConnectionInfo` — `Name` (the `--name` the database was created under), `Type` (`"postgres"` is the only engine provisioned today, and `AppDatabaseConnection.Create` throws `NotSupportedException` for anything else) and a ready-to-use ADO.NET `ConnectionString` that carries credentials, so never log it or send it to a client. An app never constructs one. **Defining and evolving the schema is the app's job.** For anything past a single flat table — related entities, or a schema that will change over time — use **EF Core**: typed LINQ instead of hand-written SQL, and versioned migrations applied automatically at startup. Add the packages (EF Core 10 + the Npgsql provider):

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
```

Call it at startup, so every pending migration is applied before the app serves traffic:

```csharp
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

**Migrations workflow.** Add a design-time factory once so `dotnet ef` can build the context — the `ikon app db migrate *` commands drive `dotnet ef` and inject the real connection string as `IKON_DB`:

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

After each model change, run `ikon app db migrate add <Name>` and commit the generated migration — startup `MigrateAsync()` applies it on the next deploy. `ikon app db migrate apply` applies it now, `ikon app db migrate list` shows status, `ikon app db migrate delete` drops the last unapplied one, and `ikon app db reset` drops all data.

### Raw SQL (lightweight alternative)

For a table or two with no schema churn, skip EF Core. `await app.DatabaseAsync("mydb")` returns a standard ADO.NET `DbConnection`, unopened — open and dispose it **per operation** (never hold one as a field); create the schema with idempotent DDL at startup:

```csharp
await using var connection = await app.DatabaseAsync("mydb");
await connection.OpenAsync();
await using var cmd = connection.CreateCommand();
cmd.CommandText = "CREATE TABLE IF NOT EXISTS users (id BIGSERIAL PRIMARY KEY, name TEXT NOT NULL);";
await cmd.ExecuteNonQueryAsync();
```

Evolve additively — append more `... IF NOT EXISTS` statements (and `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`) so the startup DDL stays safe to re-run on every boot.
