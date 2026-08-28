public partial class MomentumApp
{
    private void RenderActivity(UIView view)
    {
        if (ReactiveScope.UserIdOrNull is not { Length: > 0 } userId
            || _openActivityId.Value is not { } activityId
            || ActivitiesFor(userId).FirstOrDefault(a => a.Id == activityId) is not { } activity)
        {
            RenderFeed(view);
            return;
        }

        var points = _pointsByActivity.TryGetValue(activityId, out var cached) ? cached : [];
        var highlights = _highlightsByActivity.TryGetValue(activityId, out var found) ? found : [];
        var focused = highlights.FirstOrDefault(h => h.Id == _focusedHighlightId.Value);

        view.Column(["gap-8"], content: view =>
        {
            view.Button(["bg-transparent inline-flex items-center gap-2 px-0 text-muted-foreground hover:text-[#f7f7f7] font-mono text-[10px] uppercase tracking-[0.2em]"],
                text: "The log",
                icon: "arrow-left",
                onClick: () => GoTo(MomentumSection.Feed));

            view.Row(["items-end justify-between flex-wrap gap-6"], content: view =>
            {
                view.Column(["gap-2"], content: view =>
                {
                    view.Row(["items-center gap-3 flex-wrap"], content: view =>
                    {
                        KindChip(view, activity.Kind, activity.KindLabel);
                        view.Text(LocalWhen(activity.StartedAt), [Brand.Kicker]);
                    });

                    view.Heading(activity.Title, [Brand.Title, "text-[34px] md:text-[52px]"]);

                    if (activity.Story.Length > 0)
                    {
                        view.Text(activity.Story, ["text-[14.5px] text-muted-foreground leading-relaxed max-w-[58ch] mt-1"]);
                    }
                });

                view.Column(["items-end gap-1 pb-1"], content: view =>
                {
                    view.Text($"{activity.MomentumScore:0}", [Brand.Hero, "text-[54px]"]);
                    Kicker(view, "momentum score");
                });
            });

            view.Grid(["grid-cols-2 md:grid-cols-4 gap-x-7"], content: view =>
            {
                Tile(view, "Distance", $"{activity.DistanceM / 1000:0.00}", "km");
                Tile(view, "Moving time", Momentum.FormatDuration(activity.MovingSeconds), "");
                Tile(view, "Average", Momentum.FormatRate(activity.Kind, activity.AvgSpeedMps), "");
                Tile(view, "Top speed", $"{activity.MaxSpeedMps * 3.6:0.0}", "km/h");
                Tile(view, "Climbed", $"{activity.AscentM:0}", "m");
                Tile(view, "Descended", $"{activity.DescentM:0}", "m");
                Tile(view, "Elapsed", Momentum.FormatDuration(activity.ElapsedSeconds), "");
                Tile(view, "Stopped", Momentum.FormatDuration(activity.ElapsedSeconds - activity.MovingSeconds), "");
            });

            RenderActivityMap(view, points, focused);

            if (highlights.Count > 0)
            {
                view.Column(["gap-3"], content: view =>
                {
                    Kicker(view, focused != null ? "Highlights · tap again to clear" : "Highlights · tap one to find it on the map");

                    view.Grid(["grid-cols-1 md:grid-cols-2 gap-2.5"], content: view =>
                    {
                        foreach (var highlight in highlights)
                        {
                            HighlightCard(view, highlight, selectable: true);
                        }
                    });
                });
            }

            if (points.Count < 3)
            {
                return;
            }

            view.Grid(["grid-cols-1 lg:grid-cols-2 gap-8"], content: view =>
            {
                DetailChart(view, "Speed", "km/h", points, p => p.SpeedMps * 3.6, Brand.Magenta, focused);
                DetailChart(view, "Elevation", "m", points, p => p.ElevationM, Brand.Teal, focused, area: true);
            });

            RenderSplits(view, activity, points);
        });
    }

    private void RenderActivityMap(UIView view, IReadOnlyList<TrackPoint> points, Highlight? focused)
    {
        string[] frameStyle = ["h-[340px] md:h-[420px] w-full rounded-lg overflow-hidden ring-1 ring-white/[0.06] bg-[#161618]"];

        if (points.Count < 2)
        {
            view.Row([.. frameStyle, "items-center justify-center"], content: view =>
            {
                view.Spinner(["text-muted-foreground"], size: SpinnerSize.Sm);
            });

            return;
        }

        var track = points.Select(p => p.Point).ToList();
        var markers = new List<MapMarker>
        {
            new() { Id = "start", Lat = track[0].Lat, Lon = track[0].Lon, Type = "start" },
            new() { Id = "end", Lat = track[^1].Lat, Lon = track[^1].Lon, Type = "end" },
        };

        // Selecting a highlight paints its own span in the reward yellow over the magenta line, which
        // is how the card and the map are tied together without a second view.
        var emphasis = focused != null
            ? points.Where(p => p.Seconds >= focused.StartSeconds && p.Seconds <= focused.EndSeconds).Select(p => p.Point).ToList()
            : null;

        if (emphasis is { Count: > 0 })
        {
            markers.Add(new MapMarker
            {
                Id = "highlight",
                Lat = emphasis[0].Lat,
                Lon = emphasis[0].Lon,
                Type = "highlight",
                Label = focused!.Title,
            });
        }

        view.MomentumMap(track, markers, emphasis: emphasis, style: frameStyle);
    }

    private static void DetailChart(UIView view, string label, string unit, IReadOnlyList<TrackPoint> points,
        Func<TrackPoint, double> pick, string color, Highlight? focused, bool area = false)
    {
        // A long outing is thousands of points and the chart reads the same at a few hundred.
        int stride = Math.Max(1, points.Count / 260);
        var sampled = points.Where((_, i) => i % stride == 0).ToList();

        var series = new List<LineChartSeries>
        {
            new()
            {
                Id = label,
                Color = color,
                Data = sampled.Select(p => new LineChartPoint { X = Math.Round(p.Seconds / 60.0, 2), Y = Math.Round(pick(p), 1) })
            }
        };

        if (focused != null)
        {
            var span = sampled.Where(p => p.Seconds >= focused.StartSeconds && p.Seconds <= focused.EndSeconds).ToList();

            if (span.Count > 1)
            {
                series.Add(new LineChartSeries
                {
                    Id = focused.Title,
                    Color = Brand.Gold,
                    Data = span.Select(p => new LineChartPoint { X = Math.Round(p.Seconds / 60.0, 2), Y = Math.Round(pick(p), 1) })
                });
            }
        }

        view.Column(["gap-2.5"], content: view =>
        {
            Kicker(view, $"{label} · {unit} over minutes");
            view.Box(["h-44 w-full"], content: view =>
            {
                view.LineChart(["w-full h-full"],
                    data: series,
                    xScaleType: ScaleType.Linear,
                    yScaleMin: label == "Elevation" ? null : 0,
                    margin: new ChartMargin { Top = 8, Right = 8, Bottom = 26, Left = 42 },
                    axisBottom: new AxisConfig { TickCount = 6 },
                    axisLeft: new AxisConfig { TickCount = 4 },
                    enableGridX: false,
                    enableGridY: true,
                    enablePoints: false,
                    enableArea: area,
                    areaOpacity: 0.10,
                    curve: LineCurve.MonotoneX,
                    lineWidth: 1.5,
                    enableCrosshair: true,
                    useMesh: true,
                    colors: series.Count > 1 ? [color, Brand.Gold] : [color],
                    theme: ChartThemes.DefaultDark);
            });
        });
    }

    /// <summary>Kilometre splits, with the quickest one marked. The bar is the split against the best one.</summary>
    private static void RenderSplits(UIView view, Activity activity, IReadOnlyList<TrackPoint> points)
    {
        var splits = new List<(int Km, double Seconds, double Speed)>();
        double nextMark = 1000;
        double markStartSeconds = points[0].Seconds;

        foreach (var point in points)
        {
            if (point.DistanceM < nextMark)
            {
                continue;
            }

            double seconds = point.Seconds - markStartSeconds;

            if (seconds > 0)
            {
                splits.Add(((int)(nextMark / 1000), seconds, 1000 / seconds));
            }

            markStartSeconds = point.Seconds;
            nextMark += 1000;
        }

        if (splits.Count < 2)
        {
            return;
        }

        double fastest = splits.Max(s => s.Speed);
        int fastestKm = splits.First(s => Math.Abs(s.Speed - fastest) < 1e-9).Km;

        view.Column(["gap-3"], content: view =>
        {
            Kicker(view, "Splits · every kilometre");

            view.Column(["gap-1"], content: view =>
            {
                foreach (var split in splits)
                {
                    bool best = split.Km == fastestKm;
                    double width = Math.Clamp(split.Speed / fastest * 100, 8, 100);

                    view.Row(["items-center gap-3 h-7"], key: $"split-{split.Km}", content: view =>
                    {
                        view.Text($"{split.Km}", ["font-mono text-[10px] text-muted-foreground w-6 shrink-0 text-right tracking-[0.08em]"]);

                        view.Box(["flex-1 h-1.5 rounded-[1px] bg-[#242427] overflow-hidden"], content: view =>
                        {
                            view.Box([$"h-full rounded-[1px] w-[{width:0}%]", best ? "bg-[#f2da00]" : "bg-[#db176e]"]);
                        });

                        view.Text(Momentum.FormatRate(activity.Kind, split.Speed),
                            [Brand.Mono, "text-[11.5px] w-[86px] shrink-0 text-right", best ? "text-[#f2da00]" : ""]);
                    });
                }
            });
        });
    }
}
