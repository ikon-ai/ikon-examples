using ProtocolContext = Ikon.Common.Core.Protocol.Context;

public partial class Validation
{
    // Dedicated ids, so the battery never moves an answer the Consent tab or a real user gave.
    private const string SelfTestUserId = "validation-selftest-user";
    private const string SelfTestConsentPurpose = "validation-selftest-consent";
    private const string SelfTestConsentNeverAskedPurpose = "validation-selftest-never-asked";
    private const string SelfTestChannelName = "validation-selftest-channel";

    private sealed class SelfTestNotificationChannel : INotificationChannel
    {
        private int _sent;

        public string Name => SelfTestChannelName;

        public int Sent => Volatile.Read(ref _sent);

        public Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
        {
            Interlocked.Increment(ref _sent);
            return Task.FromResult(true);
        }
    }

    private async Task<string> SelfTestConsentAsync()
    {
        var consent = app.Consent;
        var changes = new List<ConsentChange>();
        Action<ConsentChange> handler = change =>
        {
            if (change.UserId == SelfTestUserId && change.Record.Purpose == SelfTestConsentPurpose)
            {
                lock (changes)
                {
                    changes.Add(change);
                }
            }
        };

        consent.OnChanged(handler);
        IReadOnlyList<ConsentRecord> records;

        try
        {
            Expect(consent.StateOf(SelfTestUserId, SelfTestConsentNeverAskedPurpose).State == ConsentAnswer.NotAsked, "a purpose nobody answered did not read as NotAsked");
            Expect(!consent.IsGranted(SelfTestUserId, SelfTestConsentNeverAskedPurpose), "a purpose nobody answered read as granted");
            Expect(consent.StateOf("", SelfTestConsentPurpose).State == ConsentAnswer.NotAsked, "an empty user id did not read as NotAsked");

            var revoked = consent.Revoke(SelfTestUserId, SelfTestConsentPurpose);
            Expect(revoked.State == ConsentAnswer.Denied && !revoked.Granted && revoked.DecidedAt != null, $"Revoke returned {revoked.State} with DecidedAt={revoked.DecidedAt}");
            Expect(!consent.IsGranted(SelfTestUserId, SelfTestConsentPurpose), "IsGranted was true after Revoke");

            var required = ExpectThrows<ConsentRequiredException>(() => consent.Require(SelfTestUserId, SelfTestConsentPurpose), "Require after Revoke");
            Expect(required.UserId == SelfTestUserId && required.Purpose == SelfTestConsentPurpose, $"ConsentRequiredException named {required.UserId}/{required.Purpose}");

            var granted = consent.Grant(SelfTestUserId, SelfTestConsentPurpose);
            Expect(granted.Granted && consent.IsGranted(SelfTestUserId, SelfTestConsentPurpose), "Grant did not make IsGranted true");
            consent.Require(SelfTestUserId, SelfTestConsentPurpose);
            Expect(consent.StateOf(SelfTestUserId, SelfTestConsentPurpose).State == ConsentAnswer.Granted, "StateOf did not read Granted after Grant");

            records = consent.RecordsFor(SelfTestUserId);
            Expect(records.Any(record => record.Purpose == SelfTestConsentPurpose && record.Granted), "RecordsFor does not list the granted purpose");
            Expect(records.All(record => record.Purpose != SelfTestConsentNeverAskedPurpose), "RecordsFor lists a purpose nobody answered");
        }
        finally
        {
            consent.RemoveHandler(handler);
        }

        consent.Revoke(SelfTestUserId, SelfTestConsentPurpose);

        List<ConsentChange> seen;

        lock (changes)
        {
            seen = [.. changes];
        }

        Expect(seen.Count == 2, $"OnChanged fired {seen.Count} times for two answers (a removed handler must not see the third)");
        Expect(seen[0].Record.State == ConsentAnswer.Denied && seen[1].Record.State == ConsentAnswer.Granted, $"OnChanged order was {string.Join(",", seen.Select(change => change.Record.State))}");

        return $"Revoke then Require threw ConsentRequiredException, Grant then Require passed, OnChanged fired 2x and stopped after RemoveHandler, RecordsFor lists {records.Count}";
    }

    private async Task<string> SelfTestCostsBudgetAsync()
    {
        string id;

        using (var budget = app.Costs.Budget("validation-selftest", 0.25))
        {
            Expect(budget.Name == "validation-selftest", $"Name was {budget.Name}");
            Expect(budget.MaxCredits == 0.25, $"MaxCredits was {budget.MaxCredits}");
            Expect(budget.Id.StartsWith("validation-selftest/", StringComparison.Ordinal), $"Id {budget.Id} is not name/guid/max");

            var parsed = SpendBudgetScope.TryParse(budget.Id);
            Expect(parsed is { } scope && scope.BudgetName == budget.Name && scope.MaxCredits == budget.MaxCredits, $"SpendBudgetScope.TryParse did not round-trip {budget.Id}");

            using (budget.Enter())
            {
                using var inner = app.Costs.Budget("validation-selftest-inner", 0.1);
                Expect(inner.Id != budget.Id, "two budgets shared an id");
            }

            id = budget.Id;
            budget.Dispose();
        }

        ExpectThrows<ArgumentException>(() => app.Costs.Budget("", 1).Dispose(), "Budget with an empty name");
        ExpectThrows<ArgumentException>(() => app.Costs.Budget("a/b", 1).Dispose(), "Budget with a / in the name");
        ExpectThrows<ArgumentException>(() => app.Costs.Budget("validation-selftest", 0).Dispose(), "Budget with a zero cap");

        return $"budget {id} named, parsed back, entered, nested and disposed twice; empty name, '/' and a zero cap refused";
    }

    private async Task<string> SelfTestCostsBackendAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var status = await app.Costs.GetSpendStatusAsync(timeout.Token);

        Expect(status.Mode is "off" or "warn" or "enforce", $"Mode was '{status.Mode}'");
        Expect(status.Applicable || (status.Counters.Count == 0 && status.Breaches.Count == 0), "a non-applicable status carried counters or breaches");
        Expect(status.Counters.All(counter => counter.Scope is "app-session" or "user" or "space" or "organisation"), $"unknown counter scope in {string.Join(",", status.Counters.Select(counter => counter.Scope))}");
        Expect(status.Counters.All(counter => counter.LimitCredits >= 0 && counter.CurrentCredits >= 0), "a counter had negative credits");

        using var budget = app.Costs.Budget("validation-selftest-backend", 0.01);
        var spent = await budget.SpentCreditsAsync(timeout.Token);
        Expect(spent == 0, $"a fresh budget that ran nothing reports {spent} credits spent");

        return $"spend status Applicable={status.Applicable} Mode={status.Mode} Counters={status.Counters.Count}; fresh budget spent 0";
    }

    private async Task<string> SelfTestInboxAsync()
    {
        const string user = "validation-selftest-inbox-user";
        var inbox = _selfTestInbox;
        var silentWithChannel = NotificationRoute.Silent.With(SelfTestChannelName);

        inbox.ClearFor(user);
        Expect(inbox.ItemsFor(user).Count == 0 && inbox.UnreadCountFor(user) == 0, "ClearFor left items behind");

        var sentBefore = _selfTestChannel.Sent;
        var first = await inbox.NotifyAsync(user, new NotificationContent("Self-test one", "first", Tag: "selftest-a"), kind: "selftest", route: silentWithChannel);
        Expect(first.Item is { Kind: "selftest", Read: false }, "the first notification did not land as an unread inbox item of kind selftest");
        Expect(first.Delivered.Contains(SelfTestChannelName) && _selfTestChannel.Sent == sentBefore + 1, $"the registered channel was not delivered to (Delivered={string.Join(",", first.Delivered)})");
        Expect(first.PushResults.Count == 0 && !first.Delivered.Contains(NotificationInbox.PushChannel), "an inbox without a push service reported a push");

        await inbox.NotifyAsync(user, new NotificationContent("Self-test one again", Tag: "selftest-a"), route: NotificationRoute.Silent);
        var collapsed = inbox.ItemsFor(user);
        Expect(collapsed.Count == 1 && collapsed[0].Title == "Self-test one again", $"the same Tag did not collapse into one item ({collapsed.Count} items)");

        var second = await inbox.NotifyAsync(user, new NotificationContent("Self-test two", Tag: "selftest-b"), route: NotificationRoute.Silent.With(SelfTestDeliberate));
        Expect(second.Skipped.Contains(SelfTestDeliberate), "an unregistered channel was not reported as skipped");
        Expect(inbox.ItemsFor(user).Count == 2 && inbox.UnreadCountFor(user) == 2, $"expected 2 unread items, found {inbox.ItemsFor(user).Count} items / {inbox.UnreadCountFor(user)} unread");
        Expect(inbox.ItemsFor(user)[0].Title == "Self-test two", "ItemsFor is not newest first");

        inbox.MarkReadFor(user, second.Item!.Id);
        Expect(inbox.UnreadCountFor(user) == 1 && inbox.ItemsFor(user).Single(item => item.Id == second.Item.Id).Read, "MarkReadFor did not mark the item read");

        inbox.MuteFor(user, SelfTestChannelName);
        var sentBeforeMuted = _selfTestChannel.Sent;
        var muted = await inbox.NotifyAsync(user, new NotificationContent("Self-test muted", Tag: "selftest-c"), route: silentWithChannel);
        Expect(muted.Skipped.Contains(SelfTestChannelName) && _selfTestChannel.Sent == sentBeforeMuted, "a muted channel was still sent to");
        inbox.MuteFor(user, SelfTestChannelName, muted: false);

        var low = await inbox.NotifyAsync(user, new NotificationContent("Self-test ambient", Tag: "selftest-d", Priority: NotificationPriority.Low), route: silentWithChannel);
        Expect(low.Skipped.Contains(SelfTestChannelName) && low.Item != null, "a Low priority notification left the inbox");

        var noInbox = await inbox.NotifyAsync(user, new NotificationContent("Self-test no inbox"), route: new NotificationRoute(Inbox: false, Push: false));
        Expect(noInbox.Item == null, "a route without the inbox still recorded an item");

        inbox.SetQuietHoursFor(user, new TimeOnly(22, 0), new TimeOnly(6, 0));
        var quiet = inbox.QuietHoursFor(user);
        Expect(quiet is { StartUtc.Hour: 22, EndUtc.Hour: 6 }, "QuietHoursFor did not read back what SetQuietHoursFor stored");
        Expect(quiet!.Contains(new TimeOnly(23, 30)) && quiet.Contains(new TimeOnly(5, 0)) && !quiet.Contains(new TimeOnly(12, 0)), "an overnight quiet window answered Contains wrongly");
        inbox.ClearQuietHoursFor(user);
        Expect(inbox.QuietHoursFor(user) == null, "ClearQuietHoursFor left the window set");

        inbox.ClearFor(user);
        Expect(inbox.ItemsFor(user).Count == 0, "ClearFor left items behind at the end");

        return "Tag collapse, newest-first order, MarkReadFor, custom channel delivered/muted/unregistered/Low skipped, inbox-less route, overnight quiet hours round trip";
    }

    private async Task<string> SelfTestBackgroundWorkAsync()
    {
        Expect(BackgroundWork.DefaultHold > TimeSpan.Zero && BackgroundWork.MaxHold >= BackgroundWork.DefaultHold, $"DefaultHold={BackgroundWork.DefaultHold} MaxHold={BackgroundWork.MaxHold}");

        await ExpectThrowsAsync<ArgumentOutOfRangeException>(async () => await app.BackgroundWork.StartAsync(TimeSpan.Zero, "validation self-test"), "StartAsync with a zero duration");

        var scope = await app.BackgroundWork.StartAsync(TimeSpan.FromMinutes(1), "validation self-test");
        await scope.ExtendAsync(TimeSpan.FromSeconds(30));
        await ExpectThrowsAsync<ArgumentOutOfRangeException>(async () => await scope.ExtendAsync(TimeSpan.Zero), "ExtendAsync with a zero duration");
        await scope.DisposeAsync();
        await scope.DisposeAsync();
        await scope.ExtendAsync(TimeSpan.FromSeconds(1));

        return $"scope held, extended and released; zero durations refused; double dispose and extend-after-release are no-ops (DefaultHold {BackgroundWork.DefaultHold.TotalMinutes:0} min, MaxHold {BackgroundWork.MaxHold.TotalHours:0} h)";
    }

    private async Task<string> SelfTestUploadsAsync()
    {
        const string uploadId = "validation.selftest.upload";

        app.Uploads.Register(uploadId, onStart: args => Task.FromResult(new FileUploadResult { Accepted = false }));
        app.Uploads.Register(uploadId, onStart: args => Task.FromResult<FileUploadResult>(false), onComplete: args => Task.CompletedTask);

        ExpectThrows<ArgumentException>(() => app.Uploads.Register("", onStart: args => Task.FromResult<FileUploadResult>(false)), "Register with an empty id");
        ExpectThrows<ArgumentNullException>(() => app.Uploads.Register(uploadId, onStart: null!), "Register without onStart");

        FileUploadResult accepted = true;
        FileUploadResult refused = false;
        Expect(accepted.Accepted && !refused.Accepted && accepted.AssetUri == null, "the bool conversion of FileUploadResult is wrong");

        return $"{uploadId} registered and re-registered (replaces); empty id and null onStart refused; FileUploadResult bool conversion holds";
    }

    private async Task<string> SelfTestSeedAsync()
    {
        ExpectThrows<ArgumentNullException>(() => app.Seed(null!), "Seed(null)");

        // The seed ran (or was skipped) at startup, so a registration now is never invoked. The
        // first call succeeds on a fresh instance and throws on every later run; the second must
        // always throw, because an app has exactly one seed.
        var firstRegistered = true;

        try
        {
            app.Seed(() => Task.CompletedTask);
        }
        catch (InvalidOperationException)
        {
            firstRegistered = false;
        }

        ExpectThrows<InvalidOperationException>(() => app.Seed(() => Task.CompletedTask), "a second Seed registration");

        return firstRegistered
            ? "Seed(null) refused; first post-startup registration accepted, the second threw InvalidOperationException"
            : "Seed(null) refused; a seed was already registered, and registering again threw InvalidOperationException";
    }

    private async Task<string> SelfTestSharedSessionAsync()
    {
        var shared = new ProtocolContext { Parameters = new Dictionary<string, string> { [ProtocolContext.SharedSessionParameter] = "TRUE" } };
        var notShared = new ProtocolContext { Parameters = new Dictionary<string, string> { [ProtocolContext.SharedSessionParameter] = "false" } };
        var absent = new ProtocolContext { Parameters = new Dictionary<string, string>() };

        Expect(shared.IsSharedSession, "a context minted with ikon-shared-session=TRUE is not shared (the value is case-insensitive)");
        Expect(!notShared.IsSharedSession && !absent.IsSharedSession, "a context without ikon-shared-session=true reads as shared");

        var decoded = ProtocolMessage.CopyFrom(ProtocolMessage.Create(1, new Ikon.Common.Core.Protocol.OnClientLeft { ClientContext = shared }).Data.Span)
            .GetPayload<Ikon.Common.Core.Protocol.OnClientLeft>().ClientContext;
        Expect(decoded.IsSharedSession, "IsSharedSession did not survive a protocol round trip of the Context");

        var clients = app.GlobalState.Clients.Values.ToList();
        var sharedClients = clients.Count(client => client.IsSharedSession);

        return $"parameter parsed case-insensitively, survives the wire; {sharedClients} of {clients.Count} connected clients are shared sessions";
    }
}
