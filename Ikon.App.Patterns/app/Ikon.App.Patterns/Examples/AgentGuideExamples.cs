using Ikon.Common.Core.Protocol;
using Microsoft.EntityFrameworkCore;

namespace Ikon.App.Patterns.Examples;

// The examples the agent guides teach — as code the Patterns app runs. The class is split across
// Examples/AgentGuides/, one part per guide; this part holds what no single guide claims.
//
// Compiling an example is not the same as it being real. A private method nobody calls proves the
// names and types still exist and nothing else. So every example here is reachable from the
// gallery's "Agent guide examples" demo: the free ones run when it renders, which the pattern render
// smoke-test does too, and the ones that call an AI service run when a person presses their button
// — on the service's mock (GalleryMocks), so pressing every one costs nothing.
//
// A hand-written fence in the docs is a copy of code that once worked: nothing compiles it, so it
// stays exactly as written while the API beneath it is renamed or deleted. Each method here is
// wrapped in a `#region example:<id>` that ExamplePinner copies into the fence under
// `<!-- ikon-example: <id> -->`, so the example a reader copies IS this code and the build fails
// before it can drift.
//
// The rules that keep the splice honest:
//   * The region contains ONLY the lines the doc should show. Whatever the example needs but does
//     not teach — a parameter, a using, a surrounding method — lives outside it.
//   * The code is the shape the docs TEACH, not merely the shape that happens to exist elsewhere.
//   * `ExamplesArePinnedTests` holds the docs to no hand-written fence at all.
internal sealed partial class AgentGuideExamples(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{    public string Slug => "agent-guide-examples";
    public string Title => "Agent guide examples";
    public string Category => "Examples";
    public void RenderDemo(IView view) => RenderDocExamplesSection(view);

    private readonly Reactive<string?> _docExampleResult = new(null);
    private readonly Reactive<string?> _docExampleError = new(null);
    private readonly ClientReactive<string> _docValueMutationResult = new("Value mutation: not run");
    private readonly Reactive<string?> _docExampleBusy = new(null);

    // What each AI button runs, by label, as the last render drew it: the test that presses them all
    // reads this, so it runs exactly what a person's press would.
    internal IReadOnlyDictionary<string, DocExampleRun> DocExampleRuns => _docExampleRuns;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DocExampleRun> _docExampleRuns = new(StringComparer.Ordinal);

    // NeedsLiveApp: it plays audio through the app's Audio, which only a running app has.
    internal sealed record DocExampleRun(Func<Task<string>> Run, bool NeedsLiveApp);

    // Inputs the examples take as the reader's own data: a 1x1 PNG, a 256x256 one for the upscaler
    // (which refuses an image under 128 pixels a side), and the zip signature a .docx starts with.
    private static readonly byte[] SamplePng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
    private static readonly byte[] SampleUpscalePng = ImageUtils.EncodePng(new byte[256 * 256 * 4], 256, 256);
    private static readonly byte[] SampleDocx = [0x50, 0x4B, 0x03, 0x04];

    private void RenderDocExamplesSection(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Doc examples");

            // These cost nothing, so running them on render is the strongest statement available:
            // the example is not merely compiled, it executed to draw what is on screen. Each sits
            // under its label, so the smoke test judges it on its own rather than by the card's
            // other content. DocSpeechRecognition is left out: it subscribes the app's recognition
            // handler, which a render would add again every time it ran.
            ExampleGallery.RenderMethodLabel(view, nameof(DocConditionalRendering));
            DocConditionalRendering(view);

            if (_imageData.Value is null)
            {
                PatternDemoNote.RenderCaption(view, "The condition is false, so nothing draws: the image shows once _imageData holds bytes");
            }

            ExampleGallery.RenderMethodLabel(view, nameof(DocSortableList));
            DocSortableList(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocVirtualList));
            DocVirtualList(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocJoinUrlAndQr));
            DocJoinUrlAndQr(view, app.GlobalState.SpaceId);
            ExampleGallery.RenderMethodLabel(view, nameof(DocTextAndContent));
            DocTextAndContent(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocLayoutComponents));
            DocLayoutComponents(view);
            PatternDemoNote.RenderCaption(view, "Each layout container above is empty, so only the separator shows");
            ExampleGallery.RenderMethodLabel(view, nameof(DocLoadingState));
            DocLoadingState(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocCallbackErrorHandling));
            DocCallbackErrorHandling(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocActionButton));
            DocActionButton(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocCaptureButtons));
            DocCaptureButtons(view);
            ExampleGallery.RenderMethodLabel(view, nameof(DocMicToggleButton));
            DocMicToggleButton(view);
            DocLogLevels("swap-42", 3);
        });

        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Run on press");

            view.Row(["gap-2 items-center flex-wrap"], content: view =>
            {
                view.Button([Button.OutlineMd], text: "Run value mutation",
                    onClick: () => _docValueMutationResult.Value = ValueMutationExamples.Run());
                view.Text([Text.Body], _docValueMutationResult.Value);
            });

            view.Text([Text.Caption, "mt-4 mb-2"], "AI services: each runs on the service's mock here, so pressing costs nothing");

            view.Row(["gap-2 flex-wrap"], content: view =>
            {
                RenderDocExampleButton(view, "Object generation", async () => (await AiRObjectGenerationExamples.AirObjectGenerationAsync()).Name);
                RenderDocExampleButton(view, "Image one-shot", async () => await GenerateImageOneShotAsync("A neon-lit cyberpunk street"));
                RenderDocExampleButton(view, "Image config", async () => { await DocImageGenerateConfigAsync(); return "generated"; });
                RenderDocExampleButton(view, "Upscale one-shot", () => DocImageUpscaleOneShotAsync(SampleUpscalePng));
                RenderDocExampleButton(view, "Upscale", async () => await DocImageUpscaleConfigAsync());
                RenderDocExampleButton(view, "Sound effect", DocSoundEffectOneShotAsync);
                RenderDocExampleButton(view, "Sound effect streamed", async () => { await DocSoundEffectStreamedAsync(); return "played"; }, needsLiveApp: true);
                RenderDocExampleButton(view, "Speech", async () => (await DocSpeechGenerateOneShotAsync()).ToString());
                RenderDocExampleButton(view, "Speech streamed", async () => { await DocSpeechGenerateStreamedAsync(); return "played"; }, needsLiveApp: true);
                RenderDocExampleButton(view, "Speak", async () => { await DocSpeakAsync(ReactiveScope.ClientId); return "spoken"; }, needsLiveApp: true);
                RenderDocExampleButton(view, "Speech recognition", async () => { await DocSpeechRecognizeBatchAsync(new float[16000]); return "recognized"; });
                RenderDocExampleButton(view, "Pronunciation", async () => { await DocPronunciationAssessmentAsync(new float[16000], 16000); return "assessed"; });
                RenderDocExampleButton(view, "Video", DocVideoGenerateOneShotAsync);
                RenderDocExampleButton(view, "Video config", DocVideoGenerateConfigAsync);
                RenderDocExampleButton(view, "Video enhance", () => DocVideoEnhanceOneShotAsync("https://example.com/clip.mp4"));
                RenderDocExampleButton(view, "Video enhance config", () => DocVideoEnhanceConfigAsync([0, 0, 0, 0]));
                RenderDocExampleButton(view, "Web search", async () => (await DocWebSearchOneShotAsync()).ToString());
                RenderDocExampleButton(view, "Web search config", async () => { await DocWebSearchConfigAsync(); return "searched"; });
                RenderDocExampleButton(view, "Embeddings", async () => (await DocEmbeddingsOneShotAsync()).ToString());
                RenderDocExampleButton(view, "Data services", async () =>
                {
                    await DocOtherDataServicesAsync("Hello there", SamplePng, SampleDocx, ["Cats purr when content", "Dogs bark at strangers"], "dogs bark");
                    return "scraped, classified, read, converted and ranked";
                });
                RenderDocExampleButton(view, "Moderation by label", () => DocModerationByLabelAsync("You never listen in meetings"));
                RenderDocExampleButton(view, "Typed decisions", async () => { await DocTypedDecisionsAsync("I was charged twice for one order", new DocOrder("A-1001")); return "decided"; });
                RenderDocExampleButton(view, "Decision per stakeholder", async () => { await DocDecisionStakeholdersAsync("A fix for the wrong invoice total is ready; the logs that would confirm it arrive in 40 minutes."); return "weighed"; });
                RenderDocExampleButton(view, "Emerge basic", async () => { await DocEmergeBasicAsync("tide pools"); return "analyzed"; });
                RenderDocExampleButton(view, "Emerge typed run", async () => (await Research("tide pools")).Summary);
                RenderDocExampleButton(view, "Emerge tools", DocEmergeToolsRunAsync);
                RenderDocExampleButton(view, "Cancellation", async () => (await DocCancellationAsync("tide pools")).Summary);
                RenderDocExampleButton(view, "Best of", async () => { await DocBestOfAsync("a seaside cafe"); return "picked"; });
                RenderDocExampleButton(view, "Conversation history", async () => { await DocConversationHistoryAsync("Hi there", "Tell me more"); return "replied twice"; });
            });

            if (_docExampleError.Value is { } error)
            {
                view.Text([Text.Caption, "mt-3 text-error-primary"], error);
            }
            else if (_docExampleResult.Value is { } result)
            {
                view.Text([Text.Caption, "mt-3"], result);
            }
        });
    }

    private void RenderDocExampleButton(UIView view, string label, Func<Task<string>> run, bool needsLiveApp = false)
    {
        var busy = _docExampleBusy.Value == label;
        _docExampleRuns[label] = new DocExampleRun(run, needsLiveApp);

        view.Button([Button.OutlineSm], text: busy ? $"{label}…" : label, disabled: _docExampleBusy.Value != null,
            onClick: async () =>
            {
                _docExampleBusy.Value = label;
                _docExampleError.Value = null;
                _docExampleResult.Value = null;

                try
                {
                    GalleryMocks.Require();
                    _docExampleResult.Value = $"{label}: {await run()}";
                }
                catch (Exception ex)
                {
                    // Shown on the card rather than thrown, so the demo stays usable. Every service
                    // here is a mock, so no provider is to blame: a failure, an AIException included,
                    // is the example's or its mock's, and the log keeps the trace for whoever fixes it.
                    Log.Instance.Error($"Doc example {label} failed on the gallery's mocks: {ex}");
                    _docExampleError.Value = $"{label} failed with {ex.GetType().Name}: the example or its mock is broken, and the app log has the trace";
                }
                finally
                {
                    _docExampleBusy.Value = null;
                }
            });
    }

    // The app-side shapes these examples stand on. They are the reader's own records in the docs,
    // so they live outside every region — the example teaches the reactive, not the payload.
    private sealed record MyState(string Title = "");
    private sealed record Prefs(bool DarkMode = false);
    private sealed record TodoItem(string Text = "", bool Done = false);
    private sealed record Bookmark(string Url = "");

    private readonly Reactive<DateTime> _now = new(DateTime.Now);

    private DateTime DocNow() => _now.Value;

    private int DocVisits() => _totalVisits.Value + _todos.Count + _bookmarks.Count
        + _state.Value.Title.Length + (_prefs.Value.DarkMode ? 1 : 0);

    private readonly Reactive<byte[]?> _imageData = new(null);
    private readonly Reactive<string> _imageMime = new("image/png");
    private readonly ClientReactive<string> _host = new("");

    private static Task LongRunningTask() => Task.CompletedTask;

    // Seeded so the sortable-list example draws rows a person can reorder.
    private readonly ReactiveList<string> _items = new(["Write the brief", "Review the draft", "Ship it"]);

    // Created on first use, like Audio: the render smoke-test's stand-in app throws on the
    // notification service an inbox binds to.
    private NotificationInbox? _inboxInstance;
    private NotificationInbox _inbox => _inboxInstance ??= new(app);

    private static async Task DocTypedDecisionsAsync(string ticketText, object order)
    {
        #region example:typed-decisions
        using var decider = new Decider(DecisionModel.Jev);

        var triage = await decider.DecideAsync(ticketText, new Dictionary<string, DecisionQuestion>
        {
            ["department"] = DecisionQuestion.Choice("Which team should handle this?", "billing", "technical", "sales"),
            ["frustration"] = DecisionQuestion.Score("How frustrated is the customer?", "calm", "annoyed", "furious"),
            ["refund"] = DecisionQuestion.Noul("Is a refund being requested?"),
        });

        string team = triage["department"].AsChoice();         // one of the option names
        double frustration = triage["frustration"].AsScore();  // fractional, indexed from 0
        double refundOdds = triage["refund"].AsNoul();         // a probability, not a bool

        // Structured state instead of text, and the one-shot form for a single question
        string warehouse = await Decider.ChooseAsync(
            DecisionState.From(order), "Which warehouse ships this?", "helsinki", "tampere");
        #endregion

        _ = (team, frustration, refundOdds, warehouse);
    }

    private static async Task DocDecisionStakeholdersAsync(string incident)
    {
        #region example:decision-stakeholders
        // Eevo answers once per stakeholder and weighs the consequences listed under each option
        using var decider = new Decider(DecisionModel.Eevo1);

        var question = DecisionQuestion.Choice("Ship the fix now, or wait for the logs that confirm it?", "ship", "wait")
            .ForStakeholders(
                DecisionStakeholder.Of("customers", "The customers who see a wrong invoice total"),
                DecisionStakeholder.Of("team", "The on-call team", values: "A deploy they can stand behind"))
            .WithConsequences("ship",
                DecisionConsequence.Benefit("About 200 customers stop seeing a wrong total 40 minutes sooner", "Correct sooner"),
                DecisionConsequence.Harm("The team deploys without the logs that would confirm the fix", "Blind deploy").For("team"))
            .WithConsequences("wait",
                DecisionConsequence.Harm("About 200 customers see a wrong invoice total for 40 more minutes", "Wrong totals").For("customers"));

        var answer = await decider.DecideAsync(incident, question);

        // One verdict per stakeholder, with each option's expected value on that stakeholder's own scale
        foreach ((string stakeholder, StakeholderAnswer verdict) in answer.ByStakeholder)
        {
            double shipValue = verdict.ExpectedValues["ship"].Mean;
            Log.Instance.Info($"{stakeholder}: {verdict.Choice} ({verdict.Confidence:P0}); shipping is worth {shipValue:0.0} to them");
        }

        // There is no verdict across stakeholders. AsChoice returns the option they all chose and throws
        // when they disagree, which is when to show the table to a person
        string shared = answer.AllAgree ? answer.AsChoice() : "the stakeholders disagree";
        #endregion

        _ = shared;
    }

    private sealed record DocOrder(string Id);

    private sealed record DocCategory(string Name, double Total, string Hex);
    private sealed record DocDay(string Label, double Amount);
    private sealed record AnalysisResult(string Summary);

    private static string SearchWeb(string query) => query;
    private static string GetData(string topic) => topic;

    private static int MyMethod(int a) => a;

    // The reader's own pipeline type. RegisterPipeline<T> only requires a class, so the example
    // teaches the registration rather than the pipeline.
    private sealed class MyPipeline;

    private static Task RiskyOperation() => Task.CompletedTask;

    private sealed record DocClip(string Url, string PosterUrl);

    private readonly ClientReactive<string> _clientTheme = new("light");

    private sealed record Settings(string Theme)
    {
        public static Settings Default => new("light");
    }

    private readonly Reactive<string?> _streamId = new(null);

    private sealed record CreativeResponse(string Tagline);

    private static double ScoreResponse(CreativeResponse response) => response.Tagline.Length;

    private sealed record ChatResponse(string Reply);
    private sealed class MyStreamState(Context clientContext)
    {
        private readonly List<float> _samples = [];
        public Context ClientContext { get; } = clientContext;
        public IReadOnlyList<float> Samples => _samples;
        public void AddSamples(float[] samples) => _samples.AddRange(samples);
    }

    private readonly Dictionary<string, MyStreamState> _myStreamStates = [];

    private sealed record DashboardLayout
    {
        public int Columns { get; init; }
    }

    private readonly PersistentUserReactive<bool> _measuring = new(false);

    private readonly Reactive<string> _measurementState = new("");

    private sealed record Courier(int SessionId, double Lat, double Lon);

    private readonly ReactiveList<Courier> _couriers = new();
    private static Task DoWork() => Task.CompletedTask;
    private Audio? _audio;
    private readonly ClientReactive<string> _activeTab = new("");

    // Created on first use: the render smoke-test hands the gallery a stand-in app that throws on
    // everything a live audio pipeline would subscribe to.
    private Audio Audio => _audio ??= new(app);

    private static Task SendChatMessageAsync(string text) => Task.CompletedTask;

    private static Task RememberLastEventAsync(TriggerContext context, string subject) => Task.CompletedTask;
}

// Abstract so the bundle scan passes it over: DiscoverTriggers registers a [Trigger] on any concrete
// class, and this one is an example of the app's listener, not a listener of this app.
file abstract class EmailTriggerExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private static Task RememberLastEventAsync(TriggerContext context, string subject) => Task.CompletedTask;

    #region example:email-trigger
    [Trigger(TriggerEventType.EmailReceived, MaxParallelism = 4)]
    internal async Task OnEmailReceivedAsync(TriggerContext context, CancellationToken ct)
    {
        // The payload is the envelope only; the subject and body stay behind app.Email
        var envelope = context.GetPayload<EmailReceivedPayload>();
        var message = await app.Email.GetMessageAsync(envelope.Id, ct);

        // Returning acknowledges the event; throwing leaves it pending for redelivery with backoff
        await RememberLastEventAsync(context, message.Subject);
    }
    #endregion
}

file static class SimpleStylingExamples
{
    public static void Render(UIView view)
    {
        #region example:styling-oneliners
        view.Button([Button.PrimaryMd, "mt-2 w-fit self-center"], text: "Submit");
        view.Box([Card.Default, "p-6 mb-4"], content: view => { /* ... */ });
        #endregion
    }

    public static void Keyboard(UIView view)
    {
        #region example:keyboard-listener
        // KeyboardListener
        view.KeyboardListener(global: true,
            onKeyDown: async args => { /* args.Key, args.ShiftKey, args.CtrlKey */ },
            content: view => { /* ... */ });
        #endregion
        PatternDemoNote.RenderCaption(view, "A global keyboard listener is mounted here: it draws nothing and receives every key pressed anywhere on the page, with its modifier state");
    }

    public static async Task OptimisticConcurrencyAsync(AssetUri uri, string modified)
    {
        #region example:asset-optimistic-concurrency
        var content = await Asset.Instance.GetTextWithMetadataAsync(uri);
        // ... modify content.Content ...
        var result = await Asset.Instance.TrySetTextAsync(uri, modified, new AssetMetadata(lastModified: content.MetaData?.LastModified));
        if (result.IsConflict) { /* re-read and retry */ }
        #endregion
    }
}

#region example:pipeline-secret
[Pipeline]
public class FetchFromGithub(IPipelineHost<EmptyPipelineConfig> host)
{
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        string token = host.Secrets["GITHUB_TOKEN"];

        if (host.Secrets.TryGet("GITHUB_API_BASE", out var apiBase))
        {
            Log.Instance.Info($"Using custom GitHub API base: {apiBase}");
        }

        Log.Instance.Info($"Running in organisation {host.OrganisationId} space {host.SpaceId}");

        // ...
        inputItems.Output();
        await Task.CompletedTask;
    }
}
#endregion

// The typed result the Emerge example returns. Public and top-level so the docs' corpus check
// resolves the name the guide shows.
public sealed record TopicBrief(
    [Description("One-sentence summary of the topic")] string Summary,
    [Description("Key facts a reader should know")] string[] KeyFacts,
    [Description("Follow-up questions worth pursuing")] string[] OpenQuestions,
    [Description("0.0–1.0 confidence in the brief's accuracy")] double Confidence);
