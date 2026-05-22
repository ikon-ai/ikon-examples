# Charts & Data Visualization

## Charts & Data Visualization

Ikon.Parallax ships first-class, themed, interactive chart components: `view.PieChart`, `view.BarChart`, and `view.LineChart`. **Use these — do NOT hand-roll charts** with `view.Box` divs, `conic-gradient`, inline `width: %` styles, or raw SVG. Like every component, the style array is the first positional argument; the data is a named argument.

### Pie / donut chart

`data` is a list of `PieChartDatum { string Id; string Label; double Value; string Color }`. Give `innerRadius` a value above 0 for a donut.

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

`data` is a list of `LineChartSeries { string Id; IEnumerable<LineChartPoint> Data; string Color }`; each point is `LineChartPoint { object X; object Y }`.

```csharp
view.LineChart(
    ["h-72 w-full"],
    data: [new LineChartSeries
    {
        Id = "Daily", Color = "#34d399",
        Data = days.Select(d => new LineChartPoint { X = d.Label, Y = d.Amount })
    }]);
```

All three also accept `colorScheme:` (a `ChartColorScheme` enum value like `ChartColorScheme.Nivo` — NOT a string like `"nivo"`, which is CS1503), `margin:` (`ChartMargin`), `axisBottom:` / `axisLeft:` (`AxisConfig`), `legends:`, `isInteractive:`, and an `onClick:` handler. See the UI API Reference for the full parameter list.

For dark backgrounds, pass the ready-made preset `theme: ChartThemes.DefaultDark` (charts inherit sensible defaults if you omit `theme:`). Do NOT hand-construct `new ChartTheme { TextColor = ..., GridColor = ..., TooltipBackground = ... }` — `ChartTheme` has no such flat color properties (it nests `Axis`, `Grid`, `Tooltip`, ... style objects), so invented names are CS0117. Use the preset, or omit `theme:` entirely.
