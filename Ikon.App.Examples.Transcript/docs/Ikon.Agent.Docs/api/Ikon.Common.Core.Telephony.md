namespace Ikon.Common.Core.Telephony
  sealed record SmsMessage
    // From: Who sent it, in E.164. Pass it to app.Telephony.SendSmsAsync to reply.
    // To: The number of the app's that received it.
    // Text: The message body.
    ctor(string From, string To, string Text, string MessageId)
    string From { get; init; }
    string MessageId { get; init; }
    string Text { get; init; }
    string To { get; init; }
  // The outcome of sending an SMS. No price: a send is charged to the space in platform credits, readable with ikon app costs.
  sealed record SmsSendResult
    // MessageId: The provider's id for the message, for correlating delivery reports.
    // From: The number or sender id the message was sent from.
    // Parts: Billable segments. A message using non-GSM characters fits roughly half as much per segment.
    // Status: The provider's status for the message at the moment it was accepted.
    // Replyable: Whether the recipient can reply. False when the space holds no number local to the recipient's market: a foreign number is commonly stripped in transit and shown as "Unknown", so the message arrives but nothing can be sent back.
    ctor(string MessageId, string From, int Parts, string Status, bool Replyable)
    string From { get; init; }
    string MessageId { get; init; }
    int Parts { get; init; }
    bool Replyable { get; init; }
    string Status { get; init; }
  sealed record TelephonyNumber
    // Number: The number in E.164 form, for example +358401234567.
    // Country: The ISO 3166-1 alpha-2 country the number belongs to.
    // Provider: Which carrier serves this number. Two of the app's numbers may differ.
    // Capabilities: What the number can carry, as the provider names it — sms, voice.
    // IsDefault: Whether this is the number used when a send or a call names none. At most one of the app's numbers is the default; when none is, the platform picks one local to each recipient's market.
    // SessionIdentity: Which instance this number's incoming messages and calls are delivered to. Empty means the app's shared instance. Two numbers can carry different identities, which is how one app answers as several users.
    ctor(string Number, string Country, string Provider, IReadOnlyList<string> Capabilities, bool IsDefault, IReadOnlyDictionary<string, string> SessionIdentity)
    IReadOnlyList<string> Capabilities { get; init; }
    string Country { get; init; }
    bool IsDefault { get; init; }
    string Number { get; init; }
    string Provider { get; init; }
    IReadOnlyDictionary<string, string> SessionIdentity { get; init; }
  sealed record TelephonyStatus
    // Enabled: Whether the space holds any number at all.
    // Numbers: The numbers the space holds. Messages and calls are sent from these.
    ctor(bool Enabled, IReadOnlyList<TelephonyNumber> Numbers)
    bool Enabled { get; init; }
    IReadOnlyList<TelephonyNumber> Numbers { get; init; }
