<!-- mined-from: MuistiSeniori -->
# Relative-Time With i18n Buckets — "5 min ago" Across Languages

A nullable timestamp is formatted into a human "ago" string by bucketing the elapsed time into four ranges (just now / N min ago / N h ago / N d ago) and looking up the localized format string via the app's `T(key, args)` helper. Renders as a small caption under the entity's name; if the timestamp is null, falls back to a separate "no activity yet" string.

## When to use

Lists where each row needs a "last seen" / "last active" / "last updated" caption — patient dashboards, presence indicators, status pages, recently-edited file lists. The bucketing is what makes it feel right: one decimal hour ("1.4 h ago") looks fake; "1 h ago" is the conversational form.

## Snippet

```csharp
// Translation table (Finnish)
["nurse.last_active"] = "viimeksi aktiivinen {0}",
["nurse.no_activity"] = "ei vielä aktiviteettia",
["nurse.now"]         = "juuri nyt",
["nurse.min_ago"]     = "{0} min sitten",
["nurse.hour_ago"]    = "{0} h sitten",
["nurse.day_ago"]     = "{0} d sitten",

// Translation table (English)
["nurse.last_active"] = "last active {0}",
["nurse.no_activity"] = "no activity yet",
["nurse.now"]         = "just now",
["nurse.min_ago"]     = "{0} min ago",
["nurse.hour_ago"]    = "{0} h ago",
["nurse.day_ago"]     = "{0} d ago",

// Render in row
foreach (var p in _unitPatients.Value)
{
    var patient = p;
    var stats = _unitTimeStats.Value.TryGetValue(patient.Id, out var s) ? s : new TimeStats(0, 0, 0, 0);

    view.Column(["gap-1 flex-1"], content: view =>
    {
        view.Text(["font-medium"], patient.FullName);
        view.Text(["text-xs text-gray-500"],
            T("nurse.patient_summary",
                stats.AllSeconds / 60,
                stats.MentalSeconds / 60,
                stats.PhysicalSeconds / 60,
                stats.SocialSeconds / 60));

        var last = _unitLastActive.Value.TryGetValue(patient.Id, out var l) ? l : null;

        if (last.HasValue)
        {
            var ago = DateTime.UtcNow - last.Value;
            var agoLabel =
                ago.TotalMinutes < 1 ? T("nurse.now")
                : ago.TotalHours < 1 ? T("nurse.min_ago", (int)ago.TotalMinutes)
                : ago.TotalDays < 1 ? T("nurse.hour_ago", (int)ago.TotalHours)
                : T("nurse.day_ago", (int)ago.TotalDays);

            view.Text(["text-xs text-gray-400"], T("nurse.last_active", agoLabel));
        }
        else
        {
            view.Text(["text-xs text-gray-400 italic"], T("nurse.no_activity"));
        }
    });
}
```

## Notes

- Use `int` casts (`(int)ago.TotalMinutes`) — fractional buckets ("1.4 h ago") read as machine output, not natural language.
- The thresholds are open-on-the-right (`< 1` minute, `< 1` hour, `< 1` day) — at exactly one minute the user sees `1 min ago`, not `0 min ago` or `60 sec ago`. Don't try to be too granular below a minute; "just now" is enough.
- Compose the final string via a *second* template (`nurse.last_active = "last active {0}"`) so the full sentence reads naturally in languages with different prepositions or word order. Direct concatenation breaks for SOV languages.
- Distinguish "never active" (italic muted) from "5 min ago" (regular muted) — italic carries the "no data" semantic without needing a second color.
- Bucket pre-formatted summary stats (`AllSeconds / 60`) by minute too — showing "total 3 min · brain 1 min" matches the resolution of the activity-time data.

## See also

- `language-picker-i18n` — how `T(key, args)` and the language reactive are wired
- `kpi-card-grid` — when the timestamps drive headline tiles instead of row captions
