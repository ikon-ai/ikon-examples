using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ValidationApp.Data;

public partial class Validation
{
    // All three access styles share one Postgres table. The cap keeps the demo DB from being
    // spammed full.
    private const int MaxRows = 100;
    private const string DatabaseName = "validationdb";

    private readonly Reactive<string> _dbAuthor = new("");
    private readonly Reactive<string> _dbMessage = new("");
    private readonly Reactive<string> _dbReadVia = new("ef");
    private readonly ReactiveList<Entry> _dbEntries = new();
    private readonly Reactive<string> _dbStatus = new("");
    private readonly Reactive<bool> _dbBusy = new(false);

    private bool DatabaseAvailable => app.Databases.Any(d => d.Name == DatabaseName);

    private async Task InitDatabaseAsync()
    {
        if (!DatabaseAvailable)
        {
            Log.Instance.Warning("validationdb not provisioned — Database tab is read-only until 'ikon app config' allocates it");
            return;
        }

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
        await RefreshEntriesAsync();
    }

    // Wipe the demo table on shutdown so each run starts from a clean slate and the capped table
    // never carries stale rows between sessions.
    private async Task ClearDatabaseAsync()
    {
        if (!DatabaseAvailable)
        {
            return;
        }

        try
        {
            await using var connection = app.Database(DatabaseName);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM entries";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Database cleanup on shutdown failed: {ex}");
        }
    }

    // Fresh context per operation — never a long-lived field (concurrent handlers would clash).
    private ValidationDbContext CreateDbContext()
    {
        var dbInfo = app.Databases.First(d => d.Name == DatabaseName);

        var options = new DbContextOptionsBuilder<ValidationDbContext>()
            .UseNpgsql(dbInfo.ConnectionString)
            // MigrateAsync inside the running plugin trips this warning even when the schema is in
            // sync; ignore it so startup migration doesn't throw (same as the other DB-backed apps).
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ValidationDbContext(options);
    }

    private void RenderDatabaseSection(UIView view)
    {
        if (RenderSectionLocked(view, "Database"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Database");
                view.Text([Text.Caption, "mb-2"], "Managed PostgreSQL via the 'validationdb' database declared in ikon-config. The same 'entries' table is exercised through three .NET data-access styles — EF Core, Dapper, and raw ADO.NET/Npgsql — so this tab doubles as a reference for app developers.");
                view.Text([Text.Caption], "EF Core owns the schema (created by its migration, applied at startup); Dapper and raw SQL read/write the same table.");
            });

            if (!DatabaseAvailable)
            {
                view.Box([Card.Default, "p-6"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], "Database not provisioned");
                    view.Text([Text.Caption], "'validationdb' is declared in ikon-config but not yet provisioned for this space. Run 'ikon app config' (logged into the backend) to allocate it, then restart the app.");
                });

                return;
            }

            // Add form
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Add a row");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextField(bind: _dbAuthor, style: [Input.Default, "w-full"], label: "Author", placeholder: "Your name", props: TestId("db-author"));
                    view.TextField(bind: _dbMessage, style: [Input.Default, "w-full"], label: "Message", placeholder: "Say something", props: TestId("db-message"));

                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Button([Button.PrimaryMd],
                            text: "Add via EF Core",
                            disabled: _dbBusy.Value,
                            onClick: async () => await AddEntryAsync("ef"));

                        view.Button([Button.SecondaryMd],
                            text: "Add via Dapper",
                            disabled: _dbBusy.Value,
                            onClick: async () => await AddEntryAsync("dapper"));

                        view.Button([Button.OutlineMd],
                            text: "Add via Raw SQL",
                            disabled: _dbBusy.Value,
                            onClick: async () => await AddEntryAsync("raw"));
                    });

                    if (!string.IsNullOrEmpty(_dbStatus.Value))
                    {
                        view.Text([Text.Caption], _dbStatus.Value, props: TestId("db-status"));
                    }
                });
            });

            // Read controls + list
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Row(["flex items-center justify-between mb-4 flex-wrap gap-3"], content: view =>
                {
                    view.Text([Text.H3], $"Entries ({_dbEntries.Value.Count} / {MaxRows})", props: TestId("db-entries-header"));

                    view.Row([Layout.Row.Sm, "items-center flex-wrap"], content: view =>
                    {
                        view.Text([Text.Label], "Read via");
                        view.Select(
                            value: _dbReadVia.Value,
                            options:
                            [
                                new SelectOption("ef", "EF Core"),
                                new SelectOption("dapper", "Dapper"),
                                new SelectOption("raw", "Raw SQL")
                            ],
                            disabled: _dbBusy.Value,
                            onValueChange: async value =>
                            {
                                _dbReadVia.Value = value;
                                await RefreshEntriesAsync();
                            });

                        view.Button([Button.OutlineMd],
                            text: "Refresh",
                            disabled: _dbBusy.Value,
                            onClick: async () => await RefreshEntriesAsync());

                        view.Button([Button.ErrorMd],
                            text: "Delete all",
                            disabled: _dbBusy.Value,
                            onClick: DeleteAllEntriesAsync);
                    });
                });

                if (_dbEntries.Value.Count == 0)
                {
                    view.Text([Text.Caption], "No entries yet. Add one above.", props: TestId("db-empty"));
                }
                else
                {
                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        foreach (var entry in _dbEntries.Value)
                        {
                            view.Row(["items-center justify-between p-3 rounded-lg bg-background border border-secondary"],
                                key: entry.Id.ToString(),
                                content: view =>
                                {
                                    view.Column(["gap-0.5"], content: view =>
                                    {
                                        view.Text([Text.Body], $"{entry.Author}: {entry.Message}", props: TestId("db-entry"));
                                        view.Text([Text.Caption], entry.CreatedAt.ToString("u"));
                                    });

                                    view.Box(["px-2 py-1 rounded-md bg-card border border-secondary"],
                                        content: v => v.Text([Text.Caption], entry.Source));
                                });
                        }
                    });
                }
            });
        });
    }

    private async Task AddEntryAsync(string library)
    {
        if (_dbBusy.Value)
        {
            return;
        }

        var author = _dbAuthor.Value.Trim();
        var message = _dbMessage.Value.Trim();

        if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(message))
        {
            _dbStatus.Value = "Author and message are both required";
            return;
        }

        _dbBusy.Value = true;

        try
        {
            switch (library)
            {
                case "ef":
                    await AddViaEfAsync(author, message);
                    break;
                case "dapper":
                    await AddViaDapperAsync(author, message);
                    break;
                default:
                    await AddViaRawAsync(author, message);
                    break;
            }

            _dbAuthor.Value = "";
            _dbMessage.Value = "";
            await RefreshEntriesAsync();
        }
        catch (RowLimitReachedException)
        {
            _dbStatus.Value = $"Row limit reached ({MaxRows}). Delete some entries first.";
        }
        catch (Exception ex)
        {
            _dbStatus.Value = $"Insert failed: {ex.Message}";
            Log.Instance.Warning($"Database insert failed: {ex}");
        }
        finally
        {
            _dbBusy.Value = false;
        }
    }

    // --- EF Core ---

    private async Task AddViaEfAsync(string author, string message)
    {
        await using var db = CreateDbContext();

        if (await db.Entries.CountAsync() >= MaxRows)
        {
            throw new RowLimitReachedException();
        }

        db.Entries.Add(new Entry
        {
            Author = author,
            Message = message,
            Source = "EF Core",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _dbStatus.Value = "Added via EF Core";
    }

    private async Task<List<Entry>> ReadViaEfAsync()
    {
        await using var db = CreateDbContext();
        return await db.Entries.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    // --- Dapper ---

    private async Task AddViaDapperAsync(string author, string message)
    {
        await using var connection = app.Database(DatabaseName);
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM entries");

        if (count >= MaxRows)
        {
            throw new RowLimitReachedException();
        }

        await connection.ExecuteAsync(
            "INSERT INTO entries (author, message, source, created_at) VALUES (@Author, @Message, @Source, @CreatedAt)",
            new { Author = author, Message = message, Source = "Dapper", CreatedAt = DateTime.UtcNow });

        _dbStatus.Value = "Added via Dapper";
    }

    private async Task<List<Entry>> ReadViaDapperAsync()
    {
        await using var connection = app.Database(DatabaseName);
        await connection.OpenAsync();

        var rows = await connection.QueryAsync<Entry>(
            "SELECT id AS Id, author AS Author, message AS Message, source AS Source, created_at AS CreatedAt FROM entries ORDER BY created_at DESC");

        return rows.ToList();
    }

    // --- Raw ADO.NET / Npgsql ---

    private async Task AddViaRawAsync(string author, string message)
    {
        await using var connection = app.Database(DatabaseName);
        await connection.OpenAsync();

        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(*) FROM entries";
            var count = Convert.ToInt64(await countCommand.ExecuteScalarAsync());

            if (count >= MaxRows)
            {
                throw new RowLimitReachedException();
            }
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO entries (author, message, source, created_at) VALUES (@author, @message, @source, @created_at)";
        AddParameter(command, "@author", author);
        AddParameter(command, "@message", message);
        AddParameter(command, "@source", "Raw SQL");
        AddParameter(command, "@created_at", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync();

        _dbStatus.Value = "Added via Raw SQL";
    }

    private async Task<List<Entry>> ReadViaRawAsync()
    {
        await using var connection = app.Database(DatabaseName);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, author, message, source, created_at FROM entries ORDER BY created_at DESC";

        var entries = new List<Entry>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            entries.Add(new Entry
            {
                Id = reader.GetInt64(0),
                Author = reader.GetString(1),
                Message = reader.GetString(2),
                Source = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return entries;
    }

    // --- Shared ---

    private async Task RefreshEntriesAsync()
    {
        if (!DatabaseAvailable)
        {
            return;
        }

        try
        {
            _dbEntries.Value = _dbReadVia.Value switch
            {
                "dapper" => await ReadViaDapperAsync(),
                "raw" => await ReadViaRawAsync(),
                _ => await ReadViaEfAsync()
            };
        }
        catch (Exception ex)
        {
            _dbStatus.Value = $"Read failed: {ex.Message}";
            Log.Instance.Warning($"Database read failed: {ex}");
        }
    }

    private async Task DeleteAllEntriesAsync()
    {
        if (_dbBusy.Value)
        {
            return;
        }

        _dbBusy.Value = true;

        try
        {
            await using var connection = app.Database(DatabaseName);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM entries";
            await command.ExecuteNonQueryAsync();

            _dbStatus.Value = "Deleted all entries";
            await RefreshEntriesAsync();
        }
        catch (Exception ex)
        {
            _dbStatus.Value = $"Delete failed: {ex.Message}";
            Log.Instance.Warning($"Database delete failed: {ex}");
        }
        finally
        {
            _dbBusy.Value = false;
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed class RowLimitReachedException : Exception;
}
