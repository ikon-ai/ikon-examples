using System.Runtime.CompilerServices;
using System.Text;
using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;

return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParams(string Name = "Ikon");

public sealed record PendingUpload(string FileName, string FilePath, string MimeType, long SizeBytes);

public sealed record TranscriptEntry(
    string Id,
    string FileName,
    string AudioAssetUri,
    string TranscriptAssetUri,
    string Language,
    double DurationSeconds,
    string Summary,
    IReadOnlyList<string> ActionItems,
    DateTimeOffset CreatedAt);

public sealed class TranscriptAnalysis
{
    public string Summary { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = [];
}

public sealed class TranscriptChunkSummary
{
    public string Summary { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = [];
}

[App]
public class Transcript(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());
    private SpeechRecognizer SpeechRecognizer { get; } = new(SpeechRecognizerModel.WhisperLarge3Turbo);

    private readonly Reactive<PendingUpload?> _pendingUpload = new(null);
    private readonly Reactive<IReadOnlyList<TranscriptEntry>> _transcripts = new([]);
    private readonly Reactive<string?> _activeTranscriptId = new(null);
    private readonly Reactive<string> _languageCode = new("");
    private readonly Reactive<string> _transcriptText = new("");
    private readonly Reactive<string> _summaryText = new("");
    private readonly Reactive<IReadOnlyList<string>> _actionItems = new([]);
    private readonly Reactive<string> _statusMessage = new("");
    private readonly Reactive<double> _uploadProgress = new(0);
    private readonly Reactive<double> _transcriptionProgress = new(0);
    private readonly Reactive<bool> _isTranscribing = new(false);
    private readonly Reactive<bool> _generateSummary = new(true);

    private readonly object _transcriptsLock = new();

    public async Task Main()
    {
        await LoadTranscriptHistoryAsync();

        app.StoppingAsync += async _ => SpeechRecognizer.Dispose();

        UI.Root([Page.Default], content: view =>
        {
            view.Column([Container.Xl4, "py-8 px-4"], content: view =>
            {
                view.Column([Layout.Column.Lg], content: view =>
                {
                    view.Text([Text.Display], "Transcript Studio");
                    view.Text([Text.Body, "text-muted-foreground"], "Upload audio or video files and get searchable transcripts with summaries");
                });

                view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
                {
                    view.Column(["flex-1"], content: view =>
                    {
                        view.Box([Card.Default, "p-6"], content: view =>
                        {
                            view.Text([Text.H2, "mb-2"], "New transcript");
                            view.Text([Text.Caption, "mb-4"], "Supports .ogg, .mp4, .wav, .mp3 and long recordings");

                            view.FileUpload(
                                [FileUpload.Zone.Base],
                                accept: ["audio/*", "video/*", ".ogg", ".mp4", ".m4a", ".wav", ".mp3"],
                                multiple: false,
                                onUploadStart: async _ =>
                                {
                                    _uploadProgress.Value = 0;
                                    _statusMessage.Value = "Uploading audio";
                                    return true;
                                },
                                onUploadProgress: async args =>
                                {
                                    _uploadProgress.Value = args.ProgressPercentage;
                                },
                                onUploadComplete: async args =>
                                {
                                    _pendingUpload.Value = new PendingUpload(args.FileName, args.LocalTempFilePath ?? "", args.MimeType, args.Size);
                                    _statusMessage.Value = $"Ready to transcribe {args.FileName}";
                                    _uploadProgress.Value = 100;
                                    _transcriptionProgress.Value = 0;
                                },
                                onUploadError: async args =>
                                {
                                    _statusMessage.Value = $"Upload failed: {args.ErrorMessage}";
                                },
                                content: view =>
                                {
                                    view.Column([Layout.Column.Center], content: view =>
                                    {
                                        view.Icon([Media.PlaceholderIcon], name: "upload");
                                        view.Text([Text.Body], "Click to upload or drag & drop");
                                        view.Text([Text.Caption], "Audio or video files");
                                    });
                                });

                            if (_uploadProgress.Value > 0 && _uploadProgress.Value < 100)
                            {
                                view.Progress(value: _uploadProgress.Value, max: 100, rootStyle: [Progress.Root, "mt-3"]);
                            }

                            if (_pendingUpload.Value != null)
                            {
                                view.Box(["mt-4"], content: view =>
                                {
                                    view.Text([Text.Body], _pendingUpload.Value.FileName);
                                    view.Text([Text.Caption, "text-muted-foreground"], FormatBytes(_pendingUpload.Value.SizeBytes));
                                });
                            }

                            view.Column([Layout.Column.Sm, "mt-4"], content: view =>
                            {
                                view.Text([FormField.Label], "Language code (optional)");
                                view.TextField(
                                    [Input.Default],
                                    placeholder: "auto or en-US",
                                    value: _languageCode.Value,
                                    onValueChange: async value => _languageCode.Value = value);
                            });

                            view.Row([Layout.Row.Sm, "mt-4 items-center"], content: view =>
                            {
                                view.Switch(
                                    [Switch.Default],
                                    @checked: _generateSummary.Value,
                                    onCheckedChange: async value => _generateSummary.Value = value,
                                    content: view => view.SwitchThumb([Switch.Thumb]));
                                view.Text([Text.Caption], "Generate summary and action items");
                            });

                            view.Row([Layout.Row.Md, "mt-4"], content: view =>
                            {
                                view.Button(
                                    [Button.PrimaryMd],
                                    label: _isTranscribing.Value ? "Transcribing..." : "Transcribe",
                                    disabled: _isTranscribing.Value || _pendingUpload.Value == null,
                                    onClick: async () => await StartTranscriptionAsync());

                                view.Button(
                                    [Button.OutlineMd],
                                    label: "Clear",
                                    disabled: _isTranscribing.Value,
                                    onClick: async () => ClearCurrentTranscript());
                            });

                            if (_statusMessage.Value.Length > 0)
                            {
                                view.Text([Text.Caption, "text-muted-foreground mt-3"], _statusMessage.Value);
                            }

                            if (_isTranscribing.Value)
                            {
                                view.Progress(value: _transcriptionProgress.Value, max: 100, rootStyle: [Progress.Root, "mt-3"]);
                            }
                        });

                        view.Box([Card.Default, "p-6 mt-6"], content: view =>
                        {
                            view.Text([Text.H2, "mb-2"], "Transcript output");

                            if (string.IsNullOrWhiteSpace(_transcriptText.Value))
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Transcripts will appear here once processed");
                            }
                            else
                            {
                                view.TextArea(
                                    [Input.Default, "min-h-[240px]"],
                                    value: _transcriptText.Value,
                                    placeholder: "Transcript output",
                                    onValueChange: null);
                            }
                        });

                        view.Box([Card.Default, "p-6 mt-6"], content: view =>
                        {
                            view.Text([Text.H2, "mb-2"], "Summary");

                            if (string.IsNullOrWhiteSpace(_summaryText.Value))
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Summaries will appear here when enabled");
                            }
                            else
                            {
                                view.Text([Text.Body], _summaryText.Value);
                            }

                            if (_actionItems.Value.Count > 0)
                            {
                                view.Text([Text.H3, "mt-4"], "Action items");
                                view.Column([Layout.Column.Sm, "mt-2"], content: view =>
                                {
                                    foreach (var item in _actionItems.Value)
                                    {
                                        view.Row([Layout.Row.Sm, "items-start"], content: view =>
                                        {
                                            view.Text([Text.Body, "text-brand-primary"], "•");
                                            view.Text([Text.Body], item);
                                        });
                                    }
                                });
                            }
                        });
                    });

                    view.Column(["w-full lg:w-[320px] shrink-0"], content: view =>
                    {
                        view.Box([Card.Default, "p-6"], content: view =>
                        {
                            view.Text([Text.H2, "mb-2"], "Saved transcripts");

                            if (_transcripts.Value.Count == 0)
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "No transcripts yet");
                            }
                            else
                            {
                                view.Column([Layout.Column.Md], content: view =>
                                {
                                    foreach (var entry in _transcripts.Value)
                                    {
                                        var isActive = entry.Id == _activeTranscriptId.Value;
                                        var cardStyle = isActive ? "border border-brand-primary/60 bg-brand-primary/10" : "border border-transparent";

                                        view.Box([Card.Default, "p-4", cardStyle], content: view =>
                                        {
                                            view.Text([Text.Body, "font-semibold"], entry.FileName);
                                            view.Text([Text.Caption, "text-muted-foreground"], entry.CreatedAt.ToLocalTime().ToString("g"));
                                            view.Text([Text.Caption, "text-muted-foreground"], FormatDuration(entry.DurationSeconds));

                                            if (!string.IsNullOrWhiteSpace(entry.Summary))
                                            {
                                                view.Text([Text.Caption, "mt-2 text-muted-foreground"], entry.Summary);
                                            }

                                            view.Button(
                                                [Button.OutlineSm, "mt-3"],
                                                label: "Load",
                                                onClick: async () => await LoadTranscriptAsync(entry));
                                        });
                                    }
                                });
                            }
                        });
                    });
                });
            });
        });
    }

    private void ClearCurrentTranscript()
    {
        _pendingUpload.Value = null;
        _activeTranscriptId.Value = null;
        _transcriptText.Value = string.Empty;
        _summaryText.Value = string.Empty;
        _actionItems.Value = [];
        _statusMessage.Value = string.Empty;
        _uploadProgress.Value = 0;
        _transcriptionProgress.Value = 0;
    }

    private async Task StartTranscriptionAsync()
    {

        if (_isTranscribing.Value)
        {
            return;
        }


        if (_pendingUpload.Value == null)
        {
            return;
        }

        var pending = _pendingUpload.Value;


        if (string.IsNullOrWhiteSpace(pending.FilePath) || !File.Exists(pending.FilePath))
        {
            _statusMessage.Value = "Uploaded file was not found";
            return;
        }

        var userId = ResolveUserId();


        if (string.IsNullOrWhiteSpace(userId))
        {
            _statusMessage.Value = "User id not available";
            return;
        }

        var spaceId = app.GlobalState.SpaceId;
        var channelId = app.GlobalState.ChannelId;


        if (string.IsNullOrWhiteSpace(channelId))
        {
            _statusMessage.Value = "Channel id not available";
            return;
        }

        _isTranscribing.Value = true;
        _statusMessage.Value = "Preparing audio";
        _transcriptionProgress.Value = 0;
        _transcriptText.Value = string.Empty;
        _summaryText.Value = string.Empty;
        _actionItems.Value = [];
        _activeTranscriptId.Value = null;

        var entryId = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(pending.FileName);


        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = MimeTypes.GetExtensionFromMimeType(pending.MimeType);
        }


        if (!string.IsNullOrWhiteSpace(extension) && !extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        var audioAssetUri = new AssetUri(
            AssetClass.CloudFile,
            $"transcripts/{entryId}/audio{extension}",
            spaceId: spaceId,
            userId: userId,
            channelId: channelId);

        var transcriptAssetUri = new AssetUri(
            AssetClass.CloudFile,
            $"transcripts/{entryId}/transcript.txt",
            spaceId: spaceId,
            userId: userId,
            channelId: channelId);

        string? rawAudioPath = null;

        try
        {
            await SaveAudioAssetAsync(audioAssetUri, pending.FilePath, pending.MimeType);

            rawAudioPath = await ConvertToRawAudioAsync(pending.FilePath);
            var transcriptBuilder = new StringBuilder();
            var language = _languageCode.Value.Trim();
            var sampleRate = 16000;
            var channelCount = 1;


            if (SpeechRecognizer.SupportsContinuousRecognition)
            {
                await foreach (var text in SpeechRecognizer.RecognizeContinuousSpeechAsync(
                                   new RecognizeContinuousSpeechConfig
                                   {
                                       SampleRate = sampleRate,
                                       ChannelCount = channelCount,
                                       Language = language
                                   },
                                   ReadAudioSamplesAsync(rawAudioPath, sampleRate, channelCount, CancellationToken.None)))
                {
                    AppendTranscript(transcriptBuilder, text);
                }
            }
            else
            {
                await foreach (var chunk in ReadAudioSamplesAsync(rawAudioPath, sampleRate, channelCount, CancellationToken.None))
                {
                    var chunkText = await SpeechRecognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
                    {
                        Samples = chunk,
                        SampleRate = sampleRate,
                        ChannelCount = channelCount,
                        Language = language,
                        Timeout = TimeSpan.FromMinutes(10)
                    });

                    AppendTranscript(transcriptBuilder, chunkText);
                }
            }

            var transcriptText = transcriptBuilder.ToString();
            _transcriptText.Value = transcriptText;
            _statusMessage.Value = "Transcription complete";
            await Asset.Instance.SetTextAsync(transcriptAssetUri, transcriptText, new AssetMetadata(mimeType: MimeTypes.TextPlain));

            var summary = string.Empty;
            var actionItems = new List<string>();


            if (_generateSummary.Value && !string.IsNullOrWhiteSpace(transcriptText))
            {
                _statusMessage.Value = "Generating summary";
                var analysis = await SummarizeTranscriptAsync(transcriptText, CancellationToken.None);
                summary = analysis.Summary;
                actionItems = analysis.ActionItems;
                _summaryText.Value = summary;
                _actionItems.Value = actionItems;
            }

            var durationSeconds = CalculateDurationSeconds(rawAudioPath, sampleRate, channelCount);
            var entry = new TranscriptEntry(
                entryId,
                pending.FileName,
                audioAssetUri.ToString(),
                transcriptAssetUri.ToString(),
                language,
                durationSeconds,
                summary,
                actionItems,
                DateTimeOffset.UtcNow);

            await SaveTranscriptEntryAsync(entry);
            _activeTranscriptId.Value = entry.Id;
        }
        catch (Exception ex)
        {
            _statusMessage.Value = $"Transcription failed: {ex.Message}";
            Log.Instance.Warning($"Transcription failed: {ex.Message}");
        }
        finally
        {

            if (!string.IsNullOrWhiteSpace(rawAudioPath) && File.Exists(rawAudioPath))
            {
                File.Delete(rawAudioPath);
            }

            _isTranscribing.Value = false;
            _transcriptionProgress.Value = 100;
        }
    }

    private void AppendTranscript(StringBuilder builder, string text)
    {

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }


        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        builder.Append(text.Trim());
        _transcriptText.Value = builder.ToString();
    }

    private async Task SaveAudioAssetAsync(AssetUri assetUri, string filePath, string mimeType)
    {
        await using var source = File.OpenRead(filePath);
        await using var destination = await Asset.Instance.GetWriteStreamAsync(assetUri, new AssetMetadata(mimeType: mimeType));
        await source.CopyToAsync(destination);
        await destination.FlushAsync();
    }

    private async Task<string> ConvertToRawAudioAsync(string inputPath)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"ikon-transcript-{Guid.NewGuid():N}.f32");
        var command = $"ffmpeg -loglevel warning -i \"{inputPath}\" -vn -f f32le -ac 1 -ar 16000 \"{outputPath}\"";

        var result = await ProcessRunner.RunAsync(command, ignoreErrors: true);


        if (!result.Success || result.ExitCode != 0)
        {
            var error = string.IsNullOrWhiteSpace(result.StdErr) ? "ffmpeg failed" : result.StdErr.Trim();
            throw new InvalidOperationException(error);
        }

        return outputPath;
    }

    private async IAsyncEnumerable<float[]> ReadAudioSamplesAsync(
        string filePath,
        int sampleRate,
        int channelCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var bufferSize = sampleRate * channelCount * 5;
        var byteBuffer = new byte[bufferSize * sizeof(float)];

        await using var stream = File.OpenRead(filePath);
        var totalBytes = stream.Length;
        long bytesReadTotal = 0;

        while (true)
        {
            var bytesRead = await stream.ReadAsync(byteBuffer.AsMemory(), cancellationToken);

            if (bytesRead <= 0)
            {
                break;
            }

            bytesReadTotal += bytesRead;
            var validBytes = bytesRead - (bytesRead % sizeof(float));

            if (validBytes <= 0)
            {
                continue;
            }

            var floatCount = validBytes / sizeof(float);
            var samples = new float[floatCount];
            Buffer.BlockCopy(byteBuffer, 0, samples, 0, validBytes);

            if (totalBytes > 0)
            {
                _transcriptionProgress.Value = Math.Min(100, bytesReadTotal / (double)totalBytes * 100);
            }

            yield return samples;
        }
    }

    private async Task<TranscriptAnalysis> SummarizeTranscriptAsync(string transcriptText, CancellationToken cancellationToken)
    {
        var ctx = new KernelContext();
        var chunks = SplitTranscript(transcriptText, 4000);
        TranscriptAnalysis? analysis = null;

        await foreach (var ev in Emerge.MapReduce<TranscriptChunkSummary, TranscriptAnalysis>(LLMModel.Claude45Sonnet, ctx, mr =>
                       {
                           mr.Input = chunks;
                           mr.MaxParallel = 3;

                           mr.Map(map =>
                           {
                               map.Command = $"""
                                   Summarize the following transcript chunk and extract any action items.
                                   Return JSON:
                                   {map.JsonSchema}
                                   """;
                           });

                           mr.Reduce(reduce =>
                           {
                               reduce.Command = $"""
                                   Combine the chunk summaries into a concise overall summary.
                                   Provide a clear list of actionable follow-ups based on the full transcript.
                                   Return JSON:
                                   {reduce.JsonSchema}
                                   """;
                           });
                       }).WithCancellation(cancellationToken))
        {

            if (ev is Completed<TranscriptAnalysis> completed)
            {
                analysis = completed.Result;
            }
        }

        return analysis ?? new TranscriptAnalysis();
    }

    private async Task LoadTranscriptHistoryAsync()
    {
        var userId = ResolveUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var indexUri = BuildTranscriptIndexUri(userId);
        var entries = await Asset.Instance.TryGetAsync<List<TranscriptEntry>>(indexUri);

        if (entries == null)
        {
            return;
        }

        lock (_transcriptsLock)
        {
            _transcripts.Value = entries;
        }
    }

    private async Task SaveTranscriptEntryAsync(TranscriptEntry entry)
    {
        List<TranscriptEntry> updated;

        lock (_transcriptsLock)
        {
            updated = new List<TranscriptEntry>(_transcripts.Value);
            updated.Insert(0, entry);
            _transcripts.Value = updated;
        }

        var userId = ResolveUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var indexUri = BuildTranscriptIndexUri(userId);
        await Asset.Instance.SetAsync(indexUri, updated, new AssetMetadata(mimeType: MimeTypes.ApplicationJson));
    }

    private async Task LoadTranscriptAsync(TranscriptEntry entry)
    {
        _activeTranscriptId.Value = entry.Id;
        _summaryText.Value = entry.Summary;
        _actionItems.Value = entry.ActionItems;
        _statusMessage.Value = "Loading transcript";


        if (!AssetUri.TryParse(entry.TranscriptAssetUri, out var transcriptUri))
        {
            _statusMessage.Value = "Transcript asset missing";
            return;
        }

        var text = await Asset.Instance.GetTextAsync(transcriptUri);
        _transcriptText.Value = text;
        _statusMessage.Value = "Transcript loaded";
    }

    private AssetUri BuildTranscriptIndexUri(string userId)
    {
        return new AssetUri(
            AssetClass.CloudJson,
            "transcripts/index.json",
            spaceId: app.GlobalState.SpaceId,
            userId: userId,
            channelId: app.GlobalState.ChannelId);
    }

    private static double CalculateDurationSeconds(string rawAudioPath, int sampleRate, int channelCount)
    {
        var fileInfo = new FileInfo(rawAudioPath);
        var totalSamples = fileInfo.Length / sizeof(float);


        if (sampleRate <= 0 || channelCount <= 0)
        {
            return 0;
        }

        return totalSamples / (double)(sampleRate * channelCount);
    }

    private static string FormatDuration(double seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));


        if (duration.TotalHours >= 1)
        {
            return duration.ToString("hh\\:mm\\:ss");
        }

        return duration.ToString("mm\\:ss");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {suffixes[suffixIndex]}";
    }

    private static IReadOnlyList<string> SplitTranscript(string transcriptText, int chunkSize)
    {

        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            return [];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < transcriptText.Length)
        {
            var length = Math.Min(chunkSize, transcriptText.Length - start);
            chunks.Add(transcriptText.Substring(start, length));
            start += length;
        }

        return chunks;
    }

    private string ResolveUserId()
    {
        if (ReactiveScope.TryGet(out UserScope userScope) && !string.IsNullOrWhiteSpace(userScope.Id))
        {
            return userScope.Id;
        }

        if (!string.IsNullOrWhiteSpace(app.GlobalState.PrimaryUserId))
        {
            return app.GlobalState.PrimaryUserId;
        }

        // Fallback for local development when auth is disabled
        return "dev-user";
    }
}
