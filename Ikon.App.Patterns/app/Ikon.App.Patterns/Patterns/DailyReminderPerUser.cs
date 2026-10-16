using Dapper;
using Ikon.Common.Core.Protocol;

namespace Ikon.App.Patterns.Patterns;

// Pattern: daily-reminder-per-user — see docs/patterns/daily-reminder-per-user.md.
internal sealed class DailyReminderPerUser : IPatternDemo
{
    public string Slug => "daily-reminder-per-user";
    public string Title => "Daily reminder per user";
    public string Category => "Notifications";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern: an hourly [Cron] job reads every user's time zone and last activity from the database. It sends one notification to each user whose local reminder hour it is and who has not been active today. The notification is kept in the in-app inbox and pushed to every device. See docs/patterns/daily-reminder-per-user.md.");
}

// Abstract so neither the bundle scan nor the Patterns app registers its [Cron] job: the gallery has
// no reminders table to read.
file abstract class DailyReminderPerUserExample(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:pattern-daily-reminder-per-user
    private const int ReminderHour = 19;

    // The inbox keeps every reminder in the app's bell as well as pushing it, and sends it on the
    // channels a route names, such as an EmailNotificationChannel registered in OnStarting.
    private readonly NotificationInbox _inbox = new(app);

    // Npgsql reads a Postgres date column as DateOnly; a DateTime property here fails at runtime.
    private sealed record ReminderRow(string UserId, string TimeZone, DateOnly? LastActive, DateOnly? LastReminded);

    // Run once from app.OnStarting.
    private async Task CreateRemindersTableAsync()
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS reminders (
                user_id text PRIMARY KEY,
                time_zone text NOT NULL,
                last_active date,
                last_reminded date)
            """);
    }

    // Call from app.OnClientJoined: the client's time zone is known only while it is connected, and
    // the cron job below runs with no client connected. A deploy's boot-snapshot capture joins as a
    // client too (ctx.IsSnapshot, user id "snapshot-0"), and it is not a user to remind.
    private async Task RememberUserAsync(Context ctx)
    {
        if (string.IsNullOrEmpty(ctx.UserId) || ctx.IsSnapshot)
        {
            return;
        }

        var timeZone = string.IsNullOrEmpty(ctx.Timezone) ? "UTC" : ctx.Timezone;
        var today = LocalToday(timeZone, DateTime.UtcNow);

        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            INSERT INTO reminders (user_id, time_zone, last_active) VALUES (@userId, @timeZone, @today)
            ON CONFLICT (user_id) DO UPDATE SET time_zone = excluded.time_zone, last_active = excluded.last_active
            """, new { userId = ctx.UserId, timeZone, today });
    }

    // Every hour on the hour, UTC. Whoever's local reminder hour it now is, and who has not been active
    // or reminded today, gets one notification. The hour comes from cron.FireTimeUtc, so
    // 'ikon cron run --at <UTC time>' tests any hour.
    [Cron("0 * * * *")]
    public async Task SendDailyRemindersAsync(CronContext cron, CancellationToken ct)
    {
        await using var connection = await app.DatabaseAsync();
        await connection.OpenAsync(ct);

        var rows = await connection.QueryAsync<ReminderRow>("""
            SELECT user_id AS UserId, time_zone AS TimeZone, last_active AS LastActive, last_reminded AS LastReminded
            FROM reminders
            """);

        foreach (var row in rows)
        {
            var now = LocalNow(row.TimeZone, cron.FireTimeUtc);
            var today = DateOnly.FromDateTime(now);

            if (now.Hour != ReminderHour || row.LastActive == today || row.LastReminded == today)
            {
                continue;
            }

            // Mark first: overlapping ticks are allowed, and a second tick must not send the same reminder.
            var claimed = await connection.ExecuteAsync(
                "UPDATE reminders SET last_reminded = @today WHERE user_id = @userId AND last_reminded IS DISTINCT FROM @today",
                new { today, userId = row.UserId });

            if (claimed == 0)
            {
                continue;
            }

            // AllDevices pushes to every device the user registered; NotificationRoute.Everywhere("email")
            // would also email it.
            await _inbox.NotifyAsync(row.UserId,
                new NotificationContent("Time for today's practice", "Ten minutes keeps your streak going", Tag: "daily-reminder", LaunchUrl: "/"),
                kind: "reminder", route: NotificationRoute.AllDevices, ct: ct);
        }
    }

    // An unknown zone id falls back to UTC rather than failing the whole job.
    private static DateTime LocalNow(string timeZone, DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc,
            TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone) ? zone : TimeZoneInfo.Utc);

    private static DateOnly LocalToday(string timeZone, DateTime utc) => DateOnly.FromDateTime(LocalNow(timeZone, utc));
    #endregion
}
