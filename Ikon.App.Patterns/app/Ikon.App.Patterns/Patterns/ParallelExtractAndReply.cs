namespace Ikon.App.Patterns.Patterns;

// Pattern: parallel-extract-and-reply — see docs/patterns/parallel-extract-and-reply.md.
// The types and stubs outside the region stand in for the app's session model, per-client answer
// queue, and the scripted question/chat plumbing the two-call turn drives.
internal sealed class ParallelExtractAndReply : IPatternDemo
{
    public string Slug => "parallel-extract-and-reply";
    public string Title => "Parallel extract and reply";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend AI pattern with no standalone UI: runs trait extraction and the character reply as two parallel LLM calls per answer. See the source and docs/patterns/parallel-extract-and-reply.md.");

    private enum Screen { Intro, Interview, Result }

    private sealed record TraitDelta
    {
        public string Trait { get; init; } = "";
        public double Value { get; init; }
    }

    private sealed record InterviewQuestion(string Text);

    private sealed class TraitProfile
    {
        public void Apply(TraitDelta delta) => throw new NotImplementedException();
    }

    private sealed class SessionState
    {
        public readonly Reactive<Screen> CurrentScreen = new(Screen.Intro);
        public readonly Reactive<bool> IsProcessing = new(false);
        public readonly Reactive<int> QuestionIndex = new(0);
        public readonly Reactive<string> CurrentQuestion = new("");
        public readonly TraitProfile AccumulatedTraits = new();
        public KernelContext InterviewContext = new();
    }

    private const int TotalQuestionCount = 10;
    private const string InterviewSystemPrompt = "You are an in-character quiz host. Keep replies to 1-2 sentences and do not ask follow-up questions.";
    private const string ExtractionSystemPrompt = "Given a quiz question and the user's answer, output a JSON object describing how the answer shifts the tracked trait.";
    private readonly CancellationTokenSource _appCts = new();
    private System.Threading.Channels.Channel<string> GetOrCreateAnswerQueue(int clientId) => throw new NotImplementedException();
    private InterviewQuestion GetQuestion(int questionIndex) => throw new NotImplementedException();
    private static void StartProcessingTextCycle(SessionState session) => throw new NotImplementedException();
    private static void StopProcessingTextCycle(SessionState session) => throw new NotImplementedException();
    private static void AddChatMessage(SessionState session, string text) => throw new NotImplementedException();
    private Task FinishInterviewAsync(SessionState session, CancellationToken ct) => throw new NotImplementedException();

    #region docsnippet:pattern-parallel-extract-and-reply
    private async Task ProcessAnswersAsync(SessionState session, int clientId)
    {
        var queue = GetOrCreateAnswerQueue(clientId);
        await foreach (var userText in queue.Reader.ReadAllAsync(_appCts.Token))
        {
            if (session.CurrentScreen.Value != Screen.Interview) break;
            session.IsProcessing.Value = true;
            StartProcessingTextCycle(session);
            try
            {
                var question = GetQuestion(session.QuestionIndex.Value);
                var extractionTask = ExtractTraitsAsync(question.Text, userText, _appCts.Token);
                var replyTask     = GenerateCharacterReplyAsync(session, userText, _appCts.Token);
                await Task.WhenAll(extractionTask, replyTask);

                var traitDelta = await extractionTask;
                var reply      = await replyTask;
                if (traitDelta != null) session.AccumulatedTraits.Apply(traitDelta);
                AddChatMessage(session, reply);

                session.QuestionIndex.Value++;
                if (session.QuestionIndex.Value >= TotalQuestionCount)
                {
                    await FinishInterviewAsync(session, _appCts.Token);
                    break;
                }
                var next = GetQuestion(session.QuestionIndex.Value);
                session.InterviewContext = session.InterviewContext.Add(new MessageBlock(MessageBlockRole.Model, next.Text));
                session.CurrentQuestion.Value = next.Text;
            }
            finally
            {
                StopProcessingTextCycle(session);
                session.IsProcessing.Value = false;
            }
        }
    }

    private async Task<TraitDelta?> ExtractTraitsAsync(string question, string answer, CancellationToken ct)
    {
        try
        {
            return await Emerge.Run<TraitDelta>(LLMModel.Default, pass =>
            {
                pass.SystemPrompt = ExtractionSystemPrompt;
                pass.Command = $"Question: {question}\nAnswer: {answer}";
            }, ct);
        }
        catch (EmergenceStoppedException)
        {
            return null;
        }
    }

    private async Task<string> GenerateCharacterReplyAsync(SessionState session, string userText, CancellationToken ct)
    {
        var (reply, context) = await Emerge.Run<string>(LLMModel.Default, session.InterviewContext, pass =>
        {
            pass.SystemPrompt = InterviewSystemPrompt;
            pass.Command = userText;
        }, ct).FinalAsync(ct);

        session.InterviewContext = context;
        return reply ?? "";
    }
    #endregion
}
