public partial class MomentumApp
{
    private void RenderFeed(UIView view)
    {
        if (ReactiveScope.UserIdOrNull is not { Length: > 0 } userId)
        {
            SectionTitle(view, "Feed", "Sign in to keep a log.");
            return;
        }

        var activities = ActivitiesFor(userId);

        view.Column(["gap-7"], content: view =>
        {
            view.Row(["items-end justify-between flex-wrap gap-6"], content: view =>
            {
                SectionTitle(view, "Everything you have done", "The log");

                view.Row(["gap-8 pb-7"], content: view =>
                {
                    Tile(view, "Outings", $"{activities.Count}", "", "border-t-0 py-0");
                    Tile(view, "Distance", $"{activities.Sum(a => a.DistanceM) / 1000:0.0}", "km", "border-t-0 py-0");
                    Tile(view, "Climbed", $"{activities.Sum(a => a.AscentM):0}", "m", "border-t-0 py-0");
                });
            });

            if (activities.Count == 0)
            {
                if (_feedLoading.Value)
                {
                    view.Row(["items-center gap-3 py-10"], content: view =>
                    {
                        view.Spinner(["text-muted-foreground"], size: SpinnerSize.Sm);
                        Kicker(view, "Opening the log");
                    });
                }
                else
                {
                    view.EmptyState("Nothing here yet", description: "Start an outing from the Move screen", icon: "route");
                }

                return;
            }

            view.Column(["border-t border-border"], content: view =>
            {
                foreach (var activity in activities)
                {
                    RenderFeedRow(view, activity);
                }
            });
        });
    }

    private void RenderFeedRow(UIView view, Activity activity)
    {
        view.Box(["grid grid-cols-[88px_1fr] md:grid-cols-[150px_1fr_auto] gap-4 md:gap-7 items-center py-4 border-b border-border cursor-pointer transition-colors duration-150 hover:bg-card"],
            key: activity.Id,
            onClick: async () => await OpenActivityAsync(activity.Id),
            ariaLabel: $"Open {activity.Title}",
            content: view =>
            {
                view.Box(["h-16 md:h-20 w-full"], content: view =>
                {
                    if (_pointsByActivity.TryGetValue(activity.Id, out var points) && points.Count > 1)
                    {
                        view.RouteTrace(points.Select(p => p.Point).ToList(), ["h-full w-full"], lineWidth: 1.5);
                    }
                    else
                    {
                        // The points are a second query and the feed must paint without waiting on it.
                        view.Box(["h-full w-full rounded-sm bg-[#161618]"]);
                    }
                });

                view.Column(["gap-1.5 min-w-0"], content: view =>
                {
                    view.Row(["items-center gap-3 flex-wrap"], content: view =>
                    {
                        KindChip(view, activity.Kind, activity.KindLabel);
                        view.Text(LocalWhen(activity.StartedAt), [Brand.Kicker]);
                    });

                    view.Text(activity.Title, ["font-heading font-semibold text-[19px] md:text-[23px] text-[#f7f7f7] tracking-[-0.02em] truncate"]);

                    view.Row(["gap-4 flex-wrap"], content: view =>
                    {
                        Stat(view, $"{activity.DistanceM / 1000:0.00}", "km");
                        Stat(view, Momentum.FormatDuration(activity.MovingSeconds), "");
                        Stat(view, $"{activity.AscentM:0}", "m up");
                        Stat(view, Momentum.FormatRate(activity.Kind, activity.AvgSpeedMps), "");
                    });
                });

                view.Column(["hidden md:flex items-end gap-1 shrink-0"], content: view =>
                {
                    view.Text($"{activity.MomentumScore:0}", [Brand.Numeral, "text-[26px]"]);
                    Kicker(view, "momentum");
                });
            });
    }

    private static void Stat(UIView view, string value, string unit)
    {
        view.Row(["items-baseline gap-1"], content: view =>
        {
            view.Text(value, [Brand.Mono, "text-[12.5px]"]);

            if (unit.Length > 0)
            {
                view.Text(unit, ["font-mono text-[10px] text-muted-foreground"]);
            }
        });
    }

    private async Task OpenActivityAsync(string activityId)
    {
        // Warm both caches before the screen swaps, so the detail view never renders half-drawn.
        await PointsForAsync(activityId);
        await HighlightsForAsync(activityId);
        _focusedHighlightId.Value = null;
        _openActivityId.Value = activityId;
        _section.Value = MomentumSection.Activity;
    }
}
