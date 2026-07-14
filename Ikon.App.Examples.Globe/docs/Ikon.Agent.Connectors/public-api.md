# Ikon.Agent.Connectors Public API

namespace Ikon.Agent.Connectors
  sealed record DriveListRequest
    ctor(string? FolderId = null, int Limit = 50)
    string? FolderId { get; init; }
    int Limit { get; init; }
  sealed class DriveSkill : Skill
    ctor(Drive drive)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  sealed record SlackHistoryRequest
    ctor(string Channel, int Limit = 20)
    string Channel { get; init; }
    int Limit { get; init; }
  sealed record SlackPostRequest
    ctor(string Channel, string Text, string? ThreadTs = null)
    string Channel { get; init; }
    string Text { get; init; }
    string? ThreadTs { get; init; }
  sealed class SlackSkill : Skill
    ctor(Slack slack)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  sealed record WhatsAppSendRequest
    ctor(string To, string Text)
    string Text { get; init; }
    string To { get; init; }
  sealed class WhatsAppSkill : Skill
    ctor(WhatsApp whatsApp)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
