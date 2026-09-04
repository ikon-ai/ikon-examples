<!-- mined-from: Ikon.App.Patterns -->
# Shaping What A Tool Tells The Model

A tool can return its value alone, or a `FunctionResult` that **frames** it. The prefix and suffix
are written into the model's transcript around the result, which is where you put what the data
cannot say about itself: that it is truncated, stale, in a particular unit, or empty for a reason.

An empty array reads to a model as *"no such thing exists"*. Saying **why** it is empty is the
difference between the model reporting a fact and inventing one.

## When to use

Any tool whose result needs context to be read correctly — a search that truncated, a lookup that
found nothing, a number with a unit, a list ordered in a way that matters.

## Notes

- `modelMessagePrefix` and `modelMessageSuffix` are for the **model**, not the user. They shape the
  next turn's reasoning; they are not UI copy.
- **`FunctionMediaResult` hands the model an image alongside text.** Only providers that support
  media in tool results inline it — everything else falls back to `ToString`, which *summarizes*
  the media rather than emitting the bytes. So it degrades instead of flooding a transcript with
  base64.
- **`Emerge.EndRun` returned from a tool body ends the run** after the current tool batch instead
  of looping back to the model. `EndRun<TValue>` also becomes the run's result when the value is
  assignable to `T`; `EndRun()` with no value completes with `default(T)`. Use it for a tool that
  *is* the answer rather than one feeding it.
- `FunctionResultPart` is what a completed call looks like in the transcript — `FunctionCall`,
  the `Events` it produced, and the `Result`. `KernelContextExtensions.GetFunctionResults` reads
  them back, which is how a later turn inspects what earlier tools returned.
- Truncate in the tool, not in the prompt. A model given 500 rows and told to consider 20 will use
  all 500 of them as context anyway, and pay for them.

## Snippet

```csharp
/// <summary>
/// A tool can return the value alone, or a FunctionResult that FRAMES it. The prefix and
/// suffix are written into the model's transcript around the result, which is where you put
/// what the data cannot say about itself -- that it is truncated, stale, or empty for a
/// reason.
/// </summary>
[Function("Look up a customer's invoices")]
public static async Task<FunctionResult> FindInvoices(string customer)
{
    var invoices = await LookupAsync(customer);

    if (invoices.Length == 0)
    {
        // An empty array reads to the model as "no invoices exist". Saying WHY it is empty is
        // the difference between the model reporting a fact and inventing one.
        return new FunctionResult(invoices,
            modelMessagePrefix: $"No invoices found for '{customer}'. The name may be spelled differently.");
    }

    return new FunctionResult(
        invoices.Take(20).ToArray(),
        modelMessagePrefix: invoices.Length > 20
            ? $"Showing the 20 most recent of {invoices.Length} invoices."
            : null,
        modelMessageSuffix: "Amounts are in EUR.");
}

/// <summary>
/// FunctionMediaResult hands the model an image alongside text. Only providers that support
/// media in tool results inline it; everything else falls back to ToString, which SUMMARIZES
/// the media rather than emitting the bytes -- so this degrades instead of flooding a
/// transcript with base64.
/// </summary>
[Function("Chart this month's revenue")]
public static async Task<FunctionMediaResult> ChartRevenue()
{
    var png = await RenderChartAsync();
    return new FunctionMediaResult("Revenue by week, this month.", new BinaryDataContainer(png, "image/png"));
}

/// <summary>
/// Returning Emerge.EndRun from a tool body ends the run after the current tool batch instead
/// of looping back to the model -- for a tool that IS the answer, rather than one feeding it.
/// </summary>
[Function("Cancel the operation")]
public static EndRun<string> Cancel() => Emerge.EndRun("Cancelled at the user's request.");
```

## See also

- `orchestrator-thread-with-tools` — wiring tools to a model in the first place.
- `run-trace-and-cost` — reading the tool-call history back off a completed run.
