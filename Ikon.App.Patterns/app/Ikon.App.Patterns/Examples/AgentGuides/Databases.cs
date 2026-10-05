using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ikon.App.Patterns.Examples;

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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(info.ConnectionString).Options;
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
