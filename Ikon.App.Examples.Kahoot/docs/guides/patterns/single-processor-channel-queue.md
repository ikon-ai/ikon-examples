<!-- mined-from: QTribunal -->
# Single-Processor Channel Queue — Serialize User Inputs Into One Async Loop

A `Channel<T>` collects user-submitted commands; one background task `await foreach`s the channel reader and processes them one at a time. The submit handler is non-blocking — it writes to the channel and returns. A boolean flag ensures the processor task is started only on the first submit.

## When to use

Apps with a single shared LLM/AI pipeline that must not be invoked concurrently for the same session — text adventures, agent loops, command-driven games, sequential code-execution shells. Prevents two overlapping LLM calls from corrupting transcript order or doubling the bill, without locks or `SemaphoreSlim` ceremony.

## Snippet

```csharp
private readonly Channel<string> _commandQueue = Channel.CreateUnbounded<string>();
private readonly CancellationTokenSource _appCts = new();
private bool _commandProcessorStarted;

public async Task Main()
{
    app.StoppingAsync += async _ =>
    {
        await _appCts.CancelAsync();
        _commandQueue.Writer.TryComplete();
        _appCts.Dispose();
    };
    // ... UI setup
}

private void SubmitCommand(string? submitted)
{
    var text = (submitted ?? "").Trim();

    if (string.IsNullOrWhiteSpace(text) || _isProcessing.Value)
    {
        return;
    }

    _inputText.Value = "";
    _commandQueue.Writer.TryWrite(text);

    if (!_commandProcessorStarted)
    {
        _commandProcessorStarted = true;
        ProcessCommandQueueAsync().RunParallel();
    }
}

private async Task ProcessCommandQueueAsync()
{
    var ct = _appCts.Token;

    try
    {
        await foreach (var commandText in _commandQueue.Reader.ReadAllAsync(ct))
        {
            _isProcessing.Value = true;

            try
            {
                await HandleCommandAsync(commandText, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AddTranscript(TranscriptRole.System, "System", $"Error: {ex.Message}");
            }
            finally
            {
                _isProcessing.Value = false;
            }
        }
    }
    catch (OperationCanceledException) { }
}
```

## Notes

- `Channel.CreateUnbounded<T>()` never blocks the writer — pair with a `_isProcessing` reactive flag to disable the Send button (or just ignore re-entries) so the user doesn't queue 50 commands in a panic.
- Hook teardown into `app.StoppingAsync`: cancel the CTS, complete the writer, dispose. The `await foreach` exits cleanly.
- Lazy-start (`!_commandProcessorStarted`) avoids a Main-time fire-and-forget that races with reactive scope setup.
- Wrap individual command handling in its own try/catch so one bad LLM response doesn't tear down the loop. Re-throw `OperationCanceledException` so the outer `await foreach` shuts down cleanly.

## See also

- `background-processing-pipeline` — when each item itself spawns a multi-stage detached pipeline
- `chatbot-streaming` — for streaming responses where the channel feeds tokens, not commands
