using System.Data.Common;

public partial class MomentumApp
{
    private const string DbName = "app";

    private async Task EnsureSchemaAsync()
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS activities (
                id              TEXT PRIMARY KEY,
                user_id         TEXT NOT NULL,
                kind            INT NOT NULL,
                title           TEXT NOT NULL,
                story           TEXT NOT NULL DEFAULT '',
                started_at      TIMESTAMPTZ NOT NULL,
                distance_m      DOUBLE PRECISION NOT NULL,
                moving_s        DOUBLE PRECISION NOT NULL,
                elapsed_s       DOUBLE PRECISION NOT NULL,
                ascent_m        DOUBLE PRECISION NOT NULL,
                descent_m       DOUBLE PRECISION NOT NULL,
                avg_speed_mps   DOUBLE PRECISION NOT NULL,
                max_speed_mps   DOUBLE PRECISION NOT NULL,
                momentum_score  DOUBLE PRECISION NOT NULL,
                published       BOOLEAN NOT NULL DEFAULT FALSE
            );
            -- An outing being recorded right now. The row and its points are written as the ride
            -- happens, so a deploy or a crash under the rider costs the seconds since the last flush
            -- rather than the whole ride.
            ALTER TABLE activities ADD COLUMN IF NOT EXISTS in_progress BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE activities ADD COLUMN IF NOT EXISTS simulated BOOLEAN NOT NULL DEFAULT FALSE;
            CREATE INDEX IF NOT EXISTS activities_in_progress ON activities (user_id) WHERE in_progress;
            CREATE INDEX IF NOT EXISTS activities_user_started ON activities (user_id, started_at DESC);
            CREATE TABLE IF NOT EXISTS activity_points (
                activity_id  TEXT NOT NULL REFERENCES activities (id) ON DELETE CASCADE,
                seconds      DOUBLE PRECISION NOT NULL,
                lat          DOUBLE PRECISION NOT NULL,
                lon          DOUBLE PRECISION NOT NULL,
                elev_m       DOUBLE PRECISION NOT NULL,
                speed_mps    DOUBLE PRECISION NOT NULL,
                heading_deg  DOUBLE PRECISION NOT NULL,
                accuracy_m   DOUBLE PRECISION NOT NULL,
                distance_m   DOUBLE PRECISION NOT NULL,
                moving       BOOLEAN NOT NULL,
                PRIMARY KEY (activity_id, seconds)
            );
            CREATE TABLE IF NOT EXISTS activity_highlights (
                id            TEXT PRIMARY KEY,
                activity_id   TEXT NOT NULL REFERENCES activities (id) ON DELETE CASCADE,
                detector      TEXT NOT NULL,
                title         TEXT NOT NULL,
                detail        TEXT NOT NULL,
                start_s       DOUBLE PRECISION NOT NULL,
                end_s         DOUBLE PRECISION NOT NULL,
                score         DOUBLE PRECISION NOT NULL,
                tier          INT NOT NULL,
                icon          TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS highlights_activity ON activity_highlights (activity_id);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<IReadOnlyList<Activity>> LoadActivitiesAsync(string userId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, kind, title, story, started_at, distance_m, moving_s, elapsed_s, ascent_m, descent_m,
                   avg_speed_mps, max_speed_mps, momentum_score, published
            FROM activities WHERE user_id = @user_id AND NOT in_progress ORDER BY started_at DESC LIMIT 200;
            """;
        Bind(cmd, "user_id", userId);

        var activities = new List<Activity>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            activities.Add(new Activity(
                reader.GetString(0),
                reader.GetString(1),
                (ActivityKind)reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetDateTime(5),
                reader.GetDouble(6),
                reader.GetDouble(7),
                reader.GetDouble(8),
                reader.GetDouble(9),
                reader.GetDouble(10),
                reader.GetDouble(11),
                reader.GetDouble(12),
                reader.GetDouble(13),
                reader.GetBoolean(14)));
        }

        return activities;
    }

    private async Task<IReadOnlyList<TrackPoint>> LoadPointsAsync(string activityId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT seconds, lat, lon, elev_m, speed_mps, heading_deg, accuracy_m, distance_m, moving
            FROM activity_points WHERE activity_id = @activity_id ORDER BY seconds;
            """;
        Bind(cmd, "activity_id", activityId);

        var points = new List<TrackPoint>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            points.Add(new TrackPoint(
                reader.GetDouble(0),
                new GeoPoint(reader.GetDouble(1), reader.GetDouble(2)),
                reader.GetDouble(3),
                reader.GetDouble(4),
                reader.GetDouble(5),
                reader.GetDouble(6),
                reader.GetDouble(7),
                reader.GetBoolean(8)));
        }

        return points;
    }

    private async Task<IReadOnlyList<Highlight>> LoadHighlightsAsync(string activityId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, activity_id, detector, title, detail, start_s, end_s, score, tier, icon
            FROM activity_highlights WHERE activity_id = @activity_id ORDER BY score DESC;
            """;
        Bind(cmd, "activity_id", activityId);

        var highlights = new List<Highlight>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            highlights.Add(new Highlight(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetDouble(5),
                reader.GetDouble(6),
                reader.GetDouble(7),
                (MedalTier)reader.GetInt32(8),
                reader.GetString(9)));
        }

        return highlights;
    }

    /// <summary>The rider's best previous score per detector, which is what a medal is measured against.</summary>
    private async Task<Dictionary<string, double>> LoadPersonalBestsAsync(string userId, ActivityKind kind)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT h.detector, MAX(h.score)
            FROM activity_highlights h
            JOIN activities a ON a.id = h.activity_id
            WHERE a.user_id = @user_id AND a.kind = @kind
            GROUP BY h.detector;
            """;
        Bind(cmd, "user_id", userId);
        Bind(cmd, "kind", (int)kind);

        var bests = new Dictionary<string, double>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            bests[reader.GetString(0)] = reader.GetDouble(1);
        }

        return bests;
    }

    /// <summary>
    /// Writes the row for an outing that has just started. Everything after this is an update: the
    /// ride exists in the database from its first second, not from the moment it ends.
    /// </summary>
    private async Task BeginActivityAsync(string activityId, string userId, ActivityKind kind, DateTime startedAt, bool simulated)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO activities (id, user_id, kind, title, story, started_at, distance_m, moving_s, elapsed_s,
                                    ascent_m, descent_m, avg_speed_mps, max_speed_mps, momentum_score, published,
                                    in_progress, simulated)
            VALUES (@id, @user_id, @kind, '', '', @started_at, 0, 0, 0, 0, 0, 0, 0, 0, FALSE, TRUE, @simulated)
            ON CONFLICT (id) DO NOTHING;
            """;
        Bind(cmd, "id", activityId);
        Bind(cmd, "user_id", userId);
        Bind(cmd, "kind", (int)kind);
        Bind(cmd, "started_at", startedAt);
        Bind(cmd, "simulated", simulated);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Appends the points recorded since the last flush and updates the running totals.</summary>
    private async Task SaveProgressAsync(string activityId, RecordedTrack progress, IReadOnlyList<TrackPoint> newPoints)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = """
                UPDATE activities SET distance_m = @distance_m, moving_s = @moving_s, elapsed_s = @elapsed_s,
                       ascent_m = @ascent_m, descent_m = @descent_m, avg_speed_mps = @avg_speed_mps,
                       max_speed_mps = @max_speed_mps
                WHERE id = @id;
                """;
            Bind(cmd, "id", activityId);
            Bind(cmd, "distance_m", progress.DistanceM);
            Bind(cmd, "moving_s", progress.MovingSeconds);
            Bind(cmd, "elapsed_s", progress.ElapsedSeconds);
            Bind(cmd, "ascent_m", progress.AscentM);
            Bind(cmd, "descent_m", progress.DescentM);
            Bind(cmd, "avg_speed_mps", progress.AvgSpeedMps);
            Bind(cmd, "max_speed_mps", progress.MaxSpeedMps);
            await cmd.ExecuteNonQueryAsync();
        }

        await InsertPointsAsync(connection, transaction, activityId, newPoints);
        await transaction.CommitAsync();
    }

    /// <summary>The outing this rider was in the middle of when the app last stopped, if any.</summary>
    private async Task<(Activity Activity, bool Simulated)?> LoadInProgressAsync(string userId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, kind, title, story, started_at, distance_m, moving_s, elapsed_s, ascent_m, descent_m,
                   avg_speed_mps, max_speed_mps, momentum_score, published, simulated
            FROM activities WHERE user_id = @user_id AND in_progress ORDER BY started_at DESC LIMIT 1;
            """;
        Bind(cmd, "user_id", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var activity = new Activity(
            reader.GetString(0), reader.GetString(1), (ActivityKind)reader.GetInt32(2), reader.GetString(3),
            reader.GetString(4), reader.GetDateTime(5), reader.GetDouble(6), reader.GetDouble(7), reader.GetDouble(8),
            reader.GetDouble(9), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12),
            reader.GetDouble(13), reader.GetBoolean(14));

        return (activity, reader.GetBoolean(15));
    }

    private async Task DeleteActivityAsync(string activityId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM activities WHERE id = @id;";
        Bind(cmd, "id", activityId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Turns the in-progress row into a published outing: the rider's title and the write-up, the
    /// final score, and the highlights. The points are already in the database — they were written as
    /// the ride happened — so this never touches them.
    /// </summary>
    private async Task FinalizeActivityAsync(Activity activity, IReadOnlyList<TrackPoint> points, IReadOnlyList<Highlight> highlights)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO activities (id, user_id, kind, title, story, started_at, distance_m, moving_s, elapsed_s,
                                        ascent_m, descent_m, avg_speed_mps, max_speed_mps, momentum_score, published,
                                        in_progress, simulated)
                VALUES (@id, @user_id, @kind, @title, @story, @started_at, @distance_m, @moving_s, @elapsed_s,
                        @ascent_m, @descent_m, @avg_speed_mps, @max_speed_mps, @momentum_score, @published, FALSE, FALSE)
                ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title, story = EXCLUDED.story,
                        distance_m = EXCLUDED.distance_m, moving_s = EXCLUDED.moving_s, elapsed_s = EXCLUDED.elapsed_s,
                        ascent_m = EXCLUDED.ascent_m, descent_m = EXCLUDED.descent_m,
                        avg_speed_mps = EXCLUDED.avg_speed_mps, max_speed_mps = EXCLUDED.max_speed_mps,
                        momentum_score = EXCLUDED.momentum_score, published = EXCLUDED.published,
                        in_progress = FALSE;
                """;
            Bind(cmd, "id", activity.Id);
            Bind(cmd, "user_id", activity.UserId);
            Bind(cmd, "kind", (int)activity.Kind);
            Bind(cmd, "title", activity.Title);
            Bind(cmd, "story", activity.Story);
            Bind(cmd, "started_at", activity.StartedAt);
            Bind(cmd, "distance_m", activity.DistanceM);
            Bind(cmd, "moving_s", activity.MovingSeconds);
            Bind(cmd, "elapsed_s", activity.ElapsedSeconds);
            Bind(cmd, "ascent_m", activity.AscentM);
            Bind(cmd, "descent_m", activity.DescentM);
            Bind(cmd, "avg_speed_mps", activity.AvgSpeedMps);
            Bind(cmd, "max_speed_mps", activity.MaxSpeedMps);
            Bind(cmd, "momentum_score", activity.MomentumScore);
            Bind(cmd, "published", activity.Published);
            await cmd.ExecuteNonQueryAsync();
        }

        // A seeded outing has never been flushed, so its points arrive here; a recorded one has been
        // writing them all along and this is a no-op.
        await InsertPointsAsync(connection, transaction, activity.Id, points);

        foreach (var highlight in highlights)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO activity_highlights (id, activity_id, detector, title, detail, start_s, end_s, score, tier, icon)
                VALUES (@id, @activity_id, @detector, @title, @detail, @start_s, @end_s, @score, @tier, @icon)
                ON CONFLICT (id) DO UPDATE SET score = EXCLUDED.score, tier = EXCLUDED.tier, detail = EXCLUDED.detail;
                """;
            Bind(cmd, "id", highlight.Id);
            Bind(cmd, "activity_id", highlight.ActivityId);
            Bind(cmd, "detector", highlight.Detector);
            Bind(cmd, "title", highlight.Title);
            Bind(cmd, "detail", highlight.Detail);
            Bind(cmd, "start_s", highlight.StartSeconds);
            Bind(cmd, "end_s", highlight.EndSeconds);
            Bind(cmd, "score", highlight.Score);
            Bind(cmd, "tier", (int)highlight.Tier);
            Bind(cmd, "icon", highlight.Icon);
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    /// <summary>
    /// One multi-row statement per chunk. An hour of driving is 3,600 points and a round trip each
    /// would make a flush take the best part of a minute.
    /// </summary>
    private static async Task InsertPointsAsync(DbConnection connection, DbTransaction transaction, string activityId, IReadOnlyList<TrackPoint> points)
    {
        foreach (var chunk in points.Chunk(500))
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            var sql = new StringBuilder("INSERT INTO activity_points (activity_id, seconds, lat, lon, elev_m, speed_mps, heading_deg, accuracy_m, distance_m, moving) VALUES ");

            for (int i = 0; i < chunk.Length; i++)
            {
                var point = chunk[i];
                sql.Append(i == 0 ? "" : ",");
                sql.Append($"(@activity_id, @s{i}, @la{i}, @lo{i}, @el{i}, @sp{i}, @he{i}, @ac{i}, @di{i}, @mv{i})");
                Bind(cmd, $"s{i}", point.Seconds);
                Bind(cmd, $"la{i}", point.Point.Lat);
                Bind(cmd, $"lo{i}", point.Point.Lon);
                Bind(cmd, $"el{i}", point.ElevationM);
                Bind(cmd, $"sp{i}", point.SpeedMps);
                Bind(cmd, $"he{i}", point.HeadingDeg);
                Bind(cmd, $"ac{i}", point.AccuracyM);
                Bind(cmd, $"di{i}", point.DistanceM);
                Bind(cmd, $"mv{i}", point.Moving);
            }

            sql.Append(" ON CONFLICT (activity_id, seconds) DO NOTHING;");
            cmd.CommandText = sql.ToString();
            Bind(cmd, "activity_id", activityId);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Every outing with the counts that say whether it worked — across all users, because this backs
    /// the operator view rather than anybody's own log.
    /// </summary>
    private async Task<IReadOnlyList<AdminRow>> LoadAdminRowsAsync(int limit = 40)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT a.id, a.user_id, a.kind, a.title, a.story, a.started_at, a.distance_m, a.moving_s, a.elapsed_s,
                   a.ascent_m, a.descent_m, a.avg_speed_mps, a.max_speed_mps, a.momentum_score, a.published,
                   a.in_progress, a.simulated,
                   (SELECT COUNT(*) FROM activity_points p WHERE p.activity_id = a.id) AS points,
                   (SELECT COUNT(*) FROM activity_highlights h WHERE h.activity_id = a.id) AS highlights,
                   (SELECT COUNT(*) FROM activity_highlights h WHERE h.activity_id = a.id
                        AND (h.detector LIKE 'motion-%' OR h.detector LIKE 'gait-%')) AS motion
            FROM activities a
            ORDER BY a.started_at DESC
            LIMIT @limit;
            """;
        Bind(cmd, "limit", limit);

        var rows = new List<AdminRow>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var activity = new Activity(
                reader.GetString(0), reader.GetString(1), (ActivityKind)reader.GetInt32(2), reader.GetString(3),
                reader.GetString(4), reader.GetDateTime(5), reader.GetDouble(6), reader.GetDouble(7),
                reader.GetDouble(8), reader.GetDouble(9), reader.GetDouble(10), reader.GetDouble(11),
                reader.GetDouble(12), reader.GetDouble(13), reader.GetBoolean(14));

            rows.Add(new AdminRow(
                activity,
                PointCount: (int)reader.GetInt64(17),
                HighlightCount: (int)reader.GetInt64(18),
                HasMotionAnalysis: reader.GetInt64(19) > 0,
                InProgress: reader.GetBoolean(15),
                Simulated: reader.GetBoolean(16),
                ArchiveStored: false,
                ArchiveBytes: 0));
        }

        return rows;
    }

    private async Task<Activity?> LoadActivityAsync(string activityId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, user_id, kind, title, story, started_at, distance_m, moving_s, elapsed_s, ascent_m, descent_m,
                   avg_speed_mps, max_speed_mps, momentum_score, published
            FROM activities WHERE id = @id;
            """;
        Bind(cmd, "id", activityId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Activity(
            reader.GetString(0),
            reader.GetString(1),
            (ActivityKind)reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetDateTime(5),
            reader.GetDouble(6),
            reader.GetDouble(7),
            reader.GetDouble(8),
            reader.GetDouble(9),
            reader.GetDouble(10),
            reader.GetDouble(11),
            reader.GetDouble(12),
            reader.GetDouble(13),
            reader.GetBoolean(14));
    }

    /// <summary>
    /// Swaps in a track re-derived from a device's own recording, replacing the points and highlights
    /// that were built from whatever the network delivered.
    /// </summary>
    /// <remarks>
    /// The old points go first. <see cref="InsertPointsAsync"/> is deliberately a no-op on conflict —
    /// it is written for progressive flushes, where re-inserting a row is normal — so leaving them in
    /// place would silently keep the gappy track and drop the repaired one. The rider's own words
    /// survive: the title and story are theirs and were never derived from the fixes.
    /// </remarks>
    private async Task ReplaceTrackAsync(Activity activity, IReadOnlyList<TrackPoint> points, IReadOnlyList<Highlight> highlights)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        foreach (var table in new[] { "activity_points", "activity_highlights" })
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = $"DELETE FROM {table} WHERE activity_id = @activity_id;";
            Bind(cmd, "activity_id", activity.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        await FinalizeActivityAsync(activity, points, highlights);
    }

    /// <summary>Replaces only the words. The measurements are not the model's to change.</summary>
    private async Task SaveWriteUpAsync(string activityId, string title, string story)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE activities SET title = @title, story = @story WHERE id = @id;";
        Bind(cmd, "title", title);
        Bind(cmd, "story", story);
        Bind(cmd, "id", activityId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task DeleteHighlightAsync(string highlightId)
    {
        await using var connection = await app.DatabaseAsync(DbName);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM activity_highlights WHERE id = @id;";
        Bind(cmd, "id", highlightId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void Bind(DbCommand cmd, string name, object? value)
    {
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(parameter);
    }
}
