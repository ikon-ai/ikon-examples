using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ValidationApp.Data;

// A single row in the shared `entries` table. The same table is written and read by all three
// data-access styles demonstrated in the Database tab (EF Core, Dapper, raw ADO.NET); `Source`
// records which one inserted the row.
public class Entry
{
    public long Id { get; set; }
    public string Author { get; set; } = "";
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ValidationDbContext(DbContextOptions<ValidationDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries => Set<Entry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map to explicit lowercase names so the Dapper and raw-SQL paths can hit the exact same
        // table and columns that EF Core's migration creates.
        modelBuilder.Entity<Entry>(entry =>
        {
            entry.ToTable("entries");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).HasColumnName("id");
            entry.Property(e => e.Author).HasColumnName("author");
            entry.Property(e => e.Message).HasColumnName("message");
            entry.Property(e => e.Source).HasColumnName("source");
            entry.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}

// Lets `dotnet ef` (driven by `ikon app db ef-migrate-add`) build the context at design time. The
// ikon tool injects the provisioned connection string as IKON_DB.
public sealed class ValidationDbContextFactory : IDesignTimeDbContextFactory<ValidationDbContext>
{
    public ValidationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("IKON_DB")
            ?? throw new InvalidOperationException("IKON_DB is not set");

        return new ValidationDbContext(
            new DbContextOptionsBuilder<ValidationDbContext>().UseNpgsql(connectionString).Options);
    }
}
