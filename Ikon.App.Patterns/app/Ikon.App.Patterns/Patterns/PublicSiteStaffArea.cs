namespace Ikon.App.Patterns.Patterns;

// Pattern: public-site-staff-area — see docs/patterns/public-site-staff-area.md.
// The demo draws the site as a visitor sees it; its switch below shows the staff view in place of a
// signed-in account with the staff role, which the gallery has none of.
internal sealed class PublicSiteStaffArea(IAppBase app) : IPatternDemo
{
    public string Slug => "public-site-staff-area";
    public string Title => "Public site with a staff area";
    public string Category => "Accounts";

    public void RenderDemo(IView view)
    {
        RenderSite((UIView)view);
        view.Button([Button.OutlineMd, "self-start mt-4"], text: _isStaff.Value ? "Show the visitor view" : "Show the staff view", onClick: async () =>
        {
            _isStaff.Value = !_isStaff.Value;
            _signedIn.Value = _isStaff.Value;
        });
    }

    #region example:pattern-public-site-staff-area
    private sealed record Question(string Id, string Text);

    private ClientProfiles Profiles { get; } = new(app);

    private readonly ClientReactive<bool> _signedIn = new(false);
    private readonly ClientReactive<bool> _isStaff = new(false);
    private readonly ClientReactive<string> _question = new("");

    // What visitors send and staff read. It reaches staff because every client is in the one
    // instance (SessionIdentity()); a real app also keeps it in a database table.
    private readonly ReactiveList<Question> _questions = new();

    /// <summary>Call once from Main.</summary>
    private void WireStaffRole()
    {
        app.OnClientJoined(async ctx =>
        {
            // Visitors are guests. Signing in ends that client, and the page joins again as the account.
            if (ctx.IsAnonymous || await Profiles.GetProfileAsync(ctx) is not { } profile)
            {
                return;
            }

            // The join handler runs in the joining client's scope, so these are that client's flags
            _signedIn.Value = true;
            _isStaff.Value = profile.HasRole("staff") || profile.HasRole(UserRole.Admin);
        });
    }

    private void RenderSite(UIView view)
    {
        view.Column(["gap-6 w-full max-w-3xl"], content: page =>
        {
            page.Row(["items-center justify-between gap-3"], content: header =>
            {
                header.Text([Text.H2], "Harbour Legal");

                if (_signedIn.Value)
                {
                    header.Button([Button.GhostSm], text: "Sign out", onClick: async () => await ClientFunctions.LogoutAsync());
                }
                else
                {
                    // The app's own sign-in screen, with every method the [Auth] section lists
                    header.Button([Button.GhostSm], text: "Staff sign in", onClick: async () => await ClientFunctions.LoginShowAsync("Sign in with your staff account"));
                }
            });

            page.Column(["gap-2"], content: ask =>
            {
                ask.Text([Text.Body], "Ask us about a tenancy, a contract or a dispute. A lawyer answers within two working days.");
                ask.TextArea(label: "Your question", bind: _question);
                ask.Button([Button.PrimaryMd, "self-start"], text: "Send question", disabled: string.IsNullOrWhiteSpace(_question.Value), onClick: SendQuestion);
            });

            if (_isStaff.Value)
            {
                RenderStaffArea(page);
            }
            else if (_signedIn.Value)
            {
                page.Text([Text.Caption], "This account has no staff access. Ask an administrator to add you.");
            }
        });
    }

    private void SendQuestion()
    {
        if (_question.Value.Trim() is { Length: > 0 } text)
        {
            _questions.Add(new Question(Guid.NewGuid().ToString("N"), text));
            _question.Value = "";
        }
    }

    private void RenderStaffArea(UIView view)
    {
        view.Column([Card.Default, "gap-2 p-4"], content: staff =>
        {
            staff.Text([Text.H3], "Staff: questions");

            if (_questions.Count == 0)
            {
                staff.Text([Text.Body], "No questions yet.");
            }

            foreach (var question in _questions)
            {
                staff.Row(["items-start justify-between gap-3"], key: question.Id, content: row =>
                {
                    row.Text([Text.Body], question.Text);
                    row.Button([Button.OutlineSm], text: "Answered", onClick: () => MarkAnswered(question));
                });
            }
        });
    }

    private void MarkAnswered(Question question)
    {
        // Check here and not only in the render, because a handler can be reached without the screen that draws its button
        if (_isStaff.Value)
        {
            _questions.Remove(question);
        }
    }
    #endregion
}
