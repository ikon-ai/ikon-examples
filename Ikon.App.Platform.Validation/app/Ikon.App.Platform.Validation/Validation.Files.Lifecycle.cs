using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// The upload lifecycle lab (docs/private/specs/file-upload-spec.md, run end to end). A scenario is a
// set of uploads with a server-side behaviour each (where the bytes go, what each hook does) and a
// client-side plan (size, content, corruption, abandoning, a forced reconnect). The C# side publishes
// the plan to the `upload-lab` React component inside the lab's FileUploadZone, which sends the files
// through the web SDK and reports back what the client saw. The verdict is computed here from both:
// what the hooks observed, what ended up stored, and what the client was told.
public partial class Validation
{
    private sealed record LabScenario(string Key, string Label, string Description, Func<Validation, string, Task<string>> Run);

    private static readonly LabScenario[] LabScenarios =
    [
        new("small", "Small file", "200 KiB to a temp file; chunk hash, progress and stored bytes checked", (v, run) => v.LabSmallAsync(run)),
        new("large", "Large file", "128 MiB to a temp file with an incremental hash in onChunkReceived", (v, run) => v.LabLargeAsync(run)),
        new("zero", "Zero-byte files", "An empty file to a temp file and, in a local run, to a new LocalFile asset", (v, run) => v.LabZeroAsync(run)),
        new("many", "Many files at once", "20 files of mixed sizes started together", (v, run) => v.LabManyAsync(run)),
        new("dest-local", "LocalFile", "1 MiB into a new LocalFile asset (a local run only)", (v, run) => v.LabDestinationAsync(run, AssetClass.LocalFile)),
        new("dest-cloud", "CloudFile", "1.5 MiB into a new CloudFile asset", (v, run) => v.LabDestinationAsync(run, AssetClass.CloudFile)),
        new("dest-public", "CloudFilePublic", "1.5 MiB into a new CloudFilePublic asset", (v, run) => v.LabDestinationAsync(run, AssetClass.CloudFilePublic)),
        new("dest-json", "CloudJson", "A JSON document into a new CloudJson asset", (v, run) => v.LabDestinationAsync(run, AssetClass.CloudJson)),
        new("dest-discard", "DiscardData", "Verified and stored nowhere", (v, run) => v.LabDiscardAsync(run)),
        new("refuse-prestart", "Refused at PreStart", "onUploadPreStart returns false, and throws", (v, run) => v.LabRefusedAsync(run, atStart: false)),
        new("refuse-start", "Refused at Start", "onUploadStart returns false, and throws: no terminal hook", (v, run) => v.LabRefusedAsync(run, atStart: true)),
        new("hash-mismatch", "Corrupted upload", "Bytes that do not match the declared hash, over existing assets of each writable store", (v, run) => v.LabHashMismatchAsync(run)),
        new("app-cancel", "App cancels", "args.Cancel from onChunkReceived mid-transfer, over an existing asset", (v, run) => v.LabAppCancelAsync(run)),
        new("client-abandon", "Client abandons", "The client stops sending mid-transfer; the stall timer ends it (about 60 s)", (v, run) => v.LabClientAbandonAsync(run)),
        new("parallel-lanes", "Parallel hook lanes", "A slow Complete does not hold another upload's hooks", (v, run) => v.LabParallelLanesAsync(run)),
        new("decision-overrun", "Start hook overruns", "onUploadStart past its 15 s deadline; another upload carries on meanwhile", (v, run) => v.LabDecisionOverrunAsync(run)),
        new("chunk-overrun", "Chunk hook overruns", "onChunkReceived past its 10 s deadline fails the upload", (v, run) => v.LabChunkOverrunAsync(run)),
        new("complete-overrun", "Complete overruns", "onUploadComplete past the 50 s completion deadline: false verdict, data kept, no Error", (v, run) => v.LabCompleteOverrunAsync(run)),
        new("resume", "Dropped connection", "The connection drops mid-upload and the client resumes", (v, run) => v.LabResumeAsync(run)),
        new("replace", "Replace an asset", "A new upload over existing assets of each writable store", (v, run) => v.LabReplaceAsync(run)),
        new("complete-throws", "Complete throws", "Committed, then onUploadComplete throws: new assets removed, existing ones keep the new content", (v, run) => v.LabCompleteThrowsAsync(run)),
    ];

    // A hosted app's LocalFile store is its read-only bundle, so there the scenarios that need a
    // writable asset use CloudFile, and the ones about LocalFile itself report SKIP.
    private const string LabLocalFileReadOnly = "SKIP LocalFile is read-only in a hosted app";

    // Not in Run all: it only ends when the app stops under it, and this instance is gone by then, so
    // the browser's lab states the verdict (upl-lab-stop-result). A local run is stopped by saving any
    // file of the app, which hot-reloads it.
    private static readonly LabScenario AppStopScenario =
        new("app-stop", "App stops mid-upload", "A slow 64 MiB upload, then stop the app (save a file of it for a hot reload); the browser's verdict is below", (v, run) => v.LabAppStopAsync(run));

    private const string UploadLabZoneTestId = "upload-lab-zone";
    private const string UploadLabFolder = "upload-lab";
    private const string AppCancelReason = "Cancelled by the app";

    // How long a run waits, once the expected hooks have run, for a hook that must not run to show up.
    private static readonly TimeSpan LabSettle = TimeSpan.FromSeconds(3);

    // How long a run waits, once the browser has reported, for the terminal hooks it expects.
    private static readonly TimeSpan LabTerminalWait = TimeSpan.FromSeconds(30);

    private readonly ClientReactiveDictionary<string, string> _uploadLabResults = new();
    private readonly ClientReactive<bool> _uploadLabRunning = new(false);

    // Runs in this instance. The client reactives above survive a hot reload, which ends any run, so
    // a run counts as live only while this instance has one.
    private int _uploadLabActiveRuns;
    private readonly ClientReactive<string> _uploadLabPlan = new("");

    // Hooks of different uploads run in parallel (spec §7.4), so the registry they all read is concurrent.
    private readonly ConcurrentDictionary<string, LabUpload> _labUploads = new();

    // What each run stored, by run, so a run's clean-up deletes exactly its own assets.
    private readonly ConcurrentDictionary<string, ConcurrentBag<AssetUri>> _labCreatedAssets = new();

    private enum LabDecision
    {
        Accept,
        Reject,
        Throw,
    }

    private sealed record LabClientReport(bool Ok, string Error, double ElapsedMs, bool Reconnected);

    private sealed class LabUpload(string id, string fileName, long size)
    {
        public string Id { get; } = id;
        public string FileName { get; } = fileName;
        public long Size { get; } = size;

        // The client's plan
        public string Content { get; init; } = "random";
        public bool Corrupt { get; init; }
        public int AbandonAfterChunks { get; init; }
        public int ReconnectAfterChunks { get; init; }
        public int DelayMs { get; init; }

        // The server's behaviour
        public AssetUri? Destination { get; init; }
        public bool DestinationAtStart { get; init; }
        public bool Discard { get; init; }
        public LabDecision PreStart { get; init; } = LabDecision.Accept;
        public LabDecision Start { get; init; } = LabDecision.Accept;
        public int StartSleepMs { get; init; }
        public bool HashChunks { get; init; }
        public int FirstChunkSleepMs { get; init; }
        public int EveryChunkSleepMs { get; init; }
        public long CancelAfterBytes { get; init; } = -1;
        public int CompleteSleepMs { get; init; }
        public bool CompleteThrows { get; init; }
        public bool ExpectTerminal { get; init; } = true;
        public bool ExpectStop { get; init; }

        // What the hooks saw. One upload's hooks never overlap (I6), so these need no lock of their own;
        // the evaluator reads them only after the terminal hook has run and the settle time has passed.
        public ConcurrentQueue<string> Hooks { get; } = new();
        public DateTimeOffset? PreStartAt { get; set; }
        public DateTimeOffset? CompleteStartedAt { get; set; }
        public DateTimeOffset? CompleteEndedAt { get; set; }
        public DateTimeOffset? ErrorAt { get; set; }
        public string? DeclaredHash { get; set; }
        public string? StoredHash { get; set; }
        public long? StoredLength { get; set; }
        public string? StoredFailure { get; set; }
        public string? LocalPath { get; set; }
        public AssetUri? CompleteAssetUri { get; set; }
        public string? ChunkHash { get; set; }
        public long ChunkBytes { get; set; }
        public int ChunkCalls { get; set; }
        public bool ChunkBytesWrittenMismatch { get; set; }
        public int ProgressCalls { get; set; }
        public long LastProgressBytes { get; set; }
        public bool ProgressWentBack { get; set; }
        public bool ProgressAfterComplete { get; set; }
        public string? ErrorMessage { get; set; }
        public Func<string?, Task>? Cancel { get; set; }
        public bool Cancelled { get; set; }
        public IncrementalHash? Hasher { get; set; }

        public TaskCompletionSource<LabClientReport> Client { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Terminal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LabClientReport Report => Client.Task.IsCompletedSuccessfully ? Client.Task.Result : new LabClientReport(false, "no report", 0, false);

        // The decision and terminal hooks in the order they ran; FullTrace adds the first chunk and progress call
        public string HookTrace => string.Join(">", Hooks.Where(h => h is not ("Chunk" or "Progress")));

        public string FullTrace => string.Join(">", Hooks);

        public bool Saw(string hook) => Hooks.Contains(hook);
    }

    // Collects what a scenario found wrong; an empty list is a pass.
    private sealed class LabVerdict
    {
        private readonly List<string> _failures = [];

        public void Expect(bool condition, string failure)
        {
            if (!condition)
            {
                _failures.Add(failure);
            }
        }

        public string Result(string passDetail) => _failures.Count == 0 ? $"PASS {passDetail}" : $"FAIL {string.Join("; ", _failures)}";
    }

    [Function(Name = "UploadLabReport", Description = "The upload lab's browser reports what the client saw of one upload", Visibility = FunctionVisibility.External)]
    [AllowAnonymous]
    public void UploadLabReport(string uploadId, string error, double elapsedMs, bool reconnected)
    {
        if (_labUploads.TryGetValue(uploadId, out var upload))
        {
            upload.Client.TrySetResult(new LabClientReport(error.Length == 0, error, elapsedMs, reconnected));
        }
    }

    private void RenderUploadLabSection(UIView view)
    {
        var running = _uploadLabRunning.Value && Volatile.Read(ref _uploadLabActiveRuns) > 0;

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-2"], content: view =>
            {
                view.Text([Text.H2, "flex-1"], "Upload Lifecycle");
                view.Button([Button.PrimaryMd],
                    text: running ? "Running…" : "Run all",
                    props: TestId("upl-run-all"),
                    disabled: running,
                    onClick: () => RunUploadLabAsync(LabScenarios));
            });

            view.Text([Text.Caption, "mb-4"],
                "Each scenario sends generated files through the web SDK and checks hooks, verdicts and stored bytes. " +
                "Cloud destinations use the space's backend. " +
                (LabHosted
                    ? "Hosted: LocalFile is the read-only app bundle, so the scenarios that need a writable asset use CloudFile. "
                    : "Local run: the scenarios that need a writable asset use LocalFile, and CloudFile beside it where they cover both. ") +
                "Each result names the store it used. A full run takes about four minutes.");

            view.FileUploadZone(
                multiple: true,
                onUploadPreStart: OnLabPreStartAsync,
                onUploadStart: OnLabStartAsync,
                onChunkReceived: OnLabChunkAsync,
                onUploadProgress: OnLabProgressAsync,
                onUploadComplete: OnLabCompleteAsync,
                onUploadError: OnLabErrorAsync,
                props: TestId(UploadLabZoneTestId),
                content: view =>
                {
                    view.AddNode("upload-lab", new Dictionary<string, object?> { ["plan"] = running ? _uploadLabPlan.Value : "", ["zoneTestId"] = UploadLabZoneTestId }, style: ["w-full"]);
                });

            view.Box(["grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 items-start mt-4"], content: view =>
            {
                foreach (var scenario in LabScenarios.Append(AppStopScenario))
                {
                    view.Button([Button.OutlineSm, "justify-start"],
                        text: scenario.Label,
                        props: TestId($"upl-run-{scenario.Key}"),
                        disabled: running,
                        onClick: () => RunUploadLabAsync([scenario]));

                    view.Column(["min-w-0"], content: view =>
                    {
                        view.Text([Text.Caption], scenario.Description);
                        var result = _uploadLabResults.TryGetValue(scenario.Key, out var value) ? value : "—";

                        if (result == "Running…" && !running)
                        {
                            result = "Interrupted: the app restarted during the run";
                        }

                        view.Text(["text-sm break-all"], result,
                            props: TestId($"upl-result-{scenario.Key}"));
                    });
                }
            });
        });
    }

    private async Task RunUploadLabAsync(IEnumerable<LabScenario> scenarios)
    {
        if (_uploadLabRunning.Value && Volatile.Read(ref _uploadLabActiveRuns) > 0)
        {
            return;
        }

        Interlocked.Increment(ref _uploadLabActiveRuns);
        _uploadLabRunning.Value = true;

        try
        {
            foreach (var scenario in scenarios)
            {
                _uploadLabResults[scenario.Key] = "Running…";
                var run = Guid.NewGuid().ToString("N")[..8];

                try
                {
                    _uploadLabResults[scenario.Key] = await scenario.Run(this, run);
                }
                catch (Exception ex)
                {
                    _uploadLabResults[scenario.Key] = $"FAIL {ex.Message}";
                }
                finally
                {
                    await CleanUpLabRunAsync(run);
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _uploadLabActiveRuns);
            _uploadLabRunning.Value = false;
        }
    }

    // Publishes the plan to this client's lab component, waits for its report on every upload and for
    // every expected terminal hook, then gives a hook that must not run the settle time to show itself.
    private async Task RunLabUploadsAsync(IReadOnlyList<LabUpload> uploads, TimeSpan timeout, TimeSpan? terminalWait = null)
    {
        foreach (var upload in uploads)
        {
            _labUploads[upload.Id] = upload;
        }

        _uploadLabPlan.Value = JsonSerializer.Serialize(new
        {
            run = Guid.NewGuid().ToString("N"),
            items = uploads.Select(u => new
            {
                id = u.Id,
                name = u.FileName,
                size = u.Size,
                content = u.Content,
                corrupt = u.Corrupt,
                abandonAfterChunks = u.AbandonAfterChunks,
                reconnectAfterChunks = u.ReconnectAfterChunks,
                expectStop = u.ExpectStop,
                delayMs = u.DelayMs,
            }),
        });

        try
        {
            try
            {
                await Task.WhenAll(uploads.Select(u => u.Client.Task)).WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                var silent = uploads.Where(u => !u.Client.Task.IsCompleted).Select(u => u.FileName);
                throw new TimeoutException($"the browser did not report {string.Join(", ", silent)} within {timeout.TotalSeconds:0} s");
            }

            try
            {
                // Error runs after the verdict, and a late Complete after the 50 s completion deadline
                await Task.WhenAll(uploads.Where(u => u.ExpectTerminal).Select(u => u.Terminal.Task)).WaitAsync(terminalWait ?? LabTerminalWait);
            }
            catch (TimeoutException)
            {
                // Reported by the scenario's own checks, which name the hook that never ran
            }

            await Task.Delay(LabSettle);
        }
        finally
        {
            _uploadLabPlan.Value = "";

            foreach (var upload in uploads)
            {
                _labUploads.TryRemove(upload.Id, out _);
            }
        }
    }

    private async Task CleanUpLabRunAsync(string run)
    {
        foreach (var uri in _labCreatedAssets.Where(entry => entry.Key == run).SelectMany(entry => entry.Value))
        {
            try
            {
                await Asset.Instance.DeleteAsync(uri);
            }
            catch (Exception ex)
            {
                // A leftover test asset is harmless and under the lab's own prefix; the run's verdict stands
                Log.Instance.Debug($"Upload lab could not delete {uri}: {ex.Message}");
            }
        }

        _labCreatedAssets.TryRemove(run, out _);

        var directory = LabLocalDirectory(run);

        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException ex)
        {
            // Left for the next run's clean-up, which deletes the same run folder by name
            Log.Instance.Debug($"Upload lab could not delete {directory}: {ex.Message}");
        }
    }

    private AssetUri LabAsset(string run, AssetClass assetClass, string name)
    {
        var uri = assetClass == AssetClass.LocalFile
            ? new AssetUri(AssetClass.LocalFile, $"{UploadLabFolder}/{run}/{name}")
            : new AssetUri(assetClass, $"validation/{UploadLabFolder}/{run}-{name}", spaceId: app.GlobalState.SpaceId);

        _labCreatedAssets.GetOrAdd(run, _ => []).Add(uri);
        return uri;
    }

    private bool LabHosted => app.GlobalState.ServerRunType != ServerRunType.Local;

    // The store a scenario writes to when its point is the upload, not the store
    private AssetClass LabWritableStore => LabHosted ? AssetClass.CloudFile : AssetClass.LocalFile;

    // Every store a scenario about replacing or keeping an existing asset covers
    private AssetClass[] LabWritableStores => LabHosted ? [AssetClass.CloudFile] : [AssetClass.LocalFile, AssetClass.CloudFile];

    private static string LabStoreKey(AssetClass store) => store == AssetClass.LocalFile ? "local" : "cloud";

    private static string LabStoreList(IEnumerable<AssetClass> stores) => string.Join(" and ", stores);

    private string LabLocalDirectory(string run) => Path.Combine(app.DataDirectory, UploadLabFolder, run);

    private string[] LabStagingLeftovers(string run)
    {
        var directory = LabLocalDirectory(run);
        return Directory.Exists(directory) ? Directory.GetFiles(directory, "*.ikon-staging", SearchOption.AllDirectories) : [];
    }

    private static string LabId(string run, string name) => $"lab-{run}-{name}";

    private static async Task<string?> LabStoredHashAsync(AssetUri uri)
    {
        var bytes = await Asset.Instance.TryGetBytesAsync(uri);
        return bytes == null ? null : Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    // ── Hooks ────────────────────────────────────────────────────────────────
    // A file dropped onto the zone by hand has no lab entry and is kept as a temp file.

    private Task<FileUploadResult> OnLabPreStartAsync(FileUploadPreStartArgs args)
    {
        if (!_labUploads.TryGetValue(args.UploadId, out var upload))
        {
            return Task.FromResult<FileUploadResult>(true);
        }

        upload.Hooks.Enqueue("PreStart");
        upload.PreStartAt = DateTimeOffset.UtcNow;
        upload.Cancel = args.Cancel;

        if (upload.HashChunks)
        {
            upload.Hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        }

        return upload.PreStart switch
        {
            LabDecision.Reject => Task.FromResult<FileUploadResult>(false),
            LabDecision.Throw => throw new InvalidOperationException("The lab refuses this upload by throwing at pre-start"),
            _ => Task.FromResult(new FileUploadResult
            {
                AssetUri = upload.DestinationAtStart ? null : upload.Destination,
                DiscardData = upload.Discard,
            }),
        };
    }

    private async Task<FileUploadResult> OnLabStartAsync(FileUploadStartArgs args)
    {
        if (!_labUploads.TryGetValue(args.UploadId, out var upload))
        {
            return true;
        }

        upload.Hooks.Enqueue("Start");
        upload.DeclaredHash = args.Hash;

        if (upload.StartSleepMs > 0)
        {
            // Deliberately ignores the args token: the scenario is a hook that overruns its deadline
            await Task.Delay(upload.StartSleepMs);
        }

        return upload.Start switch
        {
            LabDecision.Reject => false,
            LabDecision.Throw => throw new InvalidOperationException("The lab refuses this upload by throwing at start"),
            _ => new FileUploadResult { AssetUri = upload.DestinationAtStart ? upload.Destination : null },
        };
    }

    private async Task OnLabChunkAsync(FileUploadChunkArgs args)
    {
        if (!_labUploads.TryGetValue(args.UploadId, out var upload))
        {
            return;
        }

        upload.ChunkCalls++;

        if (upload.ChunkCalls == 1)
        {
            upload.Hooks.Enqueue("Chunk");

            if (upload.FirstChunkSleepMs > 0)
            {
                await Task.Delay(upload.FirstChunkSleepMs);
            }
        }

        if (upload.EveryChunkSleepMs > 0)
        {
            await Task.Delay(upload.EveryChunkSleepMs);
        }

        upload.Hasher?.AppendData(args.Data);
        upload.ChunkBytes += args.Data.Length;

        if (args.BytesWritten != upload.ChunkBytes)
        {
            upload.ChunkBytesWrittenMismatch = true;
        }

        if (upload.CancelAfterBytes >= 0 && upload.ChunkBytes >= upload.CancelAfterBytes && !upload.Cancelled && upload.Cancel is { } cancel)
        {
            upload.Cancelled = true;
            // Awaited from inside the upload's own hook lane: the cancel must not wait for that lane
            await cancel(AppCancelReason);
        }
    }

    private Task OnLabProgressAsync(FileUploadProgressArgs args)
    {
        if (!_labUploads.TryGetValue(args.UploadId, out var upload))
        {
            return Task.CompletedTask;
        }

        if (upload.ProgressCalls == 0)
        {
            upload.Hooks.Enqueue("Progress");
        }

        upload.ProgressCalls++;

        if (args.BytesUploaded < upload.LastProgressBytes)
        {
            upload.ProgressWentBack = true;
        }

        upload.LastProgressBytes = args.BytesUploaded;

        if (upload.CompleteStartedAt != null)
        {
            upload.ProgressAfterComplete = true;
        }

        return Task.CompletedTask;
    }

    private async Task OnLabCompleteAsync(FileUploadCompleteArgs args)
    {
        if (!_labUploads.TryGetValue(args.UploadId, out var upload))
        {
            return;
        }

        upload.Hooks.Enqueue("Complete");
        upload.CompleteStartedAt = DateTimeOffset.UtcNow;
        upload.LocalPath = args.LocalTempFilePath;
        upload.CompleteAssetUri = args.AssetUri;

        if (upload.Hasher != null)
        {
            upload.ChunkHash = Convert.ToHexStringLower(upload.Hasher.GetHashAndReset());
        }

        try
        {
            if (args.LocalTempFilePath is { } path)
            {
                await using var file = File.OpenRead(path);
                upload.StoredLength = file.Length;
                upload.StoredHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(file));
            }
            else if (args.AssetUri is { } uri)
            {
                var bytes = await Asset.Instance.GetBytesAsync(uri);
                upload.StoredLength = bytes.Length;
                upload.StoredHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            }
        }
        catch (Exception ex)
        {
            upload.StoredFailure = $"{ex.GetType().Name}: {ex.Message}";
        }

        if (upload.CompleteSleepMs > 0)
        {
            // Deliberately ignores the args token: the scenario is a Complete that overruns its deadline
            await Task.Delay(upload.CompleteSleepMs);
        }

        upload.CompleteEndedAt = DateTimeOffset.UtcNow;

        if (upload.CompleteThrows)
        {
            throw new InvalidOperationException("The lab's complete hook fails after the commit");
        }

        upload.Terminal.TrySetResult();
    }

    private Task OnLabErrorAsync(FileUploadErrorArgs args)
    {
        if (_labUploads.TryGetValue(args.UploadId, out var upload))
        {
            upload.Hooks.Enqueue("Error");
            upload.ErrorAt = DateTimeOffset.UtcNow;
            upload.ErrorMessage = args.ErrorMessage;
            upload.Terminal.TrySetResult();
        }

        return Task.CompletedTask;
    }

    // ── Scenarios ────────────────────────────────────────────────────────────

    // The checks every upload that should succeed passes: the client was told so, Complete ran once
    // with no Error, and what is stored is exactly what the client hashed.
    private static void ExpectDelivered(LabVerdict verdict, LabUpload upload)
    {
        var report = upload.Report;

        if (!report.Ok && !upload.Saw("Complete"))
        {
            verdict.Expect(false, $"{upload.FileName}: failed ({upload.HookTrace}): {report.Error}");
            return;
        }

        verdict.Expect(report.Ok, $"{upload.FileName}: client failed: {report.Error}");
        verdict.Expect(upload.Saw("Complete"), $"{upload.FileName}: no Complete ({upload.HookTrace})");
        verdict.Expect(!upload.Saw("Error"), $"{upload.FileName}: Error ran: {upload.ErrorMessage}");
        verdict.Expect(upload.StoredFailure == null, $"{upload.FileName}: reading the stored bytes failed: {upload.StoredFailure}");
        verdict.Expect(upload.StoredLength == upload.Size, $"{upload.FileName}: stored {upload.StoredLength} bytes, sent {upload.Size}");
        verdict.Expect(upload.DeclaredHash != null && upload.StoredHash == upload.DeclaredHash, $"{upload.FileName}: stored hash {Short(upload.StoredHash)} != declared {Short(upload.DeclaredHash)}");
    }

    // An upload that failed after PreStart accepted it: the client was told, no Complete, one Error.
    private static void ExpectFailedWithError(LabVerdict verdict, LabUpload upload, string? errorContains = null)
    {
        var report = upload.Report;
        verdict.Expect(!report.Ok, $"{upload.FileName}: client reported success");
        verdict.Expect(!upload.Saw("Complete"), $"{upload.FileName}: Complete ran");
        verdict.Expect(upload.Saw("Error"), $"{upload.FileName}: no Error ({upload.HookTrace})");

        if (errorContains != null)
        {
            verdict.Expect(upload.ErrorMessage?.Contains(errorContains, StringComparison.OrdinalIgnoreCase) == true,
                $"{upload.FileName}: Error said '{upload.ErrorMessage}', expected '{errorContains}'");
            verdict.Expect(report.Error.Contains(errorContains, StringComparison.OrdinalIgnoreCase),
                $"{upload.FileName}: client was told '{report.Error}', expected '{errorContains}'");
        }
    }

    private static void ExpectChunksAndProgress(LabVerdict verdict, LabUpload upload)
    {
        verdict.Expect(upload.ChunkBytes == upload.Size, $"{upload.FileName}: onChunkReceived saw {upload.ChunkBytes} of {upload.Size} bytes");
        verdict.Expect(!upload.ChunkBytesWrittenMismatch, $"{upload.FileName}: BytesWritten disagreed with the chunks seen");
        verdict.Expect(upload.ChunkHash == upload.DeclaredHash, $"{upload.FileName}: incremental hash {Short(upload.ChunkHash)} != declared {Short(upload.DeclaredHash)}");
        verdict.Expect(upload.ProgressCalls > 0, $"{upload.FileName}: no progress callback");
        verdict.Expect(!upload.ProgressWentBack, $"{upload.FileName}: progress went backwards");
        verdict.Expect(!upload.ProgressAfterComplete, $"{upload.FileName}: progress after Complete");
        verdict.Expect(upload.LastProgressBytes <= upload.Size, $"{upload.FileName}: progress past the size");
    }

    private static string Short(string? hash) => hash == null ? "none" : hash[..Math.Min(12, hash.Length)];

    private async Task<string> LabSmallAsync(string run)
    {
        var upload = new LabUpload(LabId(run, "small"), "small.bin", 200 * 1024) { HashChunks = true };
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        ExpectDelivered(verdict, upload);
        ExpectChunksAndProgress(verdict, upload);
        verdict.Expect(upload.FullTrace == "PreStart>Start>Chunk>Progress>Complete", $"hook order {upload.FullTrace}");
        return verdict.Result($"{upload.Size} bytes, {upload.ChunkCalls} chunks, {upload.ProgressCalls} progress, {upload.Report.ElapsedMs:0} ms");
    }

    private async Task<string> LabLargeAsync(string run)
    {
        var upload = new LabUpload(LabId(run, "large"), "large.bin", 128L * 1024 * 1024) { HashChunks = true };
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(240));

        var verdict = new LabVerdict();
        ExpectDelivered(verdict, upload);
        ExpectChunksAndProgress(verdict, upload);
        return verdict.Result($"{upload.Size / (1024 * 1024)} MiB, {upload.ChunkCalls} chunks, {upload.ProgressCalls} progress, {upload.Report.ElapsedMs / 1000:0.0} s");
    }

    private async Task<string> LabZeroAsync(string run)
    {
        var temp = new LabUpload(LabId(run, "zero-temp"), "zero-temp.bin", 0) { HashChunks = true };
        var local = LabHosted ? null : new LabUpload(LabId(run, "zero-local"), "zero-local.bin", 0) { Destination = LabAsset(run, AssetClass.LocalFile, "zero-local.bin") };
        await RunLabUploadsAsync(local == null ? [temp] : [temp, local], TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        ExpectDelivered(verdict, temp);
        verdict.Expect(temp.ChunkCalls == 0, $"{temp.FileName}: {temp.ChunkCalls} chunk callbacks for an empty file");
        verdict.Expect(temp.ChunkHash == temp.DeclaredHash, $"{temp.FileName}: empty incremental hash {Short(temp.ChunkHash)} != declared {Short(temp.DeclaredHash)}");

        if (local == null)
        {
            return verdict.Result("the empty temp file stored as empty; zero-local skipped: LocalFile is read-only in a hosted app");
        }

        ExpectDelivered(verdict, local);
        verdict.Expect(LabStagingLeftovers(run).Length == 0, "a LocalFile staging file was left behind");
        return verdict.Result("both empty files stored as empty, as a temp file and as LocalFile");
    }

    private async Task<string> LabManyAsync(string run)
    {
        int[] sizes = [1, 3, 17, 64, 100, 256, 511, 512, 513, 700, 1024, 1025, 1500, 2048, 2049, 3000, 4096, 5000, 6000, 8192];
        var uploads = sizes.Select((kib, index) => new LabUpload(LabId(run, $"many-{index:00}"), $"many-{index:00}.bin", kib * 1024L + index)).ToList();
        await RunLabUploadsAsync(uploads, TimeSpan.FromSeconds(120));

        var verdict = new LabVerdict();

        foreach (var upload in uploads)
        {
            ExpectDelivered(verdict, upload);
        }

        var paths = uploads.Select(u => u.LocalPath).Where(p => p != null).ToList();
        verdict.Expect(paths.Distinct().Count() == paths.Count, "two uploads shared a temp file");
        return verdict.Result($"{uploads.Count} files, {uploads.Sum(u => u.Size) / 1024} KiB, slowest {uploads.Max(u => u.Report.ElapsedMs):0} ms");
    }

    private async Task<string> LabDestinationAsync(string run, AssetClass assetClass)
    {
        if (assetClass == AssetClass.LocalFile && LabHosted)
        {
            return LabLocalFileReadOnly;
        }

        var json = assetClass == AssetClass.CloudJson;
        var name = json ? "dest.json" : "dest.bin";
        var size = assetClass == AssetClass.LocalFile ? 1024 * 1024 : json ? 64 * 1024 : 1536 * 1024;
        var uri = LabAsset(run, assetClass, name);

        // CloudFile's destination is set at Start and the others' at PreStart, so both places are used
        var upload = new LabUpload(LabId(run, "dest"), name, size)
        {
            Content = json ? "json" : "random",
            Destination = uri,
            DestinationAtStart = assetClass == AssetClass.CloudFile,
        };

        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(120));

        var verdict = new LabVerdict();
        var report = upload.Report;
        verdict.Expect(report.Ok, $"client failed: {report.Error}");
        verdict.Expect(upload.Saw("Complete") && !upload.Saw("Error"), $"hooks {upload.HookTrace}: {upload.ErrorMessage}");
        verdict.Expect(upload.CompleteAssetUri?.ToString() == uri.ToString(), $"Complete got AssetUri {upload.CompleteAssetUri}");
        verdict.Expect(upload.LocalPath == null, "Complete got a temp file for an asset upload");

        if (json)
        {
            // CloudJson stores the document, not the bytes: compared as JSON
            var stored = await Asset.Instance.TryGetTextAsync(uri);
            verdict.Expect(stored != null, "nothing stored");

            if (stored != null)
            {
                verdict.Expect(JsonNode.DeepEquals(JsonNode.Parse(stored), JsonNode.Parse(LabJsonDocument(size))), "the stored document differs");
            }
        }
        else
        {
            verdict.Expect(upload.StoredHash == upload.DeclaredHash && upload.StoredLength == size,
                $"stored {upload.StoredLength} bytes hash {Short(upload.StoredHash)}, declared {Short(upload.DeclaredHash)}");
        }

        if (assetClass == AssetClass.LocalFile)
        {
            verdict.Expect(LabStagingLeftovers(run).Length == 0, "a staging file was left behind");
        }

        return verdict.Result($"{size} bytes at {uri}, {report.ElapsedMs:0} ms");
    }

    // The same document the browser builds for a JSON upload of this size (upload-lab.tsx).
    private static string LabJsonDocument(long size)
    {
        var builder = new StringBuilder("{\"lab\":\"upload\",\"rows\":[");
        var row = 0;

        while (builder.Length < size - 64)
        {
            builder.Append(row == 0 ? "" : ",").Append("{\"i\":").Append(row).Append(",\"v\":\"row-").Append(row).Append("\"}");
            row++;
        }

        return builder.Append("]}").ToString();
    }

    private async Task<string> LabDiscardAsync(string run)
    {
        var upload = new LabUpload(LabId(run, "discard"), "discard.bin", 3 * 1024 * 1024 + 7) { Discard = true, HashChunks = true };
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        verdict.Expect(upload.Report.Ok, $"client failed: {upload.Report.Error}");
        verdict.Expect(upload.Saw("Complete") && !upload.Saw("Error"), $"hooks {upload.HookTrace}");
        verdict.Expect(upload.LocalPath == null && upload.CompleteAssetUri == null, "Complete got a destination for discarded data");
        verdict.Expect(upload.ChunkHash == upload.DeclaredHash, "the chunk callbacks saw other bytes than the client hashed");
        return verdict.Result("verified through onChunkReceived, stored nowhere");
    }

    private async Task<string> LabRefusedAsync(string run, bool atStart)
    {
        var store = LabWritableStore;
        var asset = LabAsset(run, store, "refused.bin");
        LabUpload[] uploads =
        [
            new(LabId(run, "false"), "refused-false.bin", 300 * 1024)
            {
                PreStart = atStart ? LabDecision.Accept : LabDecision.Reject,
                Start = atStart ? LabDecision.Reject : LabDecision.Accept,
                ExpectTerminal = false,
            },
            new(LabId(run, "throw"), "refused-throw.bin", 300 * 1024)
            {
                PreStart = atStart ? LabDecision.Accept : LabDecision.Throw,
                Start = atStart ? LabDecision.Throw : LabDecision.Accept,
                ExpectTerminal = false,
            },
            new(LabId(run, "asset"), "refused-asset.bin", 300 * 1024)
            {
                Destination = asset,
                PreStart = atStart ? LabDecision.Accept : LabDecision.Reject,
                Start = atStart ? LabDecision.Reject : LabDecision.Accept,
                ExpectTerminal = false,
            },
        ];

        await RunLabUploadsAsync(uploads, TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        var expectedTrace = atStart ? "PreStart>Start" : "PreStart";

        foreach (var upload in uploads)
        {
            verdict.Expect(!upload.Report.Ok, $"{upload.FileName}: client reported success");
            verdict.Expect(upload.HookTrace == expectedTrace, $"{upload.FileName}: hooks {upload.HookTrace}, expected {expectedTrace} and no terminal hook");
            verdict.Expect(upload.Report.ElapsedMs < 10_000, $"{upload.FileName}: the refusal took {upload.Report.ElapsedMs:0} ms");
        }

        verdict.Expect(!await Asset.Instance.ExistsAsync(asset), $"a refused upload created its {store} asset");
        return verdict.Result($"refused by false and by throwing; client told in {uploads.Max(u => u.Report.ElapsedMs):0} ms; no terminal hook; no {store} asset created");
    }

    private async Task<string> LabHashMismatchAsync(string run)
    {
        var original = RandomNumberGenerator.GetBytes(4096);
        var originalHash = Convert.ToHexStringLower(SHA256.HashData(original));
        var stores = LabWritableStores;
        var existing = new List<(AssetClass Store, AssetUri Uri, LabUpload Upload)>();

        foreach (var store in stores)
        {
            var uri = LabAsset(run, store, "existing.bin");
            await Asset.Instance.SetBytesAsync(uri, original);
            var key = LabStoreKey(store);
            existing.Add((store, uri, new LabUpload(LabId(run, key), $"corrupt-{key}.bin", 3 * 1024 * 1024) { Corrupt = true, Destination = uri }));
        }

        var temp = new LabUpload(LabId(run, "temp"), "corrupt-temp.bin", 2 * 1024 * 1024) { Corrupt = true };
        await RunLabUploadsAsync([.. existing.Select(e => e.Upload), temp], TimeSpan.FromSeconds(90));

        var verdict = new LabVerdict();
        ExpectFailedWithError(verdict, temp);

        foreach (var (store, uri, upload) in existing)
        {
            ExpectFailedWithError(verdict, upload);
            verdict.Expect(await LabStoredHashAsync(uri) == originalHash, $"the existing {store} asset changed");
        }

        verdict.Expect(LabStagingLeftovers(run).Length == 0, "a LocalFile staging file was left behind");
        return verdict.Result($"refused ({temp.Report.Error}); existing {LabStoreList(stores)} assets unchanged");
    }

    private async Task<string> LabAppCancelAsync(string run)
    {
        var original = RandomNumberGenerator.GetBytes(2048);
        var originalHash = Convert.ToHexStringLower(SHA256.HashData(original));
        var store = LabWritableStore;
        var uri = LabAsset(run, store, "cancelled.bin");
        await Asset.Instance.SetBytesAsync(uri, original);

        var upload = new LabUpload(LabId(run, "cancel"), "cancelled.bin", 48L * 1024 * 1024)
        {
            Destination = uri,
            CancelAfterBytes = 4 * 1024 * 1024,
        };

        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(90));

        var verdict = new LabVerdict();
        ExpectFailedWithError(verdict, upload, AppCancelReason);
        verdict.Expect(upload.Cancelled, "the hook never cancelled");
        verdict.Expect(upload.ChunkBytes < upload.Size, "every byte arrived before the cancel took effect");
        verdict.Expect(await LabStoredHashAsync(uri) == originalHash, $"the existing {store} asset changed");
        verdict.Expect(LabStagingLeftovers(run).Length == 0, "a staging file was left behind");
        return verdict.Result($"cancelled after {upload.ChunkBytes / (1024 * 1024)} MiB; client told in {upload.Report.ElapsedMs:0} ms; existing {store} asset unchanged");
    }

    private async Task<string> LabClientAbandonAsync(string run)
    {
        var store = LabWritableStore;
        var uri = LabAsset(run, store, "abandoned.bin");
        var upload = new LabUpload(LabId(run, "abandon"), "abandoned.bin", 8 * 1024 * 1024) { Destination = uri, AbandonAfterChunks = 2 };
        // The client says it abandoned at once; the server only notices when its 60 s stall timer fires
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(30), terminalWait: TimeSpan.FromSeconds(90));

        var verdict = new LabVerdict();
        verdict.Expect(upload.Report.Error == "abandoned", $"the client did not abandon: {upload.Report.Error}");
        verdict.Expect(!upload.Saw("Complete"), "Complete ran");
        verdict.Expect(upload.ErrorMessage?.Contains("timed out", StringComparison.OrdinalIgnoreCase) == true, $"Error said '{upload.ErrorMessage}' ({upload.HookTrace})");
        verdict.Expect(!await Asset.Instance.ExistsAsync(uri), $"the abandoned upload created its {store} asset");
        verdict.Expect(LabStagingLeftovers(run).Length == 0, "a staging file was left behind");
        var after = upload.ErrorAt - upload.PreStartAt;
        return verdict.Result($"ended by the stall timer after {after?.TotalSeconds:0} s: {upload.ErrorMessage}; no {store} asset created");
    }

    private async Task<string> LabParallelLanesAsync(string run)
    {
        var slow = new LabUpload(LabId(run, "slow"), "slow-complete.bin", 256 * 1024) { CompleteSleepMs = 8000 };
        var fast = new LabUpload(LabId(run, "fast"), "fast.bin", 256 * 1024) { DelayMs = 2000, HashChunks = true };
        await RunLabUploadsAsync([slow, fast], TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        ExpectDelivered(verdict, slow);
        ExpectDelivered(verdict, fast);
        verdict.Expect(fast.PreStartAt > slow.CompleteStartedAt, "precondition: the second upload started before the slow Complete did");
        verdict.Expect(fast.CompleteEndedAt < slow.CompleteEndedAt,
            $"the second upload's Complete ended at +{(fast.CompleteEndedAt - slow.CompleteStartedAt)?.TotalMilliseconds:0} ms, after the slow one's");
        verdict.Expect(fast.Report.ElapsedMs < 5000, $"the second upload took {fast.Report.ElapsedMs:0} ms behind a slow Complete");
        return verdict.Result($"second upload done in {fast.Report.ElapsedMs:0} ms while the first's Complete slept 8 s");
    }

    private async Task<string> LabDecisionOverrunAsync(string run)
    {
        var slow = new LabUpload(LabId(run, "slow"), "slow-start.bin", 512 * 1024) { StartSleepMs = 18_000 };
        var other = new LabUpload(LabId(run, "other"), "other.bin", 512 * 1024) { DelayMs = 3000 };
        await RunLabUploadsAsync([slow, other], TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        verdict.Expect(!slow.Report.Ok, "the overrunning upload was accepted");
        verdict.Expect(slow.Report.ElapsedMs is > 12_000 and < 21_000, $"the client was answered after {slow.Report.ElapsedMs:0} ms, expected about 15 s");
        verdict.Expect(!slow.Saw("Complete"), "Complete ran");
        // The hook accepted after its deadline: the upload was refused, and the app owes the Error (§5.3 row 2)
        verdict.Expect(slow.Saw("Error"), $"no Error after the late accept ({slow.HookTrace})");
        ExpectDelivered(verdict, other);
        verdict.Expect(other.CompleteEndedAt < slow.ErrorAt, "the other upload waited for the overrunning hook");
        return verdict.Result($"refused at {slow.Report.ElapsedMs / 1000:0.0} s, Error after the hook returned: {slow.ErrorMessage}; other upload done in {other.Report.ElapsedMs:0} ms");
    }

    private async Task<string> LabChunkOverrunAsync(string run)
    {
        var upload = new LabUpload(LabId(run, "chunk"), "slow-chunk.bin", 4 * 1024 * 1024) { FirstChunkSleepMs = 12_000 };
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(60));

        var verdict = new LabVerdict();
        ExpectFailedWithError(verdict, upload);
        return verdict.Result($"failed: {upload.Report.Error}");
    }

    private async Task<string> LabCompleteOverrunAsync(string run)
    {
        var upload = new LabUpload(LabId(run, "complete"), "slow-complete.bin", 512 * 1024) { CompleteSleepMs = 54_000 };
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(90));

        var verdict = new LabVerdict();
        verdict.Expect(!upload.Report.Ok, "the client was told it succeeded");
        verdict.Expect(upload.Report.Error.Contains("in time", StringComparison.OrdinalIgnoreCase), $"client was told '{upload.Report.Error}'");
        verdict.Expect(upload.Report.ElapsedMs < 58_000, $"the verdict took {upload.Report.ElapsedMs:0} ms, past the client's 60 s wait");
        verdict.Expect(upload.CompleteEndedAt != null, "the Complete hook never returned");
        verdict.Expect(!upload.Saw("Error"), $"Error ran: {upload.ErrorMessage}");
        verdict.Expect(upload.StoredHash == upload.DeclaredHash, "the temp file did not hold the upload during Complete");
        return verdict.Result($"false verdict after {upload.Report.ElapsedMs / 1000:0.0} s, Complete still ran to its end, no Error");
    }

    private async Task<string> LabResumeAsync(string run)
    {
        var upload = new LabUpload(LabId(run, "resume"), "resumed.bin", 48L * 1024 * 1024) { ReconnectAfterChunks = 12, HashChunks = true };
        await RunLabUploadsAsync([upload], TimeSpan.FromSeconds(120));

        var verdict = new LabVerdict();
        verdict.Expect(upload.Report.Reconnected, "the connection never dropped");
        ExpectDelivered(verdict, upload);
        ExpectChunksAndProgress(verdict, upload);
        return verdict.Result($"reconnected mid-upload and finished in {upload.Report.ElapsedMs / 1000:0.0} s");
    }

    private async Task<string> LabReplaceAsync(string run)
    {
        var stores = LabWritableStores;
        var replaced = new List<(AssetClass Store, AssetUri Uri, LabUpload Upload)>();

        foreach (var store in stores)
        {
            var uri = LabAsset(run, store, "replaced.bin");
            await Asset.Instance.SetBytesAsync(uri, RandomNumberGenerator.GetBytes(5000));
            var key = LabStoreKey(store);
            replaced.Add((store, uri, new LabUpload(LabId(run, key), $"replaced-{key}.bin", 700 * 1024) { Destination = uri }));
        }

        await RunLabUploadsAsync([.. replaced.Select(r => r.Upload)], TimeSpan.FromSeconds(90));

        var verdict = new LabVerdict();

        foreach (var (store, uri, upload) in replaced)
        {
            ExpectDelivered(verdict, upload);
            verdict.Expect(await LabStoredHashAsync(uri) == upload.DeclaredHash, $"{store} does not hold the new content");
        }

        verdict.Expect(LabStagingLeftovers(run).Length == 0, "a staging file was left behind");
        return verdict.Result($"the {LabStoreList(stores)} assets hold the new content");
    }

    private async Task<string> LabAppStopAsync(string run)
    {
        // About 1.5 s per MiB once the chunk callbacks fall 8 MiB behind, so the transfer outlasts the
        // time it takes to stop the app by hand
        var upload = new LabUpload(LabId(run, "stop"), "app-stop.bin", 64L * 1024 * 1024)
        {
            Destination = LabAsset(run, LabWritableStore, "app-stop.bin"),
            EveryChunkSleepMs = 1500,
            ExpectStop = true,
        };

        await RunLabUploadsAsync([upload], TimeSpan.FromMinutes(4));
        return $"FAIL the upload ended while the app kept running: {(upload.Report.Ok ? "delivered" : upload.Report.Error)}";
    }

    private async Task<string> LabCompleteThrowsAsync(string run)
    {
        var stores = LabWritableStores;
        var created = new List<(AssetClass Store, AssetUri Uri, LabUpload Upload)>();
        var existing = new List<(AssetClass Store, AssetUri Uri, LabUpload Upload)>();

        foreach (var store in stores)
        {
            var key = LabStoreKey(store);
            var newUri = LabAsset(run, store, "new.bin");
            created.Add((store, newUri, new LabUpload(LabId(run, $"new-{key}"), $"new-{key}.bin", 600 * 1024) { Destination = newUri, CompleteThrows = true }));

            var existingUri = LabAsset(run, store, "existing.bin");
            await Asset.Instance.SetBytesAsync(existingUri, RandomNumberGenerator.GetBytes(3000));
            existing.Add((store, existingUri, new LabUpload(LabId(run, $"existing-{key}"), $"existing-{key}.bin", 600 * 1024) { Destination = existingUri, CompleteThrows = true }));
        }

        await RunLabUploadsAsync([.. created.Select(c => c.Upload), .. existing.Select(e => e.Upload)], TimeSpan.FromSeconds(90));

        var verdict = new LabVerdict();

        foreach (var (_, _, upload) in created.Concat(existing))
        {
            verdict.Expect(!upload.Report.Ok, $"{upload.FileName}: client was told it succeeded");
            verdict.Expect(upload.HookTrace == "PreStart>Start>Complete>Error", $"{upload.FileName}: hooks {upload.HookTrace}");
            verdict.Expect(upload.StoredHash == upload.DeclaredHash, $"{upload.FileName}: Complete did not see the committed bytes");
        }

        // The new asset goes only when the store proves the upload created it: the backend's
        // `created` flag and conditional delete for CloudFile (L3 names a backend without them)
        foreach (var (store, uri, _) in created)
        {
            verdict.Expect(!await Asset.Instance.ExistsAsync(uri), $"the new {store} asset was kept");
        }

        foreach (var (store, uri, upload) in existing)
        {
            verdict.Expect(await LabStoredHashAsync(uri) == upload.DeclaredHash, $"the existing {store} asset lost the new content (I8d)");
        }

        return verdict.Result($"new {LabStoreList(stores)} assets deleted, existing ones keep the new content");
    }
}
