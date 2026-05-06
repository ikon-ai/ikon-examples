<!-- mined-from: RailGo -->
# Seat Grid Picker — Row/Column Buttons With State Colors + Aisle Gap

A 2D grid of seat buttons (`size-8` rounded squares) where each cell shows a state — Available, Selected, Shared, Occupied — via a `switch` on `state => stateStyle`. The aisle is rendered as an extra empty `Box` between columns. Disabled seats use `cursor-not-allowed` + `line-through`. The whole picker is a function of `(service, wagon, date, segment)` — no manual diffing.

## When to use

Booking flows (trains, planes, theaters, restaurants), conference room pickers, anywhere users tap one cell out of a fixed grid. Pair with a confirmation panel beneath that activates only when one cell is selected.

## Snippet

```csharp
public enum SeatState { Available, Selected, Shared, Occupied }

private void RenderWagonGrid(UIView view, Service service, Wagon wagon, DateOnly date, int fromOrder, int toOrder)
{
    var aisleCol = Math.Max(0, (wagon.Columns.Length / 2) - 1);

    view.Column([
        "w-full max-w-[480px] rounded-2xl border-2 border-[#ecc57c]/35 overflow-hidden",
        "bg-[#faf6ef] shadow-sm"
    ], content: carriage =>
    {
        carriage.Column([$"px-3 sm:px-4 py-3 gap-1"], content: body =>
        {
            for (var r = 1; r <= wagon.Rows; r++)
            {
                var row = r;
                body.Row(["items-center gap-1 sm:gap-1.5 justify-center"], content: rowView =>
                {
                    rowView.Text(["w-4 sm:w-5 text-[9px] tabular-nums text-[#5a5958]/70 text-right shrink-0"],
                        row.ToString(CultureInfo.InvariantCulture));

                    for (var c = 0; c < wagon.Columns.Length; c++)
                    {
                        var col = wagon.Columns[c];
                        var code = $"{wagon.Id}-{row}{col}";
                        var state = GetSeatState(service.Id, date, code, fromOrder, toOrder);
                        RenderSeat(rowView, state, col, code);

                        if (c == aisleCol && c < wagon.Columns.Length - 1)
                            rowView.Box(["w-3 sm:w-5 shrink-0"]);
                    }
                });
            }
        });
    });
}

private void RenderSeat(UIView view, SeatState state, string label, string seatCode)
{
    var stateStyle = state switch
    {
        SeatState.Selected  => "bg-[#ecc57c] text-[#22304a] border-[#ecc57c] shadow-sm",
        SeatState.Available => "bg-white text-[#1a1e26] border-[#ecc57c]/35 hover:border-[#22304a] cursor-pointer",
        SeatState.Shared    => "bg-[#ecc57c]/15 text-[#22304a] border-[#ecc57c]/60 hover:border-[#ecc57c] cursor-pointer",
        SeatState.Occupied  => "bg-[#f2ead9] text-[#5a5958]/60 border-[#ecc57c]/35 cursor-not-allowed line-through",
        _ => "",
    };
    var disabled = state == SeatState.Occupied;
    var seatCodeCopy = seatCode;

    view.Button(
        ["size-8 sm:size-9 shrink-0 rounded-md text-[10px] font-medium tabular-nums flex items-center justify-center transition-all border", stateStyle],
        onClick: async () => { if (!disabled) await SelectSeatAsync(seatCodeCopy); },
        disabled: disabled,
        content: b => b.Text([], label));
}
```

## Notes

- Capture loop variables (`var seatCodeCopy = seatCode;`) before the async lambda — otherwise every button calls the handler with the last row's value.
- Aisle is just an empty `Box` with width — easier than a CSS grid template.
- Seat code format `"{wagonId}-{row}{col}"` (e.g. `W3-12A`) round-trips cleanly through reactive state.
- A legend below the grid helps users decode colors — render with the same `stateStyle` switch for consistency.
- Disabled seats need both `disabled` AND a guard inside `onClick` — defense in depth.

## See also

- `multi-step-wizard` — seat picker is usually step 3 of a wizard
- `state-machine-cards-and-transitions` — different shape: cards transitioning between phases
