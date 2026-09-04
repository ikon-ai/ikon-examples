namespace Ikon.App.Patterns.Patterns;

// Pattern: single-processor-channel-queue — see docs/patterns/single-processor-channel-queue.md.
// `app` is the App's primary-constructor handle in a real app; the reactives and the two throwing
// stubs below stand in for the caller's own state and command handler so the region body compiles.
internal sealed class SingleProcessorChannelQueue(IAppBase app) : IPatternDemo
{
    public string Slug => "single-processor-channel-queue";
    public string Title => "Single-processor channel queue";
    public string Category => "Data";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: serializes submitted commands through an unbounded channel drained by a single background processor, so work runs one at a time. See the source and docs/patterns/single-processor-channel-queue.md.");

    private enum TranscriptRole { User, System, Narrator }

    private readonly Reactive<string> _inputText = new("");
    private readonly Reactive<bool> _isProcessing = new(false);

    private void AddTranscript(TranscriptRole role, string speaker, string text) => throw new NotImplementedException();
    private Task HandleCommandAsync(string commandText, CancellationToken cancellationToken) => throw new NotImplementedException();

    #region docsnippet:pattern-single-processor-channel-queue
    private readonly System.Threading.Channels.Channel<string> _commandQueue = System.Threading.Channels.Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _appCts = new();
    private bool _commandProcessorStarted;

    public async Task Main()
    {
        app.OnStopping(async () =>
        {
            await _appCts.CancelAsync();
            _commandQueue.Writer.TryComplete();
            _appCts.Dispose();
        });
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
        catch (OperationCanceledException)
        {
            // Shutdown cancelled the reader loop — expected, nothing to surface
        }
    }
    #endregion
}
