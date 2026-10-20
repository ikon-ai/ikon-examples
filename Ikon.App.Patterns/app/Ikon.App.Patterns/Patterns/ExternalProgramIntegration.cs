namespace Ikon.App.Patterns.Patterns;

// Pattern: external-program-integration — see docs/patterns/external-program-integration.md.
// Endpoints are served only from an [App] class, so here they only compile, and the demo draws the
// job list that a partner's script reads and updates.
internal sealed class ExternalProgramIntegration(IAppBase app) : IPatternDemo
{
    private static readonly string[] SampleTitles =
    [
        "Dripping kitchen tap, flat 3B",
        "Stairwell light out, staircase A",
        "Sauna heater does not start",
    ];

    public string Slug => "external-program-integration";
    public string Title => "An outside program calls the app";
    public string Category => "Platform mechanics";

    public void RenderDemo(IView view)
    {
        RenderJobs((UIView)view);

        // Simulates a tenant reporting a repair, which a real app stores in its database
        view.Button([Button.OutlineMd, "mt-3"], text: "Add a sample job", onClick: async () =>
            _jobs.Add(new Job($"job-{1040 + _jobs.Count}", SampleTitles[_jobs.Count % SampleTitles.Length], "new")));
    }

    #region example:pattern-external-program-integration
    // Public, because a public handler cannot take a less accessible type (CS0051)
    public sealed record Job(string Id, string Title, string Status);

    public sealed record JobStatusUpdate(string Status);

    // A real app keeps the jobs in its database. An endpoint call runs in the instance with the
    // empty identity, which a per-user instance does not share.
    private readonly ReactiveList<Job> _jobs = new();

    // The route is "/jobs", without /api: the platform serves every endpoint under /api, so a
    // script calls GET https://{app}.ikonai.app/api/jobs with an X-Api-Key header. Public, because
    // a script cannot carry a signed grant: the key is the authorization.
    [HttpGet("/jobs", Auth = EndpointAuth.Public)]
    public HttpResult ListJobs(Ikon.App.HttpRequest request) =>
        HasPartnerKey(request) ? HttpResult.Ok(_jobs.ToList()) : HttpResult.Unauthorized();

    // POST /api/jobs/{jobId}/status with {"status":"accepted"}: the path segment binds jobId and
    // the JSON body binds update.
    [HttpPost("/jobs/{jobId}/status", Auth = EndpointAuth.Public)]
    public HttpResult SetJobStatus(string jobId, JobStatusUpdate update, Ikon.App.HttpRequest request)
    {
        if (!HasPartnerKey(request))
        {
            return HttpResult.Unauthorized();
        }

        if (update.Status is not ("accepted" or "done"))
        {
            return HttpResult.BadRequest("status must be accepted or done");
        }

        if (!_jobs.Any(job => job.Id == jobId))
        {
            return HttpResult.NotFound();
        }

        _jobs.ReplaceWhere(job => job.Id == jobId, job => job with { Status = update.Status });
        return HttpResult.Ok();
    }

    // The PARTNER_KEY secret, compared in constant time so the response time does not tell how much
    // of a guessed key was right. TryGetValue, because a missing header must be a 401, not a 500.
    private bool HasPartnerKey(Ikon.App.HttpRequest request) =>
        app.Secrets.TryGet("PARTNER_KEY", out var partnerKey)
        && request.Headers.TryGetValue("X-Api-Key", out var sentKey)
        && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(sentKey), Encoding.UTF8.GetBytes(partnerKey));

    private void RenderJobs(UIView view)
    {
        view.Column([Card.Default, "gap-2 p-4 w-full max-w-2xl"], content: col =>
        {
            col.Text([Text.H3], "Repair jobs");

            if (_jobs.Count == 0)
            {
                col.EmptyState([EmptyState.RootSm], title: "No jobs yet", description: "Jobs you start appear here.");
            }

            foreach (var job in _jobs)
            {
                col.Row(["items-center justify-between gap-3"], key: job.Id, content: row =>
                {
                    row.Text([Text.Body], job.Title);
                    row.Text([Text.Caption], job.Status);
                });
            }

            col.Text([Text.Caption], "A partner's script reads and updates these at /api/jobs.");
        });
    }
    #endregion

    #region example:pattern-external-program-integration-policy
    // The same list behind the partner-key policy in router/index.ts. The gateway checks the key
    // before the request reaches the app, so the handler has no check of its own.
    [HttpGet("/partner/jobs", AuthPolicy = "partner-key")]
    public IReadOnlyList<Job> ListJobsBehindPolicy() => _jobs.ToList();
    #endregion
}
