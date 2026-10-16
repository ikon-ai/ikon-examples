namespace Ikon.App.Patterns.Patterns;

// Pattern: live-captions-continuous-stt — see docs/patterns/live-captions-continuous-stt.md.
// The example region below is the canonical body the doc extracts.
internal sealed class LiveCaptionsContinuousStt : IPatternDemo
{
    public string Slug => "live-captions-continuous-stt";
    public string Title => "Live captions (continuous STT)";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio => throw new NotImplementedException();

    #region example:pattern-live-captions-continuous-stt
    private readonly ReactiveList<string> _captions = new();
    private readonly Reactive<string> _liveLine = new("");

    private readonly ConcurrentDictionary<string, (int SampleRate, int ChannelCount)> _formats = new();
    private readonly ConcurrentDictionary<string, System.Threading.Channels.Channel<float[]>> _segments = new();

    // Wire once at setup. Each mic-open segment (IsFirst .. IsLast) is one recognition: the frame
    // handler only queues samples, and a task feeds them to the recognizer as they arrive.
    private void WireCaptions()
    {
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            _formats[args.StreamId] = (args.SampleRate, args.ChannelCount);
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (args.IsFirst && _formats.TryGetValue(args.StreamId, out var format))
            {
                var segment = System.Threading.Channels.Channel.CreateUnbounded<float[]>();
                _segments[args.StreamId] = segment;
                _ = Task.Run(() => CaptionAsync(segment.Reader, format.SampleRate, format.ChannelCount));
            }

            if (_segments.TryGetValue(args.StreamId, out var open))
            {
                open.Writer.TryWrite(args.Samples);

                if (args.IsLast && _segments.TryRemove(args.StreamId, out _))
                {
                    open.Writer.TryComplete();
                }
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _formats.TryRemove(args.StreamId, out _);

            if (_segments.TryRemove(args.StreamId, out var open))
            {
                open.Writer.TryComplete();
            }
        };
    }

    private async Task CaptionAsync(System.Threading.Channels.ChannelReader<float[]> samples, int sampleRate, int channelCount)
    {
        using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.DeepgramNova3General);

        var config = new RecognizeContinuousSpeechConfig
        {
            Language = "en",
            SampleRate = sampleRate,
            ChannelCount = channelCount,
            InterimResults = true,
            // Deepgram ends a turn after 10 ms of silence unless told otherwise, so a turn ends at
            // every comma. Ask for a pause as long as the end of a sentence.
            EndOfTurnSilence = TimeSpan.FromMilliseconds(800),
            // Someone who keeps talking can go minutes without a pause that long. This ends the turn after
            // 1.5 s without new words, whatever the words suggest (Deepgram takes 1 to 10 s).
            MaxTurnSilence = TimeSpan.FromMilliseconds(1500),
        };

        var sentence = new StringBuilder();

        try
        {
            await foreach (var transcriptEvent in recognizer.RecognizeContinuousSpeechAsync(config, samples.ReadAllAsync()))
            {
                if (!transcriptEvent.IsFinal)
                {
                    _liveLine.Value = $"{sentence} {transcriptEvent.Text}".Trim();
                    continue;
                }

                sentence.Append(' ').Append(transcriptEvent.Text);

                // The end of a turn can still take a long time, so a finished sentence is also a caption.
                var caption = sentence.ToString().Trim();
                bool endsSentence = caption.Length > 0 && ".?!".Contains(caption[^1]);

                if (caption.Length > 0 && (transcriptEvent.IsEndOfTurn || endsSentence))
                {
                    _captions.Add(caption);
                    sentence.Clear();
                }

                _liveLine.Value = sentence.ToString().Trim();
            }
        }
        catch (AIException ex)
        {
            Log.Instance.Warning($"Live captions stopped: {ex.Message}");
        }

        // A stream that ends with the microphone has already sent its last words as a final. One
        // that failed leaves them only in the live line, so that is kept too.
        if (_liveLine.Value is { Length: > 0 } rest)
        {
            _captions.Add(rest);
        }

        _liveLine.Value = "";
    }

    private void Render(IView view)
    {
        view.Column(["gap-3 max-w-2xl"], content: col =>
        {
            col.MicToggleButton();

            if (_captions.Count == 0 && _liveLine.Value.Length == 0)
            {
                col.Text([Text.Caption], text: "Tap the microphone and start talking. Captions appear here.");
            }

            foreach (var caption in _captions.TakeLast(6))
            {
                col.Text([Text.Body], text: caption);
            }

            if (_liveLine.Value.Length > 0)
            {
                col.Text([Text.Body, "text-muted-foreground italic"], text: _liveLine.Value);
            }
        });
    }
    #endregion
}
