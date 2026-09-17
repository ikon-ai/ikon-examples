using System.Text.Json;

public partial class Validation
{
    private const string LastTriggerAssetPath = "validation/last-trigger.json";

    private readonly Reactive<ValidationTriggerRecord?> _lastTrigger = new(null);
    private int _triggerSeedStarted;

    private AssetUri LastTriggerUri => new(AssetClass.CloudJson, LastTriggerAssetPath, spaceId: app.GlobalState.SpaceId);

    #region docsnippet:email-trigger
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

    // Declaring the listener is what makes the platform deliver the erasure to this space at all.
    // Validation keeps no user data of its own, so the run only records that it happened — the
    // platform-managed state is already erased by the time this is called.
    [Trigger(TriggerEventType.UserErased)]
    internal async Task EraseUserDataAsync(UserDataErasureEventArgs args)
    {
        await RememberErasureAsync(args);
    }

    // Persisted rather than held in memory alone: the userless instance the backend cold-starts to
    // deliver an event is not necessarily the one a browser later joins, so the card would read
    // "never" on the instance a person looks at
    private async Task RememberLastEventAsync(TriggerContext context, string subject)
    {
        var record = new ValidationTriggerRecord
        {
            EventId = context.EventId,
            EventType = context.EventType,
            SequenceNumber = context.SequenceNumber,
            FiredAtUtc = context.FiredAtUtc.ToString("O"),
            HandledAtUtc = DateTime.UtcNow.ToString("O"),
            Subject = subject,
        };

        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        await Asset.Instance.SetTextAsync(LastTriggerUri, json);
        _lastTrigger.Value = record;
        Log.Instance.Info($"Validation trigger {context.EventId} ({context.EventType}, sequence {context.SequenceNumber}) handled");
    }

    // The erasure runner invokes the handler per user id rather than once per delivery, so there is
    // no TriggerContext to date the record by — the erasure id is what names the run.
    private async Task RememberErasureAsync(UserDataErasureEventArgs args)
    {
        var record = new ValidationTriggerRecord
        {
            EventId = args.ErasureId,
            EventType = TriggerEventType.UserErased,
            SequenceNumber = 0,
            FiredAtUtc = DateTime.UtcNow.ToString("O"),
            HandledAtUtc = DateTime.UtcNow.ToString("O"),
            Subject = $"user {args.UserId}",
        };

        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        await Asset.Instance.SetTextAsync(LastTriggerUri, json);
        _lastTrigger.Value = record;
        Log.Instance.Info($"Validation erasure {args.ErasureId} handled for user {args.UserId}");
    }

    private void RenderTriggerCard(UIView view)
    {
        SeedTriggerFromAsset();

        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Event trigger");
            view.Text([Text.Caption, "mb-4"],
                "A [Trigger(TriggerEventType.EmailReceived)] listener records the last inbound email the platform delivered to this app. " +
                "Mail sent to this space's receiving domain while no instance is running cold-starts one and lands here.");

            var record = _lastTrigger.Value;

            if (record is null)
            {
                view.Text([Text.Body, "font-mono"], "never", props: TestId("trigger-last-event"));
                return;
            }

            view.Text([Text.Body, "font-mono"], $"{record.Subject} (event {record.EventId}, sequence {record.SequenceNumber})", props: TestId("trigger-last-event"));

            if (DateTimeOffset.TryParse(record.HandledAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var handledAt))
            {
                var age = DateTimeOffset.UtcNow - handledAt;
                view.Text([Text.Caption, "mt-1"], $"Handled {(int)age.TotalMinutes} min ago, fired {record.FiredAtUtc}", props: TestId("trigger-last-event-age"));
            }
        });
    }

    // Seed the UI mirror from the persisted asset once per process, so the last event is visible
    // after a restart without waiting for the next one. Read-only — see RememberLastEventAsync.
    private void SeedTriggerFromAsset()
    {
        if (Interlocked.Exchange(ref _triggerSeedStarted, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            // Started from inside a UI render (a reactive callback), whose async-local flows in via
            // ExecutionContext. Detach, or the seed write below is swallowed as re-entrant.
            using var reactiveDetach = ReactiveManager.SuppressCallbackTracking();

            try
            {
                var json = await Asset.Instance.GetTextAsync(LastTriggerUri);
                var record = JsonSerializer.Deserialize<ValidationTriggerRecord>(json);

                // Never overwrite an event handled between the read starting and this update landing
                if (record is not null && _lastTrigger.Value is null)
                {
                    _lastTrigger.Value = record;
                }
            }
            catch
            {
                // No event asset yet — the card shows "never" until the first delivery writes it
            }
        });
    }
}

public class ValidationTriggerRecord
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public int SequenceNumber { get; set; }
    public string FiredAtUtc { get; set; } = "";
    public string HandledAtUtc { get; set; } = "";
    public string Subject { get; set; } = "";
}
