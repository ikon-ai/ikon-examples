public partial class MomentumApp
{
    private void RenderYou(UIView view)
    {
        string? userId = ReactiveScope.UserIdOrNull;
        var activities = userId is { Length: > 0 } ? ActivitiesFor(userId) : [];

        view.Column(["gap-9"], content: view =>
        {
            SectionTitle(view, "Rider", _riderName.Value);

            view.Grid(["grid-cols-2 md:grid-cols-4 gap-x-7"], content: view =>
            {
                Tile(view, "Outings", $"{activities.Count}", "");
                Tile(view, "Distance", $"{activities.Sum(a => a.DistanceM) / 1000:0.0}", "km");
                Tile(view, "Moving", TotalMoving(activities), "");
                Tile(view, "Climbed", $"{activities.Sum(a => a.AscentM):0}", "m");
            });

            view.Grid(["grid-cols-1 lg:grid-cols-[3fr_2fr] gap-9"], content: view =>
            {
                view.Column(["gap-3"], content: view =>
                {
                    Kicker(view, "Distance by week · km");
                    RenderWeeklyChart(view, activities);
                });

                view.Column(["gap-7"], content: view =>
                {
                    RenderByKind(view, activities);
                    RenderCoachSettings(view);
                });
            });
        });
    }

    private static string TotalMoving(IReadOnlyList<Activity> activities)
    {
        var span = TimeSpan.FromSeconds(activities.Sum(a => a.MovingSeconds));
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{span.Minutes}m";
    }

    private static void RenderByKind(UIView view, IReadOnlyList<Activity> activities)
    {
        view.Column(["gap-3"], content: view =>
        {
            Kicker(view, "By kind");

            view.Column(["gap-1"], content: view =>
            {
                double total = Math.Max(1, activities.Sum(a => a.DistanceM));

                foreach (var kind in (ActivityKind[])[ActivityKind.Foot, ActivityKind.Bike, ActivityKind.Horse, ActivityKind.Car])
                {
                    var forKind = activities.Where(a => a.Kind == kind).ToList();
                    double distance = forKind.Sum(a => a.DistanceM);
                    double share = distance / total * 100;

                    view.Row(["items-center gap-3 h-8 border-b border-border"], key: kind.ToString(), content: view =>
                    {
                        view.Icon(Momentum.ProfileOf(kind).Icon, ["text-muted-foreground shrink-0"], size: IconSize.Sm);
                        view.Text(kind == ActivityKind.Foot ? "On foot" : Momentum.ProfileOf(kind).Label,
                            ["text-[13px] w-[74px] shrink-0"]);

                        view.Box(["flex-1 h-1.5 rounded-[1px] bg-[#242427] overflow-hidden"], content: view =>
                        {
                            view.Box([$"h-full rounded-[1px] bg-[#db176e] w-[{Math.Max(share, forKind.Count > 0 ? 3 : 0):0}%]"]);
                        });

                        view.Text($"{distance / 1000:0.0} km", [Brand.Mono, "text-[11.5px] w-[64px] shrink-0 text-right"]);
                    });
                }
            });
        });
    }

    private void RenderCoachSettings(UIView view)
    {
        view.Column(["gap-4"], content: view =>
        {
            Kicker(view, "The coach");

            view.Row(["items-center justify-between gap-4 border-t border-border pt-4"], content: view =>
            {
                view.Column(["gap-1 min-w-0"], content: view =>
                {
                    view.Text("Speak the cues", ["text-[14px] text-[#f7f7f7]"]);
                    view.Text("Your phone is in a pocket, so a cue that is only written is a cue you never get.",
                        ["text-[12px] text-muted-foreground leading-relaxed"]);
                });

                view.Switch(["default"],
                    value: _coachVoice.Value,
                    ariaLabel: "Speak the coach cues aloud",
                    onValueChange: async value => _coachVoice.Value = value);
            });

            view.Text(
                "Highlights are measured, not guessed — climbs, launches off the line, clean straights, corner load, gaits and splits all come out of the track itself. The coach only decides what is worth saying about them.",
                ["text-[12.5px] text-muted-foreground leading-relaxed border-t border-border pt-4"]);
        });
    }

    private static void RenderWeeklyChart(UIView view, IReadOnlyList<Activity> activities)
    {
        var today = DateTime.UtcNow.Date;
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var thisMonday = today.AddDays(-daysSinceMonday);

        var weeks = Enumerable.Range(0, 8).Reverse().Select(back =>
        {
            var start = thisMonday.AddDays(-7 * back);
            var end = start.AddDays(7);
            double km = activities.Where(a => a.StartedAt >= start && a.StartedAt < end).Sum(a => a.DistanceM) / 1000;

            return new Dictionary<string, object>
            {
                ["week"] = start.ToString("d MMM"),
                ["distance"] = Math.Round(km, 1)
            };
        }).ToList();

        view.Box(["h-60 w-full"], content: view =>
        {
            view.BarChart(["w-full h-full"],
                data: weeks,
                keys: ["distance"],
                indexBy: "week",
                padding: 0.5,
                margin: new ChartMargin { Top = 8, Right = 8, Bottom = 26, Left = 38 },
                axisLeft: new AxisConfig { TickCount = 4 },
                enableGridY: true,
                enableLabel: false,
                colors: [Brand.Magenta],
                theme: ChartThemes.DefaultDark);
        });
    }
}
