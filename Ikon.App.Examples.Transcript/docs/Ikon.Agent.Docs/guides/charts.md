# Charts & Data Visualization

## Charts & Data Visualization

Ikon.Parallax ships first-class, themed, interactive chart components: `view.PieChart`, `view.BarChart`, and `view.LineChart`. **Use these — do NOT hand-roll charts** with `view.Box` divs, `conic-gradient`, inline `width: %` styles, or raw SVG. Like every component, the style array is the first positional argument; the data is a named argument.

### Pie / donut chart

`data` is a list of `PieChartDatum { string Id; string Label; double Value; string Color }`. A datum's `Color` is honored as its slice color; datums without one fall back to the theme palette. Passing `colors:` overrides per-datum colors. Give `innerRadius` a value above 0 for a donut.

```csharp
view.PieChart(
    ["h-72 w-72"],
    data: categories.Select(c => new PieChartDatum
    {
        Id = c.Name, Label = c.Name, Value = c.Total, Color = c.Hex
    }),
    innerRadius: 0.5);
```

### Bar chart

`data` is a list of rows, each a `Dictionary<string, object>` holding the `indexBy` key plus one entry per series named in `keys`.

```csharp
view.BarChart(
    ["h-72 w-full"],
    data: categories.Select(c => new Dictionary<string, object>
    {
        ["category"] = c.Name,
        ["spend"] = c.Total
    }),
    keys: ["spend"],
    indexBy: "category");
```

### Line chart

`data` is a list of `LineChartSeries { string Id; IEnumerable<LineChartPoint> Data; string Color }`; each point is `LineChartPoint { object X; double Y }`. A series' `Color` is honored as its line color; series without one fall back to the theme palette. Passing `colors:` overrides per-series colors for the whole chart.

```csharp
view.LineChart(
    ["h-72 w-full"],
    data: [new LineChartSeries
    {
        Id = "Daily", Color = "#34d399",
        Data = days.Select(d => new LineChartPoint { X = d.Label, Y = d.Amount })
    }]);
```

All three also accept `colorScheme:` (a `ChartColorScheme` enum value like `ChartColorScheme.Nivo` — NOT a string like `"nivo"`, which is CS1503), `margin:` (`ChartMargin`), `legends:`, `isInteractive:`, and an `onClick:` handler. BarChart and LineChart additionally take `axisBottom:` / `axisLeft:` (`AxisConfig`) — PieChart has no axis parameters (CS1739). See the UI API Reference for the full parameter list.

Out of the box, BarChart and LineChart render labelled bottom/left axes with a sensible default margin — you do NOT need to pass `margin:` or axis configs just to see tick labels. Pass `margin:` only to adjust spacing, and `axisBottom:` / `axisLeft:` to customize ticks, legends, or formatting; an empty `new AxisConfig()` keeps the default axis.

When the plotted values carry a unit, pass `valueUnit:` — tooltips and value-axis ticks then render human-scaled unit strings instead of bare numbers. Well-known units are `"milliseconds"`, `"seconds"`, `"bytes"`, `"percent"`, and `"usd"` (auto-scaled: `1333.9` milliseconds renders as `1.33 s`, `2411724` bytes as `2.3 MB`); any other string is appended as a plain suffix (`valueUnit: "credits"` → `12 credits`).

For dark backgrounds, pass the ready-made preset `theme: ChartThemes.DefaultDark` (charts inherit sensible defaults if you omit `theme:`). Do NOT hand-construct `new ChartTheme { TextColor = ..., GridColor = ..., TooltipBackground = ... }` — `ChartTheme` has no such flat color properties (it nests `Axis`, `Grid`, `Tooltip`, ... style objects), so invented names are CS0117. Use the preset, or omit `theme:` entirely.
