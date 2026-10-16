using Dapper;

namespace Ikon.App.Patterns.Patterns;

// Pattern: review-and-approval-workflow — see docs/patterns/review-and-approval-workflow.md.
// The demo draws a reviewer's queue from sample rows; its buttons write to the app's database,
// which only a running app has.
internal sealed class ReviewAndApprovalWorkflow : IPatternDemo
{
    public string Slug => "review-and-approval-workflow";
    public string Title => "Review and approval with files";
    public string Category => "Accounts";

    private const string SampleReviewerId = "user-reviewer";

    private readonly IAppBase app;

    public ReviewAndApprovalWorkflow(IAppBase app)
    {
        this.app = app;
        _submissions.AddRange(
        [
            new Submission("s1", "user-aino", "Brochure, Finnish", "submissions/s1/v1.docx", Status.Submitted, null),
            new Submission("s2", "user-bruno", "Contract, Swedish", "submissions/s2/v2.pdf", Status.ChangesRequested, "Section 4 still reads as machine output"),
            new Submission("s3", "user-chidi", "Menu, English", "submissions/s3/v1.pdf", Status.Approved, null),
        ]);
    }

    public void RenderDemo(IView view) => RenderQueue((UIView)view, SampleReviewerId, isReviewer: true);

    #region example:pattern-review-and-approval-workflow
    private enum Status { Submitted, ChangesRequested, Approved }

    private sealed record Submission(string Id, string AuthorId, string Title, string FilePath, Status Status, string? ReviewNote);

    // Every move a submission can make, and who makes it. The buttons and the handler read the same
    // table, so the screen never offers a move the handler refuses.
    private sealed record Move(Status From, string Action, Status To, bool ByReviewer);

    private static readonly Move[] Moves =
    [
        new(Status.Submitted, "Approve", Status.Approved, ByReviewer: true),
        new(Status.Submitted, "Request changes", Status.ChangesRequested, ByReviewer: true),
        new(Status.ChangesRequested, "Resubmit", Status.Submitted, ByReviewer: false),
    ];

    // The rows are stored in a table; LoadAsync fills this list from app.OnStarting, and every move
    // updates the table first and the list after it.
    private readonly ReactiveList<Submission> _submissions = new();
    // Per user, so a reload keeps a half-written note
    private readonly UserReactive<string> _reviewNote = new("");

    private async Task LoadAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS submissions (id text PRIMARY KEY, author_id text NOT NULL, title text NOT NULL,
                file_path text NOT NULL, status text NOT NULL, review_note text);
            """);
        var rows = await db.QueryAsync<(string Id, string AuthorId, string Title, string FilePath, string Status, string? ReviewNote)>(
            "SELECT id, author_id, title, file_path, status, review_note FROM submissions");
        _submissions.ReplaceAll(rows.Select(row => new Submission(row.Id, row.AuthorId, row.Title, row.FilePath, Enum.Parse<Status>(row.Status), row.ReviewNote)));
    }

    private static bool MayMake(Move move, Submission submission, string userId, bool isReviewer) =>
        move.From == submission.Status
        && (move.ByReviewer
            // Nobody approves their own work, reviewer or not
            ? isReviewer && submission.AuthorId != userId
            : submission.AuthorId == userId);

    private async Task<bool> MoveAsync(string submissionId, string action, string userId, bool isReviewer)
    {
        var submission = _submissions.FirstOrDefault(s => s.Id == submissionId);
        var move = submission is null ? null : Moves.FirstOrDefault(m => m.Action == action && MayMake(m, submission, userId, isReviewer));

        if (move is null)
        {
            return false;
        }

        var note = move.To == Status.ChangesRequested ? _reviewNote.Value.Trim() : null;

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();

        // The status in the WHERE makes two reviewers pressing at once safe: the second update
        // finds the row already moved and changes nothing
        var updated = await db.ExecuteAsync(
            "UPDATE submissions SET status = @to, review_note = @note WHERE id = @submissionId AND status = @from",
            new { to = move.To.ToString(), note, submissionId, from = move.From.ToString() });

        if (updated == 0)
        {
            return false;
        }

        _submissions.Update(list => list.Select(s => s.Id == submissionId ? s with { Status = move.To, ReviewNote = note } : s));
        _reviewNote.Value = "";
        return true;
    }

    private void RenderQueue(UIView view, string userId, bool isReviewer)
    {
        view.Column(["gap-3 max-w-xl"], content: view =>
        {
            foreach (var submission in _submissions)
            {
                view.Column([Card.Default, "gap-2 p-4"], key: submission.Id, content: card =>
                {
                    card.Row([Layout.Row.SpaceBetween], content: row =>
                    {
                        row.Text([Text.Label], submission.Title);
                        var (label, tone) = submission.Status switch
                        {
                            Status.Submitted => ("Waiting for review", SemanticTone.Warning),
                            Status.ChangesRequested => ("Changes requested", SemanticTone.Error),
                            _ => ("Approved", SemanticTone.Success),
                        };
                        row.Badge(label, tone: tone);
                    });

                    card.ActionButton([Button.GhostSm, "self-start"], action: ActionKind.DownloadFile, text: "Open file",
                        options: new DownloadFileActionOptions
                        {
                            FileName = Path.GetFileName(submission.FilePath),
                            // Resolved on the click: a private file's URL is signed and expires an hour after it is made
                            UrlProvider = async () => await app.Files.Data.GetUrlAsync(submission.FilePath),
                        });

                    if (submission.ReviewNote is { Length: > 0 } reviewNote)
                    {
                        card.Text([Text.Body], reviewNote);
                    }

                    var moves = Moves.Where(m => MayMake(m, submission, userId, isReviewer)).ToList();

                    if (moves.Any(m => m.To == Status.ChangesRequested))
                    {
                        card.TextArea(label: "What needs to change", bind: _reviewNote);
                    }

                    card.Row(["gap-2"], content: row =>
                    {
                        foreach (var move in moves)
                        {
                            row.Button(move.To == Status.Approved ? [Button.PrimarySm] : [Button.OutlineSm], text: move.Action,
                                ariaLabel: $"{move.Action}: {submission.Title}",
                                onClick: async () => await MoveAsync(submission.Id, move.Action, userId, isReviewer));
                        }
                    });
                });
            }
        });
    }
    #endregion
}
