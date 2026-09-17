namespace Ikon.App.Triggers
  sealed record TriggerContext
    ctor(string EventId, string EventType, int SequenceNumber, DateTime FiredAtUtc, string PayloadJson)
    static TriggerContext? Current { get; }
    string EventId { get; init; }
    string EventType { get; init; }
    DateTime FiredAtUtc { get; init; }
    string PayloadJson { get; init; }
    int SequenceNumber { get; init; }
    // Uses the platform's JSON defaults — property names match case-insensitively, so a camelCase payload binds a PascalCase record. Throws on an empty payload.
    T GetPayload<T>()
    static IDisposable Use(TriggerContext context)
