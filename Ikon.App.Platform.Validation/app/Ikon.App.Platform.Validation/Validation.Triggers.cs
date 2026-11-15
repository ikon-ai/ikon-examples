using System.Text.Json;

public partial class Validation
{
    private const string LastTriggerAssetPath = "validation/last-trigger.json";

    // Read from the persisted asset the first time the card shows, so the last event is visible after a
    // restart without waiting for the next one, then set by each event handled here
    private readonly Loader<AssetUri, ValidationTriggerRecord?> _lastTrigger = new((uri, _) => ReadLastTriggerAsync(uri), name: "LastTrigger");

    private AssetUri LastTriggerUri => new(AssetClass.CloudJson, LastTriggerAssetPath, spaceId: app.GlobalState.SpaceId);

    [Trigger(TriggerEventType.EmailReceived, MaxParallelism = 4)]
    internal async Task OnEmailReceivedAsync(TriggerContext context, CancellationToken ct)
    {
        // The payload is the envelope only; the subject and body stay behind app.Email
        var envelope = context.GetPayload<EmailReceivedPayload>();
        var message = await app.Email.GetMessageAsync(envelope.Id, ct);

        // Returning acknowledges the event; throwing leaves it pending for redelivery with backoff
        await RememberLastEventAsync(context, message.Subject);
    }
    
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
        _lastTrigger.Set(LastTriggerUri, record);
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
        _lastTrigger.Set(LastTriggerUri, record);
        Log.Instance.Info($"Validation erasure {args.ErasureId} handled for user {args.UserId}");
    }

    private void RenderTriggerCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            // A [Trigger(TriggerEventType.EmailReceived)] listener: mail to the space's receiving domain
            // while no instance runs cold-starts one, so a line here proves the trigger path end to end.
            view.Text([Text.H3, "mb-4"], "Inbound email trigger");

            var record = _lastTrigger.Get(LastTriggerUri).Value;
            RenderLastRun(view, record?.HandledAtUtc, "trigger-last-event-time", label: "Last email:");

            if (record is not null)
            {
                view.Row([Layout.Row.Sm, "items-baseline flex-wrap mt-2"], content: view =>
                {
                    view.Text([Text.Body], "Subject:");
                    view.Text([Text.Body, "break-words"], record.Subject, props: TestId("trigger-last-event"));
                });
            }
        });
    }

    // Read-only — see RememberLastEventAsync. No event asset yet reads as none, and the card shows
    // "never" until the first delivery writes it.
    private static async Task<ValidationTriggerRecord?> ReadLastTriggerAsync(AssetUri uri)
    {
        var json = await Asset.Instance.TryGetTextAsync(uri);
        return json is null ? null : JsonSerializer.Deserialize<ValidationTriggerRecord>(json);
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
