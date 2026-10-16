using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ikon.App.Patterns.Examples;

file sealed class DapperExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:dapper-queries
    private sealed record Order(long Id, string Customer, decimal Total, bool Shipped);

    // Run once from app.OnStarting.
    private async Task CreateOrdersTableAsync()
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS orders (
                id bigserial PRIMARY KEY,
                customer text NOT NULL,
                total numeric NOT NULL,
                shipped boolean NOT NULL DEFAULT false)
            """);
    }

    private async Task<IReadOnlyList<Order>> OrdersForAsync(string customer)
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        // Columns map to the record's constructor by name, in order, and their types must match it.
        var rows = await connection.QueryAsync<Order>(
            "SELECT id, customer, total, shipped FROM orders WHERE customer = @customer ORDER BY id",
            new { customer });
        return rows.AsList();
    }

    private async Task MarkShippedAsync(IEnumerable<long> orderIds)
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        // A list goes in as one array parameter: Npgsql rejects `IN @ids` with 42601.
        await connection.ExecuteAsync(
            "UPDATE orders SET shipped = true WHERE id = ANY(@ids)",
            new { ids = orderIds.ToArray() });
    }
    #endregion
}

file sealed class StarterRowExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private void WireStarterRows()
    {
        #region example:starter-rows
        // Rows every copy of the app starts with, production included (app.Seed never runs there)
        app.OnStarting(async () =>
        {
            await using var connection = await app.DatabaseAsync();
            await connection.OpenAsync();
            await connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS categories (
                    id serial PRIMARY KEY,
                    name text NOT NULL UNIQUE)
                """);

            // Only into an empty table, so a category an admin deleted stays deleted. ON CONFLICT keeps
            // two instances that start at once (shards, a deploy beside a running copy) from failing.
            await connection.ExecuteAsync(
                """
                INSERT INTO categories (name)
                SELECT unnest(@names) WHERE NOT EXISTS (SELECT 1 FROM categories)
                ON CONFLICT (name) DO NOTHING
                """,
                new { names = new[] { "Repairs", "Cleaning", "Supplies" } });
        });
        #endregion
    }
}

file sealed class PgVectorExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:pgvector-search
    private sealed record PassageHit(long Id, string DocumentId, int Page, string Text, double Similarity);

    // Run once from app.OnStarting. 1536 is the length of a default EmbeddingGenerator vector.
    private async Task CreatePassagesTableAsync()
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS vector");
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS passages (
                id bigserial PRIMARY KEY,
                document_id text NOT NULL,
                page int NOT NULL,
                text text NOT NULL,
                embedding vector(1536) NOT NULL)
            """);
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS passages_embedding_idx ON passages USING hnsw (embedding vector_cosine_ops)");
    }

    private async Task AddPassagesAsync(string documentId, IReadOnlyList<(int Page, string Text)> passages)
    {
        var embeddings = await EmbeddingGenerator.EmbedAsync(passages.Select(p => p.Text).ToList(), type: EmbeddingType.Document);
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "INSERT INTO passages (document_id, page, text, embedding) VALUES (@documentId, @page, @text, CAST(@embedding AS vector))",
            passages.Select((p, i) => new { documentId, page = p.Page, text = p.Text, embedding = ToVector(embeddings[i]) }));
    }

    private async Task<IReadOnlyList<PassageHit>> NearestPassagesAsync(string question, int limit = 40)
    {
        var query = (await EmbeddingGenerator.EmbedAsync([question], type: EmbeddingType.Query))[0];
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        // <=> is the cosine distance the index is built on, so ordering by it uses the index instead of reading every row.
        var rows = await connection.QueryAsync<PassageHit>("""
            SELECT id, document_id AS DocumentId, page, text, 1 - (embedding <=> CAST(@query AS vector)) AS Similarity
            FROM passages ORDER BY embedding <=> CAST(@query AS vector) LIMIT @limit
            """, new { query = ToVector(query), limit });
        return rows.AsList();
    }

    // Without the Pgvector package Npgsql has no vector type, so a vector goes in as its text form, [0.1,0.2,...].
    private static string ToVector(float[] vector) =>
        "[" + string.Join(",", vector.Select(f => f.ToString("R", CultureInfo.InvariantCulture))) + "]";
    #endregion
}

file sealed class VectorArrayExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:vector-array-column
    // A class with settable properties, not a positional record: Dapper reads a real[] column as
    // System.Array, which no record constructor parameter matches.
    private sealed class StoredNote
    {
        public long Id { get; set; }
        public string Text { get; set; } = "";
        public float[] Embedding { get; set; } = [];
    }

    // Run once from app.OnStarting.
    private async Task CreateNotesTableAsync()
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "CREATE TABLE IF NOT EXISTS notes (id bigserial PRIMARY KEY, text text NOT NULL, embedding real[] NOT NULL)");
    }

    private async Task AddNoteAsync(string text)
    {
        var embedding = (await EmbeddingGenerator.EmbedAsync([text], type: EmbeddingType.Document))[0];
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        // A float[] parameter is a real[] value, so it needs no conversion.
        await connection.ExecuteAsync("INSERT INTO notes (text, embedding) VALUES (@text, @embedding)", new { text, embedding });
    }

    private async Task<IReadOnlyList<StoredNote>> MostSimilarNotesAsync(string question, int limit = 5)
    {
        var query = (await EmbeddingGenerator.EmbedAsync([question], type: EmbeddingType.Query))[0];
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        var notes = await connection.QueryAsync<StoredNote>("SELECT id, text, embedding FROM notes");
        return notes.OrderByDescending(n => VectorMath.CalculateCosineSimilarity(n.Embedding, query)).Take(limit).ToList();
    }
    #endregion
}

file sealed class BusinessPeriodExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:business-period-in-a-zone
    private const string OrganisationZone = "Europe/Helsinki";

    // readings.read_at is a timestamptz.
    private async Task<decimal> ThisMonthsKwhAsync()
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        // now() AT TIME ZONE gives the local time in that zone. The outer AT TIME ZONE turns the local
        // start of the month back into a timestamptz, so the bounds follow daylight saving time.
        return await connection.ExecuteScalarAsync<decimal>("""
            SELECT COALESCE(sum(kwh), 0) FROM readings
            WHERE read_at >= date_trunc('month', now() AT TIME ZONE @zone) AT TIME ZONE @zone
              AND read_at < (date_trunc('month', now() AT TIME ZONE @zone) + interval '1 month') AT TIME ZONE @zone
            """, new { zone = OrganisationZone });
    }

    private static (DateTime FromUtc, DateTime ToUtc) ThisMonthInUtc()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OrganisationZone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var monthStart = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(monthStart, zone), TimeZoneInfo.ConvertTimeToUtc(monthStart.AddMonths(1), zone));
    }
    #endregion
}

file sealed class EfCoreExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:ef-core-setup
    public class Note { public long Id { get; set; } public string Text { get; set; } = ""; public DateTime CreatedAt { get; set; } }

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();
    }

    private AppDbContext CreateDbContext()
    {
        var info = app.Databases.First(d => d.Name == "mydb");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(info.ConnectionString)
            // A hot reload's rebuilt model reads as changed, so the startup migration would throw without this
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new AppDbContext(options);
    }
    #endregion

    #region example:ef-core-default-database
    // Over the built-in database, which is provisioned the first time the app asks for it. One connection per context:
    private async Task<AppDbContext> CreateDefaultDbContextAsync()
    {
        var connection = await app.DatabaseAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection, contextOwnsConnection: true)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new AppDbContext(options);
    }
    #endregion

    #region example:ef-core-design-time-factory
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("IKON_DB") ?? throw new InvalidOperationException("IKON_DB is not set");
            return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options);
        }
    }
    #endregion

    public void MigrateAtStartup()
    {
        #region example:ef-core-migrate
        // at startup — applies every pending migration before the app serves traffic:
        app.OnStarting(async () => { await using var db = CreateDbContext(); await db.Database.MigrateAsync(); });
        #endregion
    }

    public async Task<int> QueryAsync(string text)
    {
        #region example:ef-core-query
        await using var db = CreateDbContext();
        db.Notes.Add(new Note { Text = text, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var recent = await db.Notes.OrderByDescending(n => n.CreatedAt).Take(20).ToListAsync();
        #endregion

        return recent.Count;
    }
}

internal sealed partial class AgentGuideExamples
{

    private async Task DocRawSqlAsync()
    {
        #region example:raw-sql
        await using var connection = await app.DatabaseAsync("mydb");
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS users (id BIGSERIAL PRIMARY KEY, name TEXT NOT NULL);";
        await cmd.ExecuteNonQueryAsync();
        #endregion
    }

    private async Task DocRawSqlParametersAsync(string runId, long? finishedMs)
    {
        #region example:raw-sql-parameters
        await using var connection = await app.DatabaseAsync("mydb");
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        // A null goes in as DBNull, which carries no type. Where nothing around a parameter gives it one
        // (`@x IS NULL`, `COALESCE(@x, 0)`), Postgres fails with 42P08, so the SQL casts it.
        cmd.CommandText = """
            UPDATE runs SET finished_ms = CAST(@finishedMs AS bigint),
                finished_at = CASE WHEN CAST(@finishedMs AS bigint) IS NULL THEN NULL ELSE now() END
            WHERE id = @id
            """;

        foreach (var (name, value) in new (string, object?)[] { ("id", runId), ("finishedMs", finishedMs) })
        {
            var parameter = cmd.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(parameter);
        }

        await cmd.ExecuteNonQueryAsync();
        #endregion
    }

    private void DocAppSeed()
    {
        #region example:app-seed
        app.Seed(async () =>
        {
            await using var connection = await app.DatabaseAsync();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO orders (customer, total) VALUES ('Aino Virtanen', 42.50), ('Mikko Laine', 18.00);";
            await command.ExecuteNonQueryAsync();
        });
        #endregion
    }
}
