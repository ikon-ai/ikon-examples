# Costs and Credits

## Costs and Credits

`app.Costs` is a `CostsService`, reporting what the app's AI usage cost in platform credits — per day and per usage event
name — so an app can show a spend panel, enforce its own budget, or attribute cost to whatever it
cares about.

```csharp
public async Task<double> CreditsThisMonthAsync(CancellationToken ct)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

    return await app.Costs.GetTotalCreditsAsync(firstOfMonth, today, ct);
}
```

`GetDailyCostsAsync(query)` is the detailed form. A `CostQuery` takes an inclusive `StartDate` and
`EndDate` (UTC, and it throws `ArgumentException` if the start is after the end), optional `Category`
and `EventName` narrowing, `Scopes`, and a `GroupByScopeType`. It returns one `DailyCost` per day and
event name — `Date`, `Category`, `EventName`, `TotalUsage`, `Credits`, and `RawCostEur` — with days
that had no usage producing no rows at all, ordered by date then event name.

```csharp
public async Task<IReadOnlyList<DailyCost>> ImageCostsAsync(DateOnly from, DateOnly to, CancellationToken ct)
{
    var query = new CostQuery(from, to, Category: "image-generation");

    return await app.Costs.GetDailyCostsAsync(query, ct);
}
```

### Attribution

Scopes are the app's own attribution, and they are the same scopes the logger takes: whatever you
push with `Log.Instance.UseScope` around a piece of work is stamped on every usage that work emits,
and can be filtered and grouped on afterwards. A `CostScopeFilter` is a `Type` plus an optional
`Value`; a null value matches any id of that type, and several filters are ANDed — usage must carry
all of them. `GetCreditsForScopeAsync(type, id, from, to)` is the direct "what did this one thing
cost" question.

Two things to respect before showing a number as final. Cost data is aggregated in the analytics
pipeline, so **very recent usage takes a short while to appear** — and an operation that emitted no
priced usage sums to zero, which is indistinguishable from one whose usage has not landed yet. And
the date range still has to cover when the work actually ran: usage is stored by day, and a query is
only as cheap as the range it scans.
