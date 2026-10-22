using System.Globalization;

namespace Ikon.App.Patterns.Patterns;

// Pattern: week-grid — see docs/patterns/week-grid.md.
// The week and its entries stand in for what the app keeps in its own store.
internal sealed class WeekGrid : IPatternDemo
{
    public string Slug => "week-grid";
    public string Title => "Week grid that fits a phone";
    public string Category => "Layout";
    public void RenderDemo(IView view) => Render((UIView)view);

    #region example:pattern-week-grid
    private sealed record Entry(DateOnly Day, string Time, string Title);

    private static readonly DateOnly _monday = new(2026, 10, 19);

    private readonly Entry[] _entries =
    [
        new(_monday, "09:00", "Team stand-up"),
        new(_monday, "14:00", "Design review"),
        new(_monday.AddDays(1), "10:30", "Dentist"),
        new(_monday.AddDays(2), "08:00", "Gym"),
        new(_monday.AddDays(2), "12:00", "Lunch with Sam"),
        new(_monday.AddDays(2), "16:00", "Quarterly planning"),
        new(_monday.AddDays(2), "19:00", "Book club"),
        new(_monday.AddDays(4), "17:30", "Pick up the kids"),
        new(_monday.AddDays(5), "11:00", "Farmers' market"),
    ];

    private readonly ClientReactive<int> _selectedDay = new(0);

    private void Render(UIView view)
    {
        var selected = _monday.AddDays(_selectedDay.Value);

        view.Column(["gap-4 w-full max-w-5xl"], content: col =>
        {
            // grid-cols-7 with min-w-0 columns divides the width by seven at any size. A minimum
            // width on the grid would push Friday to Sunday off a phone screen.
            col.Box(["grid grid-cols-7 gap-1 sm:gap-2 w-full"], content: grid =>
            {
                for (var offset = 0; offset < 7; offset++)
                {
                    var day = _monday.AddDays(offset);
                    var dayOffset = offset;
                    var entries = _entries.Where(e => e.Day == day).ToList();
                    var isSelected = day == selected;

                    grid.Button(
                        [
                            "flex min-w-0 flex-col items-stretch gap-1 rounded-lg border p-1 sm:p-2 text-start min-h-20 sm:min-h-32 cursor-pointer transition-colors",
                            isSelected ? "border-brand-solid bg-brand-secondary" : "border-secondary bg-card hover:bg-muted",
                        ],
                        ariaLabel: $"{day.ToString("dddd d MMMM", CultureInfo.InvariantCulture)}, {(entries.Count == 1 ? "1 entry" : $"{entries.Count} entries")}",
                        onClick: () => _selectedDay.Value = dayOffset,
                        content: cell =>
                        {
                            // One letter on a phone, the short name from sm up
                            var shortName = day.ToString("ddd", CultureInfo.InvariantCulture);
                            cell.Text(["text-[11px] font-semibold uppercase text-tertiary text-center sm:hidden"], shortName[..1]);
                            cell.Text(["hidden sm:block text-xs font-semibold uppercase text-tertiary"], shortName);
                            cell.Text(["text-sm sm:text-base font-semibold text-primary text-center sm:text-start tabular-nums"], day.Day.ToString(CultureInfo.InvariantCulture));

                            // A phone shows a dot per entry; a wider screen shows the first titles
                            cell.Box(["flex flex-wrap justify-center gap-0.5 sm:hidden"], content: dots =>
                            {
                                foreach (var _ in entries.Take(4))
                                {
                                    dots.Box(["size-1.5 rounded-full bg-brand-solid"]);
                                }
                            });

                            cell.Box(["hidden sm:flex flex-col gap-0.5 min-w-0"], content: titles =>
                            {
                                foreach (var entry in entries.Take(3))
                                {
                                    titles.Text(["text-xs text-secondary truncate"], $"{entry.Time} {entry.Title}");
                                }

                                if (entries.Count > 3)
                                {
                                    titles.Text(["text-xs text-tertiary"], $"+{entries.Count - 3} more");
                                }
                            });
                        });
                }
            });

            // The tapped day's entries, in full, under the grid
            col.Column(["gap-2"], content: list =>
            {
                list.Text([Text.H5], selected.ToString("dddd d MMMM", CultureInfo.InvariantCulture));
                var dayEntries = _entries.Where(e => e.Day == selected).ToList();

                if (dayEntries.Count == 0)
                {
                    list.Text([Text.Caption], "Nothing planned");
                }

                foreach (var entry in dayEntries)
                {
                    list.Row(["gap-3 items-baseline rounded-md border border-secondary bg-card px-3 py-2"], content: row =>
                    {
                        row.Text(["text-sm font-medium text-primary tabular-nums shrink-0"], entry.Time);
                        row.Text(["text-sm text-secondary min-w-0 break-words"], entry.Title);
                    });
                }
            });
        });
    }
    #endregion
}
